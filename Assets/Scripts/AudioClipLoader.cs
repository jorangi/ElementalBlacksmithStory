using VContainer;
using VContainer.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Cysharp.Threading.Tasks;
using R3;
using MessagePipe;
using ElementalBlacksmithStory.Events;

namespace ElementalBlacksmithStory.Core
{
    public class AudioClipLoader : MonoBehaviour
    {
        private readonly UniTaskCompletionSource _loadTcs = new();
        private bool _isLoaded = false;
        private readonly Dictionary<uint, AudioClip> _audioClips = new();
        private readonly List<AsyncOperationHandle<AudioClip>> _loadedHandles = new();
        private readonly CompositeDisposable _disposables = new();
        private ISubscriber<PlaySoundEvent> _subscriber;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioSource _loopSource;
        private float _bgmVolume = 1f;
        private float _sfxVolume = 1f;

        public void SetBGMVolume(float volume)
        {
            _bgmVolume = Mathf.Clamp01(volume);
            if (_loopSource != null)
            {
                _loopSource.volume = _bgmVolume;
            }
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            if (_audioSource != null)
            {
                _audioSource.volume = _sfxVolume;
            }
        }

        private void Awake()
        {
            LoadAudioClipsAsync().Forget();
        }
        [Inject]
        public void Construct(ISubscriber<PlaySoundEvent> subscriber)
        {
            _subscriber = subscriber;
            _subscriber.Subscribe(e =>
            {
                PlaySoundAsync(e).Forget();
            }).AddTo(_disposables);
        }

        private async UniTaskVoid PlaySoundAsync(PlaySoundEvent e)
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            var audioClip = await GetAudioClipAsync(e.Id, cancellationToken);
            if (audioClip == null) return;

            if (e._isLoop)
            {
                if (_loopSource == null)
                {
                    _loopSource = gameObject.AddComponent<AudioSource>();
                    _loopSource.playOnAwake = false;
                }
                _loopSource.clip = audioClip;
                _loopSource.loop = true;
                _loopSource.volume = _bgmVolume;
                _loopSource.Play();
            }
            else
            {
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                    _audioSource.playOnAwake = false;
                }
                _audioSource.PlayOneShot(audioClip, _sfxVolume);
            }
        }

        private async UniTask LoadAudioClipsAsync()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            try
            {
                var _handle = Addressables.LoadResourceLocationsAsync("Sounds", typeof(AudioClip));
                IList<IResourceLocation> locations = await _handle.ToUniTask(cancellationToken: cancellationToken);
                var loadTasks = locations.Select(async loc =>
                {
                    if(uint.TryParse(loc.PrimaryKey, out uint id))
                    {
                        var handle = Addressables.LoadAssetAsync<AudioClip>(loc);
                        _loadedHandles.Add(handle);
                        AudioClip clip = await handle.ToUniTask(cancellationToken: cancellationToken);
                        _audioClips[id] = clip;
                    }
                    else
                    {
                        Debug.LogWarning($"[AudioClipLoader] 주소(Address)를 숫자로 변환할 수 없습니다: '{loc.PrimaryKey}'");
                    }
                });
                await UniTask.WhenAll(loadTasks);
                Debug.Log($"[AudioClipLoader] AudioClip 로드 완료: 총 {_audioClips.Count}개");
                _isLoaded = true;
                _loadTcs.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[AudioClipLoader] 로딩 취소됨");
                _loadTcs.TrySetCanceled();
            }
            catch(Exception ex)
            {
                Debug.LogError($"[AudioClipLoader] AudioClip 로드 실패: {ex.Message}");
                _loadTcs.TrySetException(ex);
            }
        }

        public async UniTask<AudioClip> GetAudioClipAsync(uint id, CancellationToken cancellationToken = default)
        {
            if (_audioClips.TryGetValue(id, out AudioClip audioClip))
            {
                return audioClip;
            }

            if (!_isLoaded)
            {
                try
                {
                    await _loadTcs.Task.AttachExternalCancellation(cancellationToken);
                    if (_audioClips.TryGetValue(id, out audioClip))
                    {
                        return audioClip;
                    }
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            }

            // Fallback: 개별 키로 Lazy 로드 시도
            try
            {
                var handle = Addressables.LoadAssetAsync<AudioClip>(id.ToString());
                _loadedHandles.Add(handle);
                audioClip = await handle.ToUniTask(cancellationToken: cancellationToken);
                if (audioClip != null)
                {
                    _audioClips[id] = audioClip;
                    return audioClip;
                }
            }
            catch { }

            Debug.LogError($"[AudioClipLoader] {id} 해당하는 오디오 클립이 없습니다.");
            return null;
        }

        public AudioClip GetAudioClip(uint id)
        {
            if(_audioClips.TryGetValue(id, out AudioClip audioClip))
            {
                return audioClip;
            }
            Debug.LogError($"[AudioClipLoader] {id} 해당하는 오디오 클립이 없습니다.");
            return null;
        }
        private void OnDestroy()
        {
            foreach (var handle in _loadedHandles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _loadedHandles.Clear();
            _audioClips.Clear();
            _disposables.Dispose();
        }
    }
}