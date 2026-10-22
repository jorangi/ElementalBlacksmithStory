using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using R3;
using R3.Triggers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class RuneEquipView : MonoBehaviour
    {
        private const float ANIM_DURATION = 0.2f;
        [SerializeField] Image weaponImage;
        [SerializeField] Button[] runeSlots;
        [SerializeField] Image[] runeIcons;

        [SerializeField] Button applyButton;
        [SerializeField] Button cancelButton;
        /// <summary>
        /// 무기 이름
        /// </summary>
        [SerializeField] TextMeshProUGUI weaponName;
        /// <summary>
        /// 룬 착용 UI
        /// </summary>
        [SerializeField] RectTransform _runeEquipUI;
        /// <summary>
        /// 룬 전체 UI
        /// </summary>
        [SerializeField] RectTransform _runeUI;
        /// <summary>
        /// 말풍선 패널
        /// </summary>
        [SerializeField] RectTransform Bubble;
        /// <summary>
        /// 말풍선 텍스트
        /// </summary>
        [SerializeField] TextMeshProUGUI bubbleText;
        private CancellationTokenSource _cts;
        private MotionHandle motionHandle;
        [SerializeField] private Color rootPanelColor;
        private static readonly Color SELECTED_SLOT_COLOR = new(0.96f, 0.49f, 0.49f);

        public Observable<Unit> OnRuneSlotButtonAsObservable(int index)
        {
            if (runeSlots == null || index < 0 || index >= runeSlots.Length || runeSlots[index] == null)
            {
                return Observable.Empty<Unit>();
            }
            return runeSlots[index].OnClickAsObservable();
        }

        private Subject<Unit>[] _runeSlotRightClickSubjects;

        private void InitializeSlotRightClickEvents()
        {
            if (_runeSlotRightClickSubjects != null || runeSlots == null) return;

            _runeSlotRightClickSubjects = new Subject<Unit>[runeSlots.Length];
            for (int i = 0; i < runeSlots.Length; i++)
            {
                _runeSlotRightClickSubjects[i] = new Subject<Unit>();
                int index = i;
                if (runeSlots[i] != null)
                {
                    runeSlots[i].OnPointerClickAsObservable()
                        .Subscribe(e =>
                        {
                            if (e.button == PointerEventData.InputButton.Right)
                            {
                                _runeSlotRightClickSubjects[index].OnNext(Unit.Default);
                            }
                        })
                        .AddTo(this);
                }
            }
        }

        public Observable<Unit> OnRuneSlotRightClickAsObservable(int index)
        {
            InitializeSlotRightClickEvents();
            if (_runeSlotRightClickSubjects == null || index < 0 || index >= _runeSlotRightClickSubjects.Length)
            {
                return Observable.Empty<Unit>();
            }
            return _runeSlotRightClickSubjects[index];
        }

        public void SelectSocket(int index)
        {
            if (runeSlots == null) return;

            for (int i = 0; i < runeSlots.Length; i++)
            {
                if (runeSlots[i] != null && runeSlots[i].image != null)
                {
                    runeSlots[i].image.color = (i == index) ? SELECTED_SLOT_COLOR : Color.white;
                }
            }
        }
        public Observable<Unit> OnApplyButtonAsObservable()
        {
            if (applyButton != null)
            {
                return applyButton.OnClickAsObservable();
            }
            return Observable.Empty<Unit>();
        }
        public Observable<Unit> OnCancelButtonAsObservable()
        {
            if (cancelButton != null)
            {
                return cancelButton.OnClickAsObservable();
            }
            return Observable.Empty<Unit>();
        }
        public void SetWeapon(Sprite sprite, string weaponName)
        {
            this.weaponImage.sprite = sprite;
            this.weaponName.SetText(weaponName);
            this.weaponName.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)this.weaponName.transform.parent);
        }
        public async UniTask Show(CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);

            if (motionHandle.IsActive())
            {
                motionHandle.Cancel();
            }

            _runeUI.gameObject.SetActive(true);
            _runeEquipUI.gameObject.SetActive(true);

            motionHandle = LMotion.Create(Vector3.zero, Vector3.one, ANIM_DURATION)
                            .WithEase(Ease.InOutBack)
                            .BindToLocalScale(_runeUI)
                            .AddTo(gameObject);

            await motionHandle.ToUniTask(_cts.Token)
                            .SuppressCancellationThrow();
        }
        public async UniTask Hide(CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);

            if (motionHandle.IsActive())
            {
                motionHandle.Cancel();
            }

            motionHandle = LMotion.Create(Vector3.one, Vector3.zero, ANIM_DURATION)
                            .WithEase(Ease.InOutBack)
                            .BindToLocalScale(_runeUI)
                            .AddTo(gameObject);

            await motionHandle.ToUniTask(_cts.Token)
                            .SuppressCancellationThrow();

            if (_cts.IsCancellationRequested) return;

            _runeEquipUI.gameObject.SetActive(false);
            _runeUI.gameObject.SetActive(false);
        }
        public void SetDialogue(string text)
        {
            bubbleText.SetText(text);
        }
        public void SetSocketCount(int socketCount)
        {
            for (int i = 0; i < runeSlots.Length; i++)
            {
                runeSlots[i].gameObject.SetActive(i < socketCount);
            }
        }
        public void SetRuneSprite(int socketIndex, Sprite sprite)
        {
            if (socketIndex >= runeIcons.Length)
            {
                return;
            }
            runeIcons[socketIndex].enabled = sprite != null;
            runeIcons[socketIndex].sprite = sprite;
        }
        private void OnDestroy()
        {
            if (motionHandle.IsActive())
            {
                motionHandle.Cancel();
            }
            _cts?.Cancel();
            _cts?.Dispose();

            if (_runeSlotRightClickSubjects != null)
            {
                for (int i = 0; i < _runeSlotRightClickSubjects.Length; i++)
                {
                    _runeSlotRightClickSubjects[i]?.Dispose();
                }
            }
        }
    }
}