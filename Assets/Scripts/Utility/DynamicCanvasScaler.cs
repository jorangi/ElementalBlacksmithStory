using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
[ExecuteAlways]
public class DynamicCanvasScaler : MonoBehaviour
{
    [Header("기준 해상도 (기본 9:16)")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080, 1920);

    private CanvasScaler _scaler;
    private Vector2 _lastScreenSize;

    private void Awake()
    {
        _scaler = GetComponent<CanvasScaler>();
        UpdateScaler();
    }

    private void OnEnable()
    {
        UpdateScaler();
    }

    // 폰을 접거나 펼치거나 해상도/창 크기가 변경될 때 자동 호출
    private void OnRectTransformDimensionsChange()
    {
        UpdateScaler();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _lastScreenSize = Vector2.zero;
        UpdateScaler();
    }
#endif

    public void UpdateScaler()
    {
        if (_scaler == null) _scaler = GetComponent<CanvasScaler>();
        if (_scaler == null) return;

        Vector2 currentScreen = new Vector2(Screen.width, Screen.height);
        if (currentScreen == _lastScreenSize || currentScreen.x <= 0 || currentScreen.y <= 0) return;
        _lastScreenSize = currentScreen;

        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _scaler.referenceResolution = referenceResolution;

        float refAspect = referenceResolution.x / referenceResolution.y; // 1080 / 1920 ≈ 0.5625
        float currentAspect = (float)Screen.width / Screen.height;

        if (currentAspect > refAspect)
        {
            // 펼쳤을 때 (4:3, 1:1 정사각형, 16:9, 21:9 등 가로/와이드 비율) -> 세로 높이 고정 (가로폭 확장)
            _scaler.matchWidthOrHeight = 1.0f;
        }
        else
        {
            // 접었을 때 (21:9, 23:9 등 초세로형 비율) -> 가로 폭 고정
            _scaler.matchWidthOrHeight = 0.0f;
        }
    }
}

/// <summary>
/// 이전 버전 클래스명 호환성을 위한 별칭 클래스
/// </summary>
public class FoldableCanvasScaler : DynamicCanvasScaler
{
}