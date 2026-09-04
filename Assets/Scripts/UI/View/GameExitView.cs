using VContainer;
using VContainer.Unity;
using UnityEngine;
using UnityEngine.UI;
using R3;
using System;

namespace ElementalBlacksmithStory.UI
{
    public class GameExitView : MonoBehaviour
    {
        public GameObject gameExitPanel;
        public Button _button;

        public Observable<Unit> OnExitButtonAsObservable()
        {
            if(_button == null)
            {
                Debug.LogError("[GameExitView] 종료 확인 버튼이 등록되지 않았습니다.");
                return Observable.Empty<Unit>();
            }
            return _button.OnClickAsObservable();
        }
    }
}