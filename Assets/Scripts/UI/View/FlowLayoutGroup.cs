using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    /// <summary>
    /// 모바일(2열) 및 와이드 해상도(3~N열)에 맞춰 카드를 빈틈없이 자동 정렬 및 테트리스 패킹(Dense Packing)하는 uGUI 레이아웃 그룹입니다.
    /// 일반 카드(룬/재료: 1슬롯)와 장비 카드(무기: 2슬롯)를 지원하며,
    /// 빈 슬롯이 생길 경우 후속 아이템을 앞으로 당겨 슬롯을 최적 채움으로써 행 중간에 빈칸이 남는 문제를 원천 차단합니다.
    /// (Zero-Allocation: 모든 행 및 슬롯 계산 버퍼를 캐싱하여 GC 할당을 완전히 배제합니다.)
    /// </summary>
    [AddComponentMenu("Layout/Flow Layout Group")]
    [DisallowMultipleComponent]
    public class FlowLayoutGroup : LayoutGroup
    {
        [Header("Cell Settings")]
        [Tooltip("1개 기본 슬롯(룬/재료 카드)의 기준 크기")]
        [SerializeField] private Vector2 _cellSize = new Vector2(310.3f, 275f);

        [Tooltip("카드 간 가로/세로 간격")]
        [SerializeField] private Vector2 _spacing = new Vector2(20f, 15f);

        [Header("Columns")]
        [Tooltip("컨테이너 가로폭에 맞춰 열 수를 자동 계산할지 여부")]
        [SerializeField] private bool _autoColumns = true;

        [Tooltip("최소 열 수 (모바일 세로 모드 기본 2열 보장)")]
        [SerializeField] private int _minColumns = 2;

        [Tooltip("AutoColumns가 false일 때 사용할 고정 열 수")]
        [SerializeField] private int _fixedColumnCount = 2;

        [Tooltip("컨테이너 가로폭에 맞춰 셀 크기를 균등하게 늘려 채울지 여부 (기본: false, 고정 cellSize 유지 후 정렬)")]
        [SerializeField] private bool _fitColumns = false;

        [Header("Packing")]
        [Tooltip("빈칸이 발생하지 않도록 슬롯 크기에 맞는 아이템을 우선 배치하는 최적화 패킹 사용 여부")]
        [SerializeField] private bool _densePacking = true;

        public Vector2 CellSize
        {
            get => _cellSize;
            set => SetProperty(ref _cellSize, value);
        }

        public Vector2 Spacing
        {
            get => _spacing;
            set => SetProperty(ref _spacing, value);
        }

        public bool AutoColumns
        {
            get => _autoColumns;
            set => SetProperty(ref _autoColumns, value);
        }

        public int MinColumns
        {
            get => _minColumns;
            set => SetProperty(ref _minColumns, Mathf.Max(1, value));
        }

        public int FixedColumnCount
        {
            get => _fixedColumnCount;
            set => SetProperty(ref _fixedColumnCount, Mathf.Max(1, value));
        }

        public bool FitColumns
        {
            get => _fitColumns;
            set => SetProperty(ref _fitColumns, value);
        }

        public bool DensePacking
        {
            get => _densePacking;
            set => SetProperty(ref _densePacking, value);
        }

        // --- 무할당(GC-Free) 캐싱 구조체 및 버퍼 ---
        private struct LayoutItem
        {
            public RectTransform Rect;
            public int ColSpan;
            public bool Placed;
        }

        private struct PlacedItem
        {
            public RectTransform Rect;
            public float X;
            public float Y;
            public float Width;
            public float Height;
        }

        private readonly List<LayoutItem> _items = new(64);
        private readonly List<PlacedItem> _placedItems = new(64);

        private int _totalRowCount = 0;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            float minWidth = padding.horizontal + _minColumns * _cellSize.x + Mathf.Max(0, _minColumns - 1) * _spacing.x;
            SetLayoutInputForAxis(minWidth, minWidth, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            CalculatePlacement();

            float totalHeight = padding.vertical + _totalRowCount * _cellSize.y + Mathf.Max(0, _totalRowCount - 1) * _spacing.y;
            SetLayoutInputForAxis(totalHeight, totalHeight, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            // 매 레이아웃 패스마다 항상 최신 상태로 배치 계산 실행 (자식 추가/복제/순서 변경 즉각 반영)
            CalculatePlacement();

            for (int i = 0; i < _placedItems.Count; i++)
            {
                PlacedItem item = _placedItems[i];
                if (item.Rect != null)
                {
                    SetChildAlongAxis(item.Rect, 0, item.X, item.Width);
                }
            }
        }

        public override void SetLayoutVertical()
        {
            for (int i = 0; i < _placedItems.Count; i++)
            {
                PlacedItem item = _placedItems[i];
                if (item.Rect != null)
                {
                    SetChildAlongAxis(item.Rect, 1, item.Y, item.Height);
                }
            }
        }

        /// <summary>
        /// 컨테이너 너비와 자식들의 Span을 기반으로 빈틈없이 최적 배치하는 Dense Slot Packing을 실행합니다.
        /// </summary>
        private void CalculatePlacement()
        {
            _items.Clear();
            _placedItems.Clear();

            // 에디터에서 Ctrl+D 복제 등으로 rectChildren이 아직 동기화되지 않은 경우 강제 갱신
            if (rectChildren.Count != rectTransform.childCount)
            {
                base.CalculateLayoutInputHorizontal();
            }

            int childCount = rectChildren.Count;
            if (childCount == 0)
            {
                _totalRowCount = 0;
                return;
            }

            float containerWidth = rectTransform.rect.width;
            if (containerWidth <= 0f && transform.parent is RectTransform parentRect)
            {
                containerWidth = parentRect.rect.width;
            }

            float availableWidth = Mathf.Max(0f, containerWidth - padding.horizontal);
            if (availableWidth <= 0f)
            {
                availableWidth = _minColumns * _cellSize.x + Mathf.Max(0, _minColumns - 1) * _spacing.x;
            }

            // 열(Column) 개수 계산
            int columns = _autoColumns
                ? Mathf.Max(_minColumns, Mathf.FloorToInt((availableWidth + _spacing.x + 1.0f) / (_cellSize.x + _spacing.x)))
                : Mathf.Max(1, _fixedColumnCount);

            // 1. 활성 자식 아이템 수집 및 Span 결정
            for (int i = 0; i < childCount; i++)
            {
                RectTransform child = rectChildren[i];
                if (child == null) continue;

                int span = GetColSpan(child, columns);
                _items.Add(new LayoutItem
                {
                    Rect = child,
                    ColSpan = span,
                    Placed = false
                });
            }

            int totalItems = _items.Count;
            if (totalItems == 0)
            {
                _totalRowCount = 0;
                return;
            }

            // 2. 셀 너비 및 가로 오프셋(정렬) 계산
            float cellWidth = _fitColumns
                ? Mathf.Max(_cellSize.x, (availableWidth - Mathf.Max(0, columns - 1) * _spacing.x) / columns)
                : _cellSize.x;
            float cellHeight = _cellSize.y;

            float gridTotalWidth = columns * cellWidth + Mathf.Max(0, columns - 1) * _spacing.x;
            float startX = padding.left;
            TextAnchor alignment = childAlignment;
            if (alignment == TextAnchor.UpperCenter || alignment == TextAnchor.MiddleCenter || alignment == TextAnchor.LowerCenter)
            {
                startX += Mathf.Max(0f, (availableWidth - gridTotalWidth) * 0.5f);
            }
            else if (alignment == TextAnchor.UpperRight || alignment == TextAnchor.MiddleRight || alignment == TextAnchor.LowerRight)
            {
                startX += Mathf.Max(0f, availableWidth - gridTotalWidth);
            }

            // 3. Dense Slot Packing (빈 슬롯 채움 최적화 알고리즘)
            int placedCount = 0;
            int currentRow = 0;

            while (placedCount < totalItems)
            {
                int remainingCols = columns;
                int colIndex = 0;

                while (remainingCols > 0 && placedCount < totalItems)
                {
                    int bestIndex = -1;

                    if (_densePacking)
                    {
                        // [1순위] 남은 슬롯 수와 정확히 일치하는 크기의 미배치 아이템 탐색
                        // (예: 2칸 남았을 때 2칸짜리 무기를 먼저 넣어 빈틈없이 채움)
                        for (int i = 0; i < totalItems; i++)
                        {
                            if (!_items[i].Placed && _items[i].ColSpan == remainingCols)
                            {
                                bestIndex = i;
                                break;
                            }
                        }

                        // [2순위] 남은 슬롯 이하 크기 중 가장 먼저 등록된 미배치 아이템 탐색
                        if (bestIndex == -1)
                        {
                            for (int i = 0; i < totalItems; i++)
                            {
                                if (!_items[i].Placed && _items[i].ColSpan <= remainingCols)
                                {
                                    bestIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // 순차 배치 (Dense Packing 미사용 시)
                        for (int i = 0; i < totalItems; i++)
                        {
                            if (!_items[i].Placed)
                            {
                                if (_items[i].ColSpan <= remainingCols)
                                {
                                    bestIndex = i;
                                }
                                break;
                            }
                        }
                    }

                    // 이번 행의 남은 공간에 들어갈 수 있는 아이템이 더 이상 없으면 다음 행으로 넘김
                    if (bestIndex == -1)
                    {
                        break;
                    }

                    LayoutItem item = _items[bestIndex];
                    item.Placed = true;
                    _items[bestIndex] = item;
                    placedCount++;

                    float itemX = startX + colIndex * (cellWidth + _spacing.x);
                    float itemY = padding.top + currentRow * (cellHeight + _spacing.y);
                    float itemWidth = item.ColSpan * cellWidth + Mathf.Max(0, item.ColSpan - 1) * _spacing.x;
                    float itemHeight = cellHeight;

                    _placedItems.Add(new PlacedItem
                    {
                        Rect = item.Rect,
                        X = itemX,
                        Y = itemY,
                        Width = itemWidth,
                        Height = itemHeight
                    });

                    colIndex += item.ColSpan;
                    remainingCols -= item.ColSpan;
                }

                currentRow++;
            }

            _totalRowCount = currentRow;
        }

        /// <summary>
        /// 자식 RectTransform의 열 점유 수(Span)를 판별합니다.
        /// 1. ShopLayoutItem 컴포넌트 명시값
        /// 2. LayoutElement min/preferred width
        /// 3. RectTransform 프리팹 초기 sizeDelta.x
        /// 4. 오브젝트 이름 기반 (Equipment, Weapon, 무기 등)
        /// </summary>
        private int GetColSpan(RectTransform child, int maxColumns)
        {
            if (child == null) return 1;

            // 1. ShopLayoutItem 명시적 컴포넌트
            if (child.TryGetComponent<ShopLayoutItem>(out var layoutItem))
            {
                return Mathf.Clamp(layoutItem.ColSpan, 1, maxColumns);
            }

            // 2. LayoutElement
            if (child.TryGetComponent<LayoutElement>(out var layoutElement))
            {
                float pref = layoutElement.preferredWidth > 0 ? layoutElement.preferredWidth : layoutElement.minWidth;
                if (pref > _cellSize.x * 1.5f)
                {
                    return Mathf.Min(2, maxColumns);
                }
            }

            // 3. RectTransform 자체 너비 (프리팹 기준 너비: 무기는 620.6 > 310.3 * 1.5f)
            float currentWidth = child.sizeDelta.x > 0 ? child.sizeDelta.x : child.rect.width;
            if (currentWidth > _cellSize.x * 1.5f)
            {
                return Mathf.Min(2, maxColumns);
            }

            // 4. 오브젝트 이름 기반 식별
            string childName = child.name;
            if (childName.IndexOf("Equipment", StringComparison.OrdinalIgnoreCase) >= 0 ||
                childName.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                childName.IndexOf("무기", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Mathf.Min(2, maxColumns);
            }

            return 1;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetDirty();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                base.CalculateLayoutInputHorizontal();
                CalculatePlacement();
                SetLayoutHorizontal();
                SetLayoutVertical();
            }
#endif
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (!Application.isPlaying)
            {
                base.CalculateLayoutInputHorizontal();
                CalculatePlacement();
                SetLayoutHorizontal();
                SetLayoutVertical();
            }
        }

        protected override void OnTransformChildrenChanged()
        {
            base.OnTransformChildrenChanged();
            if (!Application.isPlaying)
            {
                base.CalculateLayoutInputHorizontal();
                CalculatePlacement();
                SetLayoutHorizontal();
                SetLayoutVertical();
            }
        }
#endif
    }
}
