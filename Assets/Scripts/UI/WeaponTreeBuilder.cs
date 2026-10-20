using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.UI
{
    public class WeaponTreeBuilder : MonoBehaviour
    {
        [Header("테스트 루트 무기")]
        [SerializeField] private SO_WeaponData testRootWeapon;

        [Header("UI 참조")]
        [SerializeField] private GameObject weaponTreePanel;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRect; // ScrollView의 Content
        [SerializeField] private Transform branchContainer;
        [SerializeField] private Transform nodeContainer;
        [SerializeField] private Toggle _fixRecipe;
        [SerializeField] private Button _button;

        public bool IsActivated => weaponTreePanel != null
            ? weaponTreePanel.activeSelf
            : (scrollRect != null ? scrollRect.gameObject.activeInHierarchy : gameObject.activeInHierarchy);

        public void Hide()
        {
            if (weaponTreePanel != null)
            {
                weaponTreePanel.SetActive(false);
            }
            else if (scrollRect != null)
            {
                scrollRect.gameObject.SetActive(false);
            }
        }

        [Header("프리팹")]
        [SerializeField] private WeaponNodeUI nodePrefab;
        [SerializeField] private WeaponTreeBranchView branchPrefab;

        [Header("배치 간격")]
        [SerializeField] private float nodeSpacingX = 180f;
        [SerializeField] private float nodeSpacingY = 180f;
        [SerializeField] private Vector2 padding = new Vector2(150f, 150f);
        private WeaponSpriteLoader spriteLoader;
        private Dictionary<uint, WeaponNodeUI> nodes = new();
        private float maxPosX = 0f;
        private float maxPosY = 0f;
        private CompositeDisposable _disposables = new();
        private readonly Subject<ChangeRecipeFlagEvent> _onChangeRecipeFlagEventSubject = new();
        public Observable<ChangeRecipeFlagEvent> OnChangeRecipeFlagEventAsObservable => _onChangeRecipeFlagEventSubject;
        public Observable<bool> OnFixRecipeChangedAsObservable => _fixRecipe != null ? _fixRecipe.OnValueChangedAsObservable() : Observable.Empty<bool>();
        public bool IsFixRecipeChecked => _fixRecipe != null && _fixRecipe.isOn;
        private Dictionary<(uint from, uint to), WeaponTreeBranchView> _branches = new();
        public Dictionary<(uint from, uint to), WeaponTreeBranchView> Branches => _branches;
        [Header("버튼 이동 설정")]
        [SerializeField] private float targetShowY = 295f;
        private RectTransform _buttonRect;
        private float _defaultY = 0f;
        private CancellationTokenSource _animCts;


        [Inject]
        public void Construct(WeaponSpriteLoader spriteLoader)
        {
            this.spriteLoader = spriteLoader;
        }

        private void Awake()
        {
            if (_button != null)
            {
                _buttonRect = _button.GetComponent<RectTransform>();
                if (_buttonRect != null)
                {
                    _defaultY = _buttonRect.anchoredPosition.y;
                }
            }
        }

        private const float ANIM_DURATION = 0.25f;

        /// <summary>
        /// 무기트리 패널이 보일 때 버튼을 위로 올리는 애니메이션
        /// </summary>
        /// <returns></returns>
        public async UniTaskVoid ShowingButtonAnimation()
        {
            if (_buttonRect == null) return;

            _animCts?.Cancel();
            _animCts?.Dispose();

            _animCts = new();
            var token = _animCts.Token;

            float startY = _buttonRect.anchoredPosition.y;
            float elapsed = 0f;

            while (elapsed < ANIM_DURATION)
            {
                if (token.IsCancellationRequested) return;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ANIM_DURATION);
                float ease = 1f - Mathf.Pow(1f - t, 3);
                float currentY = Mathf.LerpUnclamped(startY, targetShowY, ease);
                _buttonRect.anchoredPosition = new Vector2(_buttonRect.anchoredPosition.x, currentY);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            _buttonRect.anchoredPosition = new Vector2(_buttonRect.anchoredPosition.x, targetShowY);
        }

        /// <summary>
        /// 무기트리 패널이 닫힐 때 버튼을 내리는 애니메이션
        /// </summary>
        /// <returns></returns>
        public async UniTaskVoid HidingButtonAnimation()
        {
            if (_buttonRect == null) return;

            _animCts?.Cancel();
            _animCts?.Dispose();

            _animCts = new();
            var token = _animCts.Token;

            float startY = _buttonRect.anchoredPosition.y;
            float elapsed = 0f;

            while (elapsed < ANIM_DURATION)
            {
                if (token.IsCancellationRequested) return;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ANIM_DURATION);
                float ease = 1f - Mathf.Pow(1f - t, 3);
                float currentY = Mathf.LerpUnclamped(startY, _defaultY, ease);
                _buttonRect.anchoredPosition = new Vector2(_buttonRect.anchoredPosition.x, currentY);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            _buttonRect.anchoredPosition = new Vector2(_buttonRect.anchoredPosition.x, _defaultY);
        }

        /// <summary>
        /// 화면 스크롤시 버튼의 Y좌표를 동기화(패널이 닫힐 때 버튼이 패널에 가려지는 것을 방지)
        /// </summary>
        /// <param name="y"></param>
        public void SyncButtonYPosition(float y)
        {
            if (_buttonRect != null)
            {
                _buttonRect.anchoredPosition = new Vector2(_buttonRect.anchoredPosition.x, targetShowY + y);
            }
        }

        /// <summary>
        /// (테스트용) 컨텍스트 메뉴에서 트리 생성
        /// </summary>
        [ContextMenu("Generate Tree (트리 생성)")]
        public void GenerateTreeFromContext()
        {
            if (testRootWeapon != null)
                GenerateTree(testRootWeapon);
        }

        /// <summary>
        /// (테스트용) 컨텍스트 메뉴에서 트리 초기화
        /// </summary>
        [ContextMenu("Clear Tree (트리 초기화)")]
        public void ClearTree()
        {
            maxPosX = 0f;
            maxPosY = 0f;
            _disposables.Clear();
            _branches.Clear();

            ClearContainer(branchContainer);
            ClearContainer(nodeContainer);
        }
        /// <summary>
        /// 무기트리 생성
        /// </summary>
        /// <param name="rootWeapon">기본무기</param>
        /// <returns>생성된 노드 사전</returns>
        public Dictionary<uint, WeaponNodeUI> GenerateTree(SO_WeaponData rootWeapon)
        {
            if (rootWeapon == null) return nodes;

            nodes.Clear();
            ClearTree();

            _leafColumnIndex = 0;
            LayoutNodeDynamic(rootWeapon, 0);
            ConnectAllBranches();

            if (contentRect != null)
            {
                float totalWidth = maxPosX + padding.x;
                float totalHeight = maxPosY + padding.y;

                if (scrollRect != null && scrollRect.viewport != null)
                {
                    totalWidth = Mathf.Max(totalWidth, scrollRect.viewport.rect.width);
                    totalHeight = Mathf.Max(totalHeight, scrollRect.viewport.rect.height);
                }

                contentRect.sizeDelta = new Vector2(totalWidth, totalHeight);
            }
            if (gameObject.activeInHierarchy)
            {
                ResetScrollPositionRoutine().Forget();
            }
            return nodes;
        }

        private int _leafColumnIndex = 0;

        /// <summary>
        /// 무기 노드를 동적으로 배치
        /// </summary>
        private WeaponNodeUI LayoutNodeDynamic(SO_WeaponData weapon, int depth)
        {
            if (weapon == null) return null;

            if (nodes.TryGetValue(weapon.Id, out var existingNode))
            {
                return existingNode;
            }

            List<WeaponNodeUI> primaryChildUIs = new();

            if (weapon.recipes != null && weapon.recipes.Count > 0)
            {
                for (int i = 0; i < weapon.recipes.Count; i++)
                {
                    var recipe = weapon.recipes[i];
                    if (recipe == null || recipe.recipeOutcome.resultWeapon == null) continue;

                    var childWeapon = recipe.recipeOutcome.resultWeapon;

                    if (IsSameElementOrBase(weapon, childWeapon))
                    {
                        var childUI = LayoutNodeDynamic(childWeapon, depth + 1);
                        if (childUI != null)
                        {
                            primaryChildUIs.Add(childUI);
                        }
                    }
                }
            }

            float posX;
            if (primaryChildUIs.Count > 0)
            {
                // 부모 노드는 자식들의 첫 번째 컬럼과 마지막 컬럼의 중앙 배치
                float firstX = primaryChildUIs[0].RectTransform.anchoredPosition.x;
                float lastX = primaryChildUIs[^1].RectTransform.anchoredPosition.x;
                posX = (firstX + lastX) * 0.5f;
            }
            else
            {
                posX = padding.x + (_leafColumnIndex * nodeSpacingX);
                _leafColumnIndex++;
            }

            float posY = padding.y + (depth * nodeSpacingY);

            maxPosX = Mathf.Max(maxPosX, posX);
            maxPosY = Mathf.Max(maxPosY, posY);

            WeaponNodeUI currentNode = Instantiate(nodePrefab, nodeContainer);
            currentNode.Setup(weapon, spriteLoader).Forget();
            currentNode.name = $"Node_{weapon.weaponName}";
            currentNode.RectTransform.anchoredPosition = new Vector2(posX, posY);

            nodes[weapon.Id] = currentNode;

            currentNode.OnClickAsObservable().
                ThrottleFirst(TimeSpan.FromMilliseconds(100)).
                Subscribe(e =>
                {
                    _onChangeRecipeFlagEventSubject.OnNext(new ChangeRecipeFlagEvent { _recipeId = weapon.Id });
                }).AddTo(_disposables);

            return currentNode;
        }

        /// <summary>
        /// 무기 간 속성 일치 여부 판별 - 일렬 종방향 배치를 위함
        /// </summary>
        private static bool IsSameElementOrBase(SO_WeaponData parent, SO_WeaponData child)
        {
            if (parent == null || child == null) return false;

            if (parent.elementTypes == null || parent.elementTypes.Count == 0) return true;
            if (child.elementTypes == null || child.elementTypes.Count == 0) return true;

            for (int i = 0; i < parent.elementTypes.Count; i++)
            {
                for (int j = 0; j < child.elementTypes.Count; j++)
                {
                    if (parent.elementTypes[i] == child.elementTypes[j]) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 모든 노드 간의 브랜치(선) 연결
        /// </summary>
        private void ConnectAllBranches()
        {
            foreach (var kvp in nodes)
            {
                var parentWeapon = kvp.Value.WeaponData;
                if (parentWeapon == null || parentWeapon.recipes == null) continue;

                for (int r = 0; r < parentWeapon.recipes.Count; r++)
                {
                    var recipe = parentWeapon.recipes[r];
                    if (recipe == null || recipe.recipeOutcome.resultWeapon == null) continue;

                    var childWeapon = recipe.recipeOutcome.resultWeapon;
                    if (nodes.TryGetValue(childWeapon.Id, out var childUI))
                    {
                        var key = (parentWeapon.Id, childWeapon.Id);
                        if (!_branches.ContainsKey(key))
                        {
                            WeaponTreeBranchView branch = Instantiate(branchPrefab, branchContainer);
                            branch.name = $"Branch_{parentWeapon.weaponName}->{childWeapon.weaponName}";
                            branch.SetNodes(kvp.Value.RectTransform, childUI.RectTransform);
                            branch.SetHighlight(false);
                            _branches[key] = branch;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 스크롤 위치 초기화
        /// </summary>
        /// <returns></returns>
        private async UniTask ResetScrollPositionRoutine()
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
            if (scrollRect != null)
            {
                scrollRect.normalizedPosition = new Vector2(0.5f, 0f);
            }
        }

        /// <summary>
        /// 자식 노드 및 브랜치 제거
        /// </summary>
        /// <param name="container">제거할 컨테이너</param>
        private void ClearContainer(Transform container)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                GameObject obj = container.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(obj);
                else DestroyImmediate(obj);
            }
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}