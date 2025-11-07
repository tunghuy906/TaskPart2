using System.Collections;
using UnityEngine;
using DG.Tweening;

public class LevelManager : MonoBehaviour
{
	public static LevelManager Instance;

	private int currentLevel = 0;
	private GameObject currentBoard;

	[SerializeField] private int startWidth = 6;
	[SerializeField] private int startHeight = 6;
	[SerializeField] private int maxLevel = 20;

	void Awake() => Instance = this;
	void Start() { }

	public IEnumerator CreateBoardRoutine()
	{
		
		if (currentBoard != null)
		{
			currentBoard.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
			yield return new WaitForSeconds(0.35f);

			DestroyImmediate(currentBoard);
			currentBoard = null;
			yield return null;
		}

	
		(int width, int height) = GetBoardSizeForLevel(currentLevel);
		Debug.Log($"[LevelManager] Level {currentLevel + 1} => Using board {width}x{height} ({width * height} cells)");

		
		var container = FindObjectOfType<BottomContainer>();
		if (container != null)
		{
			var reset = container.GetType().GetMethod("ResetContainer");
			if (reset != null) reset.Invoke(container, null);
		}

	
		currentBoard = new GameObject($"Board_Level_{currentLevel + 1}");
		currentBoard.transform.position = Vector3.zero;
		currentBoard.transform.SetParent(null);

		var boardController = currentBoard.AddComponent<BoardController>();
		var gameManager = GameManager.Instance;
		boardController.bottomContainer = container;

		
		var original = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
		if (original == null)
		{
			Debug.LogError("[LevelManager] Cannot find GameSettings in Resources at: " + Constants.GAME_SETTINGS_PATH);
			yield break;
		}

		var settings = ScriptableObject.Instantiate(original);

	
		settings.BoardSizeX = width;
		settings.BoardSizeY = height;

		
		Debug.Log($"[LevelManager] Passing settings to BoardController: {settings.BoardSizeX}x{settings.BoardSizeY}");

		
		boardController.StartGame(gameManager, settings);

	
		currentBoard.transform.localScale = Vector3.zero;
		currentBoard.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

		Debug.Log($"✅ Created Level {currentLevel + 1} [{width}x{height}] ({width * height} cells, divisible by 3)");
	}

	private (int, int) GetBoardSizeForLevel(int level)
	{
		
		int baseSize = 3;

		
		int w = baseSize + level;
		int h = baseSize + level;

		
		while ((w * h) % 3 != 0)
		{
			
			h++;
			if ((w * h) % 3 != 0)
				w++;
		}

		return (w, h);
	}
	
	public void NextLevel()
	{
		StartCoroutine(NextLevelRoutine());
	}

	private IEnumerator NextLevelRoutine()
	{
		if (currentBoard != null)
			currentBoard.transform.DOShakePosition(0.5f, 0.5f, 10, 90);

		yield return new WaitForSeconds(0.7f);

		currentLevel++;
		if (currentLevel >= maxLevel)
		{
			Debug.Log("🏁 Completed all levels!");
			yield break;
		}

		yield return StartCoroutine(CreateBoardRoutine());
	}

	public void RestartLevel()
	{
		StartCoroutine(RestartRoutine());
	}

	private IEnumerator RestartRoutine()
	{
		GameManager.Instance.ClearLevel();

		if (currentBoard != null)
			currentBoard.transform.DOShakeRotation(0.5f, 40, 8, 80);

		yield return new WaitForSeconds(0.6f);

		yield return StartCoroutine(CreateBoardRoutine());
	}
}
