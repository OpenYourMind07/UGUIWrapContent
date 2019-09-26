using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIWrapContent : MonoBehaviour {

    enum AdapationType
    {
        /// <summary>
        /// 计算列数
        /// </summary>
        ModifyColumns,

        /// <summary>
        /// 固定列数
        /// </summary>
        FixedColumns,
    }

    enum SizeType
    {
        /// <summary>
        /// 计算大小
        /// </summary>
        ModifySize,

        /// <summary>
        /// 固定大小
        /// </summary>
        FixedSizes,
    }

    [SerializeField]
    private ScrollRect scrollRect;
    [SerializeField]
    private GameObject cellPrefab;
    [SerializeField]
    private SizeType cellSizeType;
    [SerializeField]
    private Vector2 cellSize = new Vector2(50, 50);
    [SerializeField]
    private Vector2 cellOffset;
    [SerializeField]
    private bool isFullLoop;
    [SerializeField]
    private AdapationType adapationType;
    [SerializeField]
    private int numberOfColumns = 1;

    private LinkedList<GameObject> localCellsPool = new LinkedList<GameObject>();
    public LinkedList<GameObject> cellsInUse = new LinkedList<GameObject>();

    private bool initFinish;
    private int visibleCellsRowCount;
    private int visibleCellsTotalCount;
    private int Datacount = 0;
    private int preFirstVisibleIndex;


    public delegate void OnSetInfo(GameObject Wrapitem , int wrapIndex);
    public delegate void OnHideInfo(GameObject Wrpaitem, int wrapIndex);
    public OnSetInfo OnSetInfoHandler;
    public OnHideInfo OnHideInfoHandler;


    private bool vertical
    {
        get { return scrollRect.vertical; }
    }
    
    private void Awake()
    {
        scrollRect.onValueChanged.AddListener(OnValueChangedHandle);
    }

    private void OnValueChangedHandle(Vector2 arg0)
    {
        CalculateCurrentIndex();
    }


    /// <summary>
    /// 重新计算并绘制界面
    /// 此方法公开，有时候外部需要调用
    /// </summary>
    public void CalculateCurrentIndex()
    {
        if (Datacount == 0)
            return;
        int firstVisibleIndex;
        if (vertical)
        {
            firstVisibleIndex = Mathf.FloorToInt((scrollRect.content.anchoredPosition.y) / (cellSize.y + cellOffset.y));
        }
        else
        {
            firstVisibleIndex = Mathf.FloorToInt((- scrollRect.content.anchoredPosition.x) / (cellSize.x+cellOffset.x));
        }

        if (!isFullLoop)
        {
            int limit = Mathf.CeilToInt((float)Datacount / (float)numberOfColumns) - visibleCellsRowCount;
            if (firstVisibleIndex < 0 || limit <= 0)
                firstVisibleIndex = 0;
            else if (firstVisibleIndex >= limit)
                firstVisibleIndex = limit - 1;
        }
        
        if (preFirstVisibleIndex != firstVisibleIndex)
        {
            bool scrollingPositive = preFirstVisibleIndex < firstVisibleIndex;
            int indexDelta = Mathf.Abs(preFirstVisibleIndex - firstVisibleIndex);

            int deltaSign = scrollingPositive ? +1 : -1;

            for (int i = 1; i <= indexDelta; i++)
                UpdateContent(preFirstVisibleIndex + i * deltaSign, scrollingPositive);

            preFirstVisibleIndex = firstVisibleIndex;
        }
    }

    void UpdateContent(int cellIndex, bool scrollingPositive)
    {
        int index = scrollingPositive ? ((cellIndex - 1) * numberOfColumns) + (visibleCellsTotalCount) : (cellIndex * numberOfColumns);
        for (int i = 0; i < numberOfColumns; i++)
        {
            FreeCell(scrollingPositive);
            ShowCell(index + i, scrollingPositive);
        }
    }

    void FreeCell(bool scrollingPositive)
    {
        LinkedListNode<GameObject> cell = null;
        if (scrollingPositive)
        {
            cell = cellsInUse.First;
            cellsInUse.RemoveFirst();
            localCellsPool.AddLast(cell);
        }
        else
        {
            cell = cellsInUse.Last;
            cellsInUse.RemoveLast();
            localCellsPool.AddFirst(cell);
        }
    }

    public void InitWithData(int listCount, Vector2 ObjSize = new Vector2(), bool resetPos = true )
    {

        if (cellSizeType == SizeType.ModifySize)
        {
            cellSize = ObjSize;
        }

        InitData();
        SetCellsPool();


        if (resetPos)
        {
            if (vertical)
                scrollRect.verticalNormalizedPosition = 1f;
            else
                scrollRect.horizontalNormalizedPosition = 1f;
        }
        
        Datacount = listCount;
        ResetContent();
    }

    void ShowCell(int cellIndex, bool scrollingPositive)
    {
        
        GameObject tempCell = GetCellFromPool(scrollingPositive);
        if (!isFullLoop)
        {
            if (cellIndex < Datacount)
            {
                tempCell.gameObject.SetActive(true);

                if (OnSetInfoHandler != null)
                {
                    OnSetInfoHandler(tempCell , cellIndex);

                }
                
            }
            else
            {
                tempCell.gameObject.SetActive(false);
                if (OnHideInfoHandler != null)
                {
                    OnHideInfoHandler(tempCell, cellIndex);
                }

            }

        }
        else
        {
            int realIndexInList = cellIndex % Datacount;
            if (realIndexInList < 0)
                realIndexInList = Datacount + realIndexInList;

            tempCell.gameObject.SetActive(true);

            if (OnSetInfoHandler != null)
            {
                OnSetInfoHandler(tempCell, realIndexInList);

            }
        }
        
        PositionCell(tempCell.gameObject, cellIndex);
        
    }

    void PositionCell(GameObject go, int index)
    {
        int rowMod = index % numberOfColumns;
        int tmepIndex = index >= 0 ? index : index + 1;
        rowMod = rowMod >= 0 ? rowMod : rowMod + numberOfColumns;
        int addValue = index >= 0 ? 0 : 1;
        Vector2 anchoredPos;
        if(vertical)
            anchoredPos = new Vector2((cellSize.x + cellOffset.x) * rowMod , (addValue - (tmepIndex / numberOfColumns)) * (cellSize.y + cellOffset.y));
        else
            anchoredPos = new Vector2((cellSize.x + cellOffset.x )* (tmepIndex / numberOfColumns - addValue) , -rowMod * (cellSize.y + cellOffset.y));
        go.GetComponent<RectTransform>().anchoredPosition = anchoredPos;
    }

    GameObject GetCellFromPool(bool scrollingPositive)
    {
        if (localCellsPool.Count == 0)
            return null;
        LinkedListNode<GameObject> cell = localCellsPool.First;
        localCellsPool.RemoveFirst();

        if (scrollingPositive)
            cellsInUse.AddLast(cell);
        else
            cellsInUse.AddFirst(cell);
        return cell.Value;
    }

    void ResetContent()
    {
        preFirstVisibleIndex = 0;
        int outSideCount = cellsInUse.Count;
        while (outSideCount > 0)
        {
            
            cellsInUse.Last.Value.gameObject.SetActive(false);

            if (OnHideInfoHandler != null)
            {
                OnHideInfoHandler(cellsInUse.Last.Value.gameObject, outSideCount);
            }
            localCellsPool.AddLast(cellsInUse.Last.Value);
            cellsInUse.RemoveLast();
            outSideCount--;
        }

        SetContentSize();
        int showCount;
        if (!isFullLoop)
            showCount = (visibleCellsTotalCount > Datacount) ? Datacount : visibleCellsTotalCount;
        else
            showCount = visibleCellsTotalCount;
        for (int i = 0; i < showCount; i++)
        {
            ShowCell(i, true);
        }
    }

    void SetContentSize()
    {
        int cellOneWayCount = Mathf.CeilToInt((float)Datacount / (float)numberOfColumns);
        float addOffset;
        if (vertical)
        {
            addOffset = cellOneWayCount > 1 ? cellOffset.y * (cellOneWayCount - 1) : 0;
            scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, cellOneWayCount * cellSize.y + addOffset);
        }
        else
        {
            addOffset = cellOneWayCount > 1 ? cellOffset.x * (cellOneWayCount - 1) : 0;
            scrollRect.content.sizeDelta = new Vector2(cellOneWayCount * cellSize.x + addOffset, scrollRect.content.sizeDelta.y);
        }
    }

    void InitData()
    {
        if (vertical)
        {
            visibleCellsRowCount = Mathf.CeilToInt(scrollRect.viewport.rect.height / (cellSize.y + cellOffset.y));
            if (adapationType == AdapationType.ModifyColumns)
                numberOfColumns = Mathf.FloorToInt(scrollRect.viewport.rect.width / (cellSize.x + cellOffset.x));
        }
        else
        {
            visibleCellsRowCount = Mathf.CeilToInt(scrollRect.viewport.rect.width / (cellSize.x + cellOffset.x));
            if (adapationType == AdapationType.ModifyColumns)
                numberOfColumns = Mathf.FloorToInt(scrollRect.viewport.rect.height / (cellSize.y + cellOffset.y));
        }
       
        visibleCellsTotalCount = (visibleCellsRowCount + 1) * numberOfColumns;
    }

    void SetCellsPool()
    {
        int outSideCount = localCellsPool.Count + cellsInUse.Count - visibleCellsTotalCount;
        if (outSideCount > 0)
        {
            while (outSideCount > 0)
            {
                outSideCount--;
                LinkedListNode<GameObject> cell = localCellsPool.Last;
                localCellsPool.RemoveLast();
                Destroy(cell.Value.gameObject);
            }
        }
        else if (outSideCount < 0)
        {
            for (int i = 0; i < -outSideCount; i++)
            {
                GameObject cell = Instantiate(cellPrefab);
                localCellsPool.AddLast(cell);
                cell.transform.SetParent(scrollRect.content.transform, false);
                cell.gameObject.SetActive(false);
                //cell.Hidden();
            }
        }
    }
}
