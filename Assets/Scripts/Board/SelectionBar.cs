using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum AddResult { Added, ClearedThree, BarFull }

public class SelectionBar : MonoBehaviour
{
    public Transform[] slots;
    public int capacity = 5;
    public float moveDuration = 0.25f;
    public float landingPunchScale = 1.08f;
    public float clearScaleDuration = 0.18f;
    public float relayoutDuration = 0.15f;

    private readonly List<Item> buffer = new List<Item>();

    private struct Entry { public Item item; public Cell origin; public Entry(Item i, Cell o) { item = i; origin = o; } }
    private readonly List<Entry> entries = new List<Entry>();

    public bool IsFull => buffer.Count >= capacity;

    public AddResult TryAdd(Item t)
    {
        if (buffer.Count >= capacity) return AddResult.BarFull;

        buffer.Add(t);
        entries.Add(new Entry(t, null));

        t.SetViewRoot(transform);

        int idx = buffer.Count - 1;
        Vector3 target = slots[idx].position;

        MoveTo(t, target);

        return CheckAndClearTriplet(t);
    }

    public AddResult TryAddFromCell(Cell fromCell)
    {
        if (fromCell == null || fromCell.IsEmpty) return AddResult.Added;
        var t = fromCell.Item;

        if (buffer.Count >= capacity) return AddResult.BarFull;

        buffer.Add(t);
        entries.Add(new Entry(t, fromCell));

        t.SetViewRoot(transform);

        int idx = buffer.Count - 1;
        Vector3 target = slots[idx].position;

        MoveTo(t, target);

        return CheckAndClearTriplet(t);
    }
    public bool TryReturnAtIndex(int idx)
    {
        if (idx < 0 || idx >= buffer.Count) return false;

        var it = buffer[idx];
        var en = entries[idx];

        buffer.RemoveAt(idx);
        entries.RemoveAt(idx);
        if (en.origin != null)
        {
            en.origin.Assign(it);
            en.origin.ApplyItemPosition(true);
        }

        Relayout();

        return true;
    }
    public int GetCountInBar(int typeId)
    {
        int c = 0;
        foreach (var it in buffer)
            if (it is NormalItem n && (int)n.ItemType == typeId) c++;
        return c;
    }

    public void ResetBar()
    {
        for (int i = 0; i < buffer.Count; i++)
        {
            if (buffer[i]?.View != null)
                Destroy(buffer[i].View.gameObject);
        }
        buffer.Clear();
        entries.Clear();
    }

    private void MoveTo(Item t, Vector3 target)
    {
        if (t?.View != null)
        {
            t.View.DOMove(target, moveDuration).OnComplete(() =>
            {
                if (landingPunchScale > 1.001f && t.View != null)
                {
                    t.View.DOPunchScale(Vector3.one * (landingPunchScale - 1f), 0.12f, 6, 0.7f);
                }
            });
        }
        else
        {
            t?.SetViewPosition(target);
        }
    }

    private AddResult CheckAndClearTriplet(Item lastAdded)
    {
        int typeId = (lastAdded is NormalItem n) ? (int)n.ItemType : -1;
        if (typeId < 0) return AddResult.Added;

        int same = 0;
        for (int i = 0; i < buffer.Count; i++)
            if (buffer[i] is NormalItem ni && (int)ni.ItemType == typeId) same++;

        if (same == 3)
        {
            //Erase 3 same items
            int removed = 0;
            for (int i = buffer.Count - 1; i >= 0 && removed < 3; --i)
            {
                if (buffer[i] is NormalItem bi && (int)bi.ItemType == typeId)
                {
                    var kill = buffer[i];

                    buffer.RemoveAt(i);
                    entries.RemoveAt(i); 

                    if (kill?.View != null)
                    {
                        kill.View.DOScale(Vector3.zero, clearScaleDuration)
                            .OnComplete(() =>
                            {
                                if (kill?.View != null)
                                    Destroy(kill.View.gameObject);
                            });
                    }
                    removed++;
                }
            }

            Relayout();

            return AddResult.ClearedThree;
        }

        return AddResult.Added;
    }

    private void Relayout()
    {
        for (int i = 0; i < buffer.Count; i++)
        {
            var it = buffer[i];
            var target = slots[Mathf.Clamp(i, 0, slots.Length - 1)].position;

            if (it?.View != null) it.View.DOMove(target, relayoutDuration);
            else it?.SetViewPosition(target);
        }
    }
}
