using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ElementalBlacksmithStory.Data;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;
using System.Threading;
using R3;
using MessagePipe;
using ElementalBlacksmithStory.Events;
using System;
using ElementalBlacksmithStory.Core;

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
        private int currentLeafIndex = 0;
        private float maxPosX = 0f;
        private float maxPosY = 0f;
        private CompositeDisposable _disposables = new();
        private readonly Subject<ChangeRecipeFlagEvent> _onChangeRecipeFlagEventSubject = new();
        public Observable<ChangeRecipeFlagEvent> OnChangeRecipeFlagEventAsObservable => _onChangeRecipeFlagEventSubject;
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
            currentLeafIndex = 0;
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
            nodes.Clear();
            ClearTree();
            BuildNodeRecursive(rootWeapon, 0);

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
        /// <summary>
        /// 무기 노드를 재귀적으로 생성
        /// </summary>
        /// <param name="weapon">현재 무기</param>
        /// <param name="depth">현재 깊이</param>
        /// <returns>생성된 노드</returns>
        private WeaponNodeUI BuildNodeRecursive(SO_WeaponData weapon, int depth)
        {
            if (weapon == null) return null;

            List<WeaponNodeUI> childNodes = new();
            if (weapon.recipes != null && weapon.recipes.Count > 0)
            {
                foreach (var recipe in weapon.recipes)
                {
                    if (recipe != null && recipe.recipeOutcome.resultWeapon != null)
                    {
                        WeaponNodeUI childUI = BuildNodeRecursive(recipe.recipeOutcome.resultWeapon, depth + 1);
                        if (childUI != null) childNodes.Add(childUI);
                    }
                }
            }
            float posX;
            if (childNodes.Count > 0)
            {
                float firstChildX = childNodes[0].RectTransform.anchoredPosition.x;
                float lastChildX = childNodes[^1].RectTransform.anchoredPosition.x;
                posX = (firstChildX + lastChildX) * 0.5f;
            }
            else
            {
                posX = padding.x + (currentLeafIndex * nodeSpacingX);
                currentLeafIndex++;
            }

            float posY = padding.y + (depth * nodeSpacingY);

            maxPosX = Mathf.Max(maxPosX, posX);
            maxPosY = Mathf.Max(maxPosY, posY);

            WeaponNodeUI currentNode = Instantiate(nodePrefab, nodeContainer);

            currentNode.Setup(weapon, spriteLoader).Forget();

            currentNode.name = $"Node_{weapon.weaponName}";
            currentNode.RectTransform.anchoredPosition = new Vector2(posX, posY);

            nodes[weapon.Id] = currentNode;

            // 무기 노드 클릭 시 레시피 변경 이벤트 발생함
            currentNode.OnClickAsObservable().
            ThrottleFirst(TimeSpan.FromMilliseconds(100)).
            Subscribe(e =>
            {
                _onChangeRecipeFlagEventSubject.OnNext(new ChangeRecipeFlagEvent { _recipeId = weapon.Id });
            }).AddTo(_disposables);

            // 자식 노드 연결
            foreach (var childUI in childNodes)
            {
                WeaponTreeBranchView branch = Instantiate(branchPrefab, branchContainer);
                branch.name = $"Branch_{weapon.weaponName}->{childUI.WeaponData.weaponName}";
                branch.SetNodes(currentNode.RectTransform, childUI.RectTransform);
                branch.SetHighlight(false);
                _branches[(weapon.Id, childUI.WeaponData.Id)] = branch;
            }

            return currentNode;
        }
        /// <summary>
        /// 스크롤 위치 초기화
        /// </summary>
        /// <returns></returns>
        private async UniTask ResetScrollPositionRoutine()
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
            if(scrollRect != null)
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