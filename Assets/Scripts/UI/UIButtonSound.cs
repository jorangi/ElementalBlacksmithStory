using UnityEngine;
using UnityEngine.UI;
using VContainer;
using MessagePipe;
using ElementalBlacksmithStory.Events;

namespace ElementalBlacksmithStory.UI
{
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour
    {
        [SerializeField] private uint _soundId = 40106;
        [SerializeField] private bool _autoBindOnClick = true;

        [Inject] private readonly IPublisher<PlaySoundEvent> _sfxPublisher;
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_autoBindOnClick && _button != null)
            {
                _button.onClick.AddListener(PlaySound);
            }
        }

        public void SetAutoBindOnClick(bool autoBind)
        {
            if (_autoBindOnClick == autoBind) return;
            _autoBindOnClick = autoBind;
            if (_button != null)
            {
                if (_autoBindOnClick)
                    _button.onClick.AddListener(PlaySound);
                else
                    _button.onClick.RemoveListener(PlaySound);
            }
        }

        public void SetSoundId(uint soundId)
        {
            _soundId = soundId;
        }

        private void OnDestroy()
        {
            if (_autoBindOnClick && _button != null)
            {
                _button.onClick.RemoveListener(PlaySound);
            }
        }

        public void PlaySound()
        {
            PlaySound(_soundId);
        }

        public void PlaySound(uint soundId)
        {
            var publisher = _sfxPublisher ?? (GlobalMessagePipe.IsInitialized ? GlobalMessagePipe.GetPublisher<PlaySoundEvent>() : null);
            if (publisher != null)
            {
                publisher.Publish(new PlaySoundEvent(soundId));
            }
            else
            {
                Debug.LogWarning($"[UIButtonSound] PlaySoundEvent Publisher를 찾을 수 없습니다. (soundId: {soundId})");
            }
        }

        public void PlaySound(int soundId)
        {
            PlaySound((uint)soundId);
        }
    }
}
