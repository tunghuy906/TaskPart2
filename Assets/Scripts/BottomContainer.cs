using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class BottomContainer : MonoBehaviour
{
	public List<Transform> slots;
	private List<Item> items = new List<Item>();

	public bool IsFull => items.Count >= slots.Count;
	public bool IsEmpty => items.Count == 0;

	public void AddItem(Item item)
	{
		if (IsFull)
		{
			GameManager.Instance.LoseGame();
			return;
		}

		items.Add(item);
		int index = items.Count - 1;

		
		if (item.View != null && index >= 0 && index < slots.Count)
		{
			item.View.SetParent(slots[index], true);
			item.View.DOScale(0.8f, 0.2f).SetEase(Ease.OutBack);
			item.View.DOMove(slots[index].position, 0.3f).SetEase(Ease.OutQuad)
				.OnComplete(() => CheckMatchAfterAdd());
		}
		else
		{
			CheckMatchAfterAdd();
		}
	}


	public void ClearTriple(string type)
	{
		
		var matched = items.Where(i => i.Type == type).Take(3).ToList();
		if (matched.Count == 3)
		{
			ClearTripleInternal(matched);
		}
	}

	private void CheckMatchAfterAdd()
	{
		var grouped = items.GroupBy(i => i.Type);
		var groupToClear = grouped.FirstOrDefault(g => g.Count() >= 3);
		if (groupToClear != null)
		{
			var matched = items.Where(i => i.Type == groupToClear.Key).Take(3).ToList();
			ClearTripleInternal(matched);
		}
	}

	private void ClearTripleInternal(List<Item> matched)
	{
		foreach (var i in matched)
		{
			items.Remove(i);

			if (i.View != null)
			{
				i.View.DOKill();
				i.View.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
					.OnComplete(() => Destroy(i.View.gameObject));
			}
		}

		RearrangeSlots();

		
		if (GameManager.Instance != null)
		{
			
			var cells = FindObjectsOfType<Cell>();
			if (cells.Length > 0 && cells.All(c => c.IsEmpty))
			{
				GameManager.Instance.WinGame();
			}
		}
	}

	private void RearrangeSlots()
	{
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].View != null && i < slots.Count)
			{
				items[i].View.DOMove(slots[i].position, 0.25f);
			}
		}
	}

	public List<Item> GetItems() => items;

	public void ResetContainer()
	{
		foreach (var i in items)
		{
			if (i != null && i.View != null)
			{
				i.View.DOKill();
				Destroy(i.View.gameObject);
			}
		}
		items.Clear();
	}
	public void RemoveItem(Item item)
	{
		if (items.Contains(item))
		{
			items.Remove(item);
		}
	}
}
