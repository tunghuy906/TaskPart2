using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class BoardController : MonoBehaviour
{
	public event Action OnMoveEvent = delegate { };
	public bool IsBusy { get; private set; }
	private Board m_board;
	private GameManager m_gameManager;
	private Camera m_cam;
	private GameSettings m_gameSettings;
	private bool m_gameOver;

	private Item lastMovedItem = null;
	private Cell lastOriginCell = null;
	private Vector3 lastOriginPos;

	[Header("References")]
	public BottomContainer bottomContainer;

	public void StartGame(GameManager gameManager, GameSettings gameSettings)
	{
		m_gameManager = gameManager;
		m_gameSettings = gameSettings;

		m_cam = Camera.main;
		m_board = new Board(this.transform, gameSettings);
		m_board.Fill();

		m_gameManager.StateChangedAction += OnGameStateChange;
	}

	private void OnGameStateChange(GameManager.eStateGame state)
	{
		switch (state)
		{
			case GameManager.eStateGame.GAME_STARTED:
				IsBusy = false;
				break;
			case GameManager.eStateGame.PAUSE:
				IsBusy = true;
				break;
			case GameManager.eStateGame.GAME_OVER:
				m_gameOver = true;
				break;
		}
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
			if (hit.collider == null) return;

			Cell cell = hit.collider.GetComponent<Cell>();
			if (cell != null && cell.Item != null)
			{
				HandleCellClick(cell);
				return;
			}

			Item clickedItem = hit.collider.GetComponentInParent<Cell>()?.Item;
			if (clickedItem != null)
			{
				HandleContainerItemClick(clickedItem);
				return;
			}
		}
	}

	private void HandleCellClick(Cell cell)
	{
		if (cell == null || cell.Item == null || bottomContainer == null)
			return;

		
		if (lastMovedItem != null && bottomContainer.GetItems().Contains(lastMovedItem))
		{
			
			var clickedObj = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
			if (clickedObj.collider != null)
			{
				Item clickedItem = clickedObj.collider.GetComponent<Item>();
				if (clickedItem != null && clickedItem == lastMovedItem)
				{

					bottomContainer.RemoveItem(lastMovedItem);

					
					lastMovedItem.View.SetParent(transform, true);
					lastMovedItem.View.DOMove(lastOriginPos, 0.5f)
						.SetEase(Ease.OutQuad)
						.OnComplete(() =>
						{
							lastOriginCell.Assign(lastMovedItem);

							
							lastMovedItem.View.DOScale(1.2f, 0.1f)
								.SetLoops(2, LoopType.Yoyo)
								.SetEase(Ease.InOutQuad);

							
							lastMovedItem = null;
							lastOriginCell = null;
						});

					return;
				}
			}
		}

		
		OnMoveEvent?.Invoke();
		Item item = cell.Item;

		
		lastMovedItem = item;
		lastOriginCell = cell;
		lastOriginPos = item.View.position;

		
		cell.Free();

		
		if (bottomContainer.IsFull)
		{
			Debug.Log("⚠️ Container full, cannot add more items.");
			return;
		}

		
		int slotIndex = bottomContainer.GetItems().Count;
		if (slotIndex >= bottomContainer.slots.Count)
			slotIndex = bottomContainer.slots.Count - 1;

		Transform targetSlot = bottomContainer.slots[slotIndex];
		Vector3 targetPos = targetSlot.position;

		
		item.View.SetParent(bottomContainer.transform, true);
		item.View.DOMove(targetPos, 0.5f)
			.SetEase(Ease.InOutQuad)
			.OnComplete(() =>
			{
				bottomContainer.AddItem(item);
				m_gameManager.CheckBottomMatch();

				
				if (IsBoardCleared())
				{
					m_gameManager.WinGame();
				}
			});
	}
	private void HandleContainerItemClick(Item clickedItem)
	{
		if (lastMovedItem == null || clickedItem != lastMovedItem)
			return;

		
		bottomContainer.RemoveItem(lastMovedItem);

		
		lastMovedItem.View.SetParent(transform, true);
		lastMovedItem.View.DOMove(lastOriginPos, 0.5f)
			.SetEase(Ease.OutBack)
			.OnComplete(() =>
			{
				lastOriginCell.Assign(lastMovedItem);

				
				lastMovedItem.View.DOScale(1.2f, 0.15f)
					.SetLoops(2, LoopType.Yoyo)
					.SetEase(Ease.InOutQuad);

				lastMovedItem = null;
				lastOriginCell = null;
			});
	}

	private bool IsBoardCleared()
	{
		return m_board != null && m_board.IsAllEmpty();
	}

	internal void Clear()
	{
		if (m_board != null)
			m_board.Clear();
	}
}
