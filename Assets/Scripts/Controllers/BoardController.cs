using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    [SerializeField] SelectionBar selectionBar;
  

    private int tilesOnBoard;
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private GameManager m_gameManager;

    private bool m_isDragging;

    private Camera m_cam;

    private Collider2D m_hitCollider;

    private GameSettings m_gameSettings;

    private List<Cell> m_potentialMatch;

    private float m_timeAfterFill;

    private bool m_hintIsShown;

    private bool m_gameOver;

    public GameManager.eLevelMode CurrentMode { get; private set; }
    public void StartGame(GameManager gameManager, GameSettings gameSettings, SelectionBar bar)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        selectionBar = bar;

        Fill();
        tilesOnBoard = m_board.CountNonEmptyCells();

    }
    private void Fill()
    {
        m_board.Fill();
        //FindMatchesAndCollapse();
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
            case GameManager.eStateGame.GAME_LOSE:
                m_gameOver = true;
                //StopHints();
                break;
        }
    }


    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;

        if (!m_hintIsShown)
        {
            m_timeAfterFill += Time.deltaTime;
            if (m_timeAfterFill > m_gameSettings.TimeForHint)
            {
                m_timeAfterFill = 0f;
                //ShowHint();
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                bool isTimerMode = (m_gameManager.CurrentMode == GameManager.eLevelMode.TIMER);

                if (isTimerMode)
                {
                    var tr = hit.collider.transform;
                    if (tr.IsChildOf(selectionBar.transform))
                    {
                        int bestIdx = -1;
                        float bestD = float.MaxValue;
                        for (int i = 0; i < selectionBar.slots.Length; i++)
                        {
                            float d = (tr.position - selectionBar.slots[i].position).sqrMagnitude;
                            if (d < bestD) { bestD = d; bestIdx = i; }
                        }
                        if (bestIdx >= 0 && selectionBar.TryReturnAtIndex(bestIdx))
                        {
                            tilesOnBoard++; 
                            return;
                        }
                    }
                }

                var c = hit.collider.GetComponent<Cell>();
                if (c != null && !c.IsEmpty)
                {
                    TryPick(c);
                }
            }
        }



        if (Input.GetMouseButtonUp(0))
        {
            ResetRayCast();
        }

        if (Input.GetMouseButton(0) && m_isDragging)
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                if (m_hitCollider != null && m_hitCollider != hit.collider)
                {
                    //StopHints();

                    Cell c1 = m_hitCollider.GetComponent<Cell>();
                    Cell c2 = hit.collider.GetComponent<Cell>();
                    if (AreItemsNeighbor(c1, c2))
                    {
                        IsBusy = true;
                        SetSortingLayer(c1, c2);
                        //m_board.Swap(c1, c2, () =>
                        //{
                        //    FindMatchesAndCollapse(c1, c2);
                        //});

                        ResetRayCast();
                    }
                }
            }
            else
            {
                ResetRayCast();
            }
        }
    }

    private void ResetRayCast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }

    //private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    //{
    //    if (cell1.Item is BonusItem)
    //    {
    //        cell1.ExplodeItem();
    //        StartCoroutine(ShiftDownItemsCoroutine());
    //    }
    //    else if (cell2.Item is BonusItem)
    //    {
    //        cell2.ExplodeItem();
    //        StartCoroutine(ShiftDownItemsCoroutine());
    //    }
    //    else
    //    {
    //        List<Cell> cells1 = GetMatches(cell1);
    //        List<Cell> cells2 = GetMatches(cell2);

    //        List<Cell> matches = new List<Cell>();
    //        matches.AddRange(cells1);
    //        matches.AddRange(cells2);
    //        matches = matches.Distinct().ToList();

    //        if (matches.Count < m_gameSettings.MatchesMin)
    //        {
    //            m_board.Swap(cell1, cell2, () =>
    //            {
    //                IsBusy = false;
    //            });
    //        }
    //        else
    //        {
    //            OnMoveEvent();

    //            CollapseMatches(matches, cell2);
    //        }
    //    }
    //}

    private void FindMatchesAndCollapse()
    {
        List<Cell> matches = m_board.FindFirstMatch();

        if (matches.Count > 0)
        {
            CollapseMatches(matches, null);
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count > 0)
            {
                IsBusy = false;

                m_timeAfterFill = 0f;
            }
            else
            {
                //StartCoroutine(RefillBoardCoroutine());
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }

    private List<Cell> GetMatches(Cell cell)
    {
        List<Cell> listHor = m_board.GetHorizontalMatches(cell);
        if (listHor.Count < m_gameSettings.MatchesMin)
        {
            listHor.Clear();
        }

        List<Cell> listVert = m_board.GetVerticalMatches(cell);
        if (listVert.Count < m_gameSettings.MatchesMin)
        {
            listVert.Clear();
        }

        return listHor.Concat(listVert).Distinct().ToList();
    }

    private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            matches[i].ExplodeItem();
        }

        if(matches.Count > m_gameSettings.MatchesMin)
        {
            m_board.ConvertNormalToBonus(matches, cellEnd);
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }

    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems();

        yield return new WaitForSeconds(0.2f);

        m_board.FillGapsWithNewItems();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator RefillBoardCoroutine()
    {
        m_board.ExplodeAllItems();

        yield return new WaitForSeconds(0.2f);

        m_board.Fill();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();

        yield return new WaitForSeconds(0.3f);

        FindMatchesAndCollapse();
    }


    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }

    internal void Clear()
    {
        m_board.Clear();
    }

    //private void ShowHint()
    //{
    //    m_hintIsShown = true;
    //    foreach (var cell in m_potentialMatch)
    //    {
    //        cell.AnimateItemForHint();
    //    }
    //}

    //private void StopHints()
    //{
    //    m_hintIsShown = false;
    //    foreach (var cell in m_potentialMatch)
    //    {
    //        cell.StopHintAnimation();
    //    }

    //    m_potentialMatch.Clear();
    //}

    private void TryPick(Cell c)
    {
        if (c.IsEmpty) return;

        var item = c.Item;
        var res = selectionBar.TryAddFromCell(c);
        bool isTimerMode = (m_gameManager.CurrentMode == GameManager.eLevelMode.TIMER);

        if (res == AddResult.BarFull)
        {
            if (!isTimerMode)
            {
                m_gameManager.GameOver();
                return;
            }
            else
            {
                return;
            }
        }

        if (res == AddResult.Added && selectionBar.IsFull && !isTimerMode)
        {
            m_gameManager.GameOver();
            return;
        }


        c.Free();
        tilesOnBoard--;
        if (tilesOnBoard == 0)
        {
            m_gameManager.Win();
            return;
        }
    }

    public IEnumerator AutoPlay(bool loseTarget, float delay = 0.5f)
    {
        while (true)
        {
            if (m_gameOver) yield break;

            var cells = new List<Cell>();
            foreach (var c in GetAllCells()) if (!c.IsEmpty) cells.Add(c);
            if (cells.Count == 0)
            {
                m_gameManager.Win();
                yield break;
            }

            Cell chosen = null;
            if (!loseTarget)
            {
                chosen = cells[0];
            }
            else
            {
                chosen = PickSafeCell(cells);
            }

            TryPick(chosen);

            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerable<Cell> GetAllCells()
    {
        return m_board.AllCells();
    }


    private Cell PickSafeCell(List<Cell> cells)
    {
        foreach (var c in cells)
        {
            if (c.Item is NormalItem n)
            {
                int typeId = (int)n.ItemType;
                int count = CountTypeInBar(typeId);
                if (count < 2) return c;
            }
        }
        return cells[0];
    }

    private int CountTypeInBar(int typeId)
    {
        var fi = typeof(SelectionBar).GetField("buffer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = fi.GetValue(selectionBar) as IList<Item>;
        int c = 0;
        foreach (var it in list)
            if (it is NormalItem n && (int)n.ItemType == typeId) c++;
        return c;
    }

}
