using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<eStateGame> StateChangedAction = delegate { };
	public static GameManager Instance;
	[SerializeField] private BottomContainer bottomContainer;

	public enum eLevelMode
    {
        TIMER,
        MOVES
    }

    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_OVER,
    }

    private eStateGame m_state;
    public eStateGame State
    {
        get { return m_state; }
        private set
        {
            m_state = value;

            StateChangedAction(m_state);
        }
    }


    private GameSettings m_gameSettings;


    private BoardController m_boardController;

    private UIMainManager m_uiMenu;

    private LevelCondition m_levelCondition;

    private void Awake()
    {
		Instance = this;
		State = eStateGame.SETUP;

		m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
		m_uiMenu = FindObjectOfType<UIMainManager>();
		m_uiMenu.Setup(this);
	}

	void Start()
    {
        State = eStateGame.MAIN_MENU;
    }

  
    void Update()
    {
        //if (m_boardController != null) m_boardController.Update();
    }


    internal void SetState(eStateGame state)
    {
        State = state;

        if(State == eStateGame.PAUSE)
        {
            DOTween.PauseAll();
        }
        else
        {
            DOTween.PlayAll();
        }
    }

	public void LoadLevel(eLevelMode mode)
	{
		
		m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
		m_boardController.StartGame(this, m_gameSettings);
		m_boardController.bottomContainer = bottomContainer;

		m_levelCondition = this.gameObject.AddComponent<LevelTime>();
		m_levelCondition.Setup(m_gameSettings.LevelTime, m_uiMenu.GetLevelConditionView(), this);

		
		m_levelCondition.ConditionCompleteEvent += GameOver;

		State = eStateGame.GAME_STARTED;
	}

	public void GameOver()
    {
        StartCoroutine(WaitBoardController());
    }

    internal void ClearLevel()
    {
        if (m_boardController)
        {
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            m_boardController = null;
        }
    }

    private IEnumerator WaitBoardController()
    {
        while (m_boardController.IsBusy)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1f);

        State = eStateGame.GAME_OVER;

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;

            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
    }
	public void CheckBottomMatch()
	{
		var allItems = bottomContainer.GetItems();

		
		if (allItems == null || allItems.Count < 3)
			return;

		
		var grouped = allItems
			.Where(i => !string.IsNullOrEmpty(i.Type))
			.GroupBy(i => i.Type)
			.ToList();

		
		var groupToClear = grouped.FirstOrDefault(g => g.Count() >= 3);

		if (groupToClear != null)
		{
			
			Debug.Log($"🧩 Match found: {groupToClear.Key} x{groupToClear.Count()}");
			bottomContainer.ClearTriple(groupToClear.Key);
		}
		else
		{
			if (bottomContainer.IsFull)
			{
				Debug.Log("❌ Container full — Lose!");
				LoseGame();
			}
		}
	}


	public void WinGame()
	{
		Debug.Log("You Win!");
		StartCoroutine(WinRoutine());
	}

	public void LoseGame()
	{
		Debug.Log("You Lose!");
		StartCoroutine(LoseRoutine());
	}

	private IEnumerator WinRoutine()
	{
		yield return new WaitForSeconds(0.5f);
		LevelManager.Instance.NextLevel(); 
	}
	private IEnumerator LoseRoutine()
	{
		yield return new WaitForSeconds(0.5f);
		LevelManager.Instance.RestartLevel();
	}
	public void HandleCellClick(Cell cell)
	{
		if (cell == null || cell.Item == null)
			return;

		
		if (bottomContainer.IsFull)
		{
			Debug.Log("⚠️ Container full, cannot add more!");
			return;
		}

		
		Item item = cell.Item;
		cell.Free(); 

		
		Vector3 startPos = item.View.position;

	
		int slotIndex = bottomContainer.GetItems().Count;
		if (slotIndex >= bottomContainer.slots.Count)
			slotIndex = bottomContainer.slots.Count - 1;

		Transform targetSlot = bottomContainer.slots[slotIndex];

		
		item.View.DOMove(targetSlot.position, 0.4f)
			.SetEase(Ease.InOutQuad)
			.OnComplete(() =>
			{
				
				bottomContainer.AddItem(item);
				CheckBottomMatch();
			});
	}
}
