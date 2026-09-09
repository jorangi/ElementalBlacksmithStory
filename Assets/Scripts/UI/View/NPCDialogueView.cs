using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R3;
using MessagePipe;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;
using System.Threading;
using LitMotion;
using LitMotion.Extensions;
using ElementalBlacksmithStory.Core;
using Cysharp.Text;

namespace ElementalBlacksmithStory.UI
{
    public class NPCDialogueView : MonoBehaviour
    {
        [SerializeField] private Button dialogueBox;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI cursorArrow;
        [SerializeField] private GameObject options;
        [SerializeField] Image standingImage;
        private CancellationTokenSource _cts;
        private float _initialY;
        private bool _isInitialized = false;
        private MotionHandle _floatingMotion;

        private void Awake()
        {
            if(cursorArrow != null)
            {
                _initialY = cursorArrow.rectTransform.anchoredPosition.y;
                _isInitialized = true;
            }
        }
        public bool IsActivated => dialogueBox != null && dialogueBox.gameObject.activeSelf;

        public void Show()
        {
            if (dialogueBox != null) dialogueBox.gameObject.SetActive(true);
            if (standingImage != null)
            {
                standingImage.transform.parent.gameObject.SetActive(true);
                standingImage.gameObject.SetActive(true);
            }
            FloatingCursor().Forget();
        }

        public void Hide()
        {
            _cts?.Cancel();
            if (_floatingMotion.IsActive()) _floatingMotion.Cancel();
            if (dialogueBox != null) dialogueBox.gameObject.SetActive(false);
            if (standingImage != null)
            {
                standingImage.gameObject.SetActive(false);
                standingImage.transform.parent.gameObject.SetActive(false);
            }
            if (options != null) options.SetActive(false);
        }

        public Observable<Unit> OnClickDialogueBoxAsObservable()
        {
            if(dialogueBox == null)
            {
                return Observable.Empty<Unit>();
            }
            return dialogueBox.OnClickAsObservable();
        }
        public UniTask FloatingCursor(CancellationToken ct = default)
        {
            if(cursorArrow == null) return UniTask.CompletedTask;
            var rect = cursorArrow.rectTransform;
            if(!_isInitialized)
            {
                _initialY = rect.anchoredPosition.y;
                _isInitialized = true;
            }
            
            float offsetY = 10f;

            if(_floatingMotion.IsActive())
            {
                _floatingMotion.Cancel();
            }

            Vector2 pos = rect.anchoredPosition;
            pos.y = _initialY;
            rect.anchoredPosition = pos;

            _floatingMotion = LMotion.Create(_initialY, _initialY + offsetY, 0.6f)
                                .WithEase(Ease.InOutSine)
                                .WithLoops(-1, LoopType.Yoyo)
                                .BindToAnchoredPositionY(rect)
                                .AddTo(gameObject);

            return UniTask.CompletedTask;
        }
        public void SetNPC(string name, Sprite sprite)
        {
            nameText.SetText(name);
            if(sprite == null)
            {
                standingImage.color = Color.darkGray;
            }
            else
            {
                standingImage.color = Color.white;
                standingImage.sprite = sprite;
            }
        }
        public async UniTask TypingText(string text, CancellationToken ct = default)
        {
            if(dialogueText == null) return;
            
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            
            dialogueText.SetText(text);
            dialogueText.maxVisibleCharacters = 0;
            int totalLength = text.Length;

            for(int i = 0; i <= totalLength; i++)
            {
                dialogueText.maxVisibleCharacters = i;

                bool isCanceled = await 
                UniTask.Delay(TimeSpan.FromMilliseconds(10), cancellationToken:_cts.Token)
                .SuppressCancellationThrow();
                
                if(isCanceled) break;
            }
            dialogueText.maxVisibleCharacters = totalLength;
        }
        private void OnEnable()
        {
            FloatingCursor().Forget();
        }
        private void OnDisable()
        {
            if(_floatingMotion.IsActive()) _floatingMotion.Cancel();
        }
        private void OnDestroy()
        {
            if(_floatingMotion.IsActive()) _floatingMotion.Cancel();
        }
    }
}