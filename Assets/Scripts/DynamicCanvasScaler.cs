using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
[ExecuteAlways]
public class FoldableCanvasScaler : MonoBehaviour
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

    // 폰을 접거나 펼쳐서 캔버스 크기가 변경될 때 자동 호출
    private void OnRectTransformDimensionsChange()
    {
        UpdateScaler();
    }

    private void UpdateScaler()
    {
        if (_scaler == null) _scaler = GetComponent<CanvasScaler>();

        Vector2 currentScreen = new Vector2(Screen.width, Screen.height);
        if (currentScreen == _lastScreenSize || currentScreen.x <= 0 || currentScreen.y <= 0) return;
        _lastScreenSize = currentScreen;

        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _scaler.referenceResolution = referenceResolution;

        float refAspect = referenceResolution.x / referenceResolution.y; // 1080 / 1920 ≈ 0.5625
        float currentAspect = (float)Screen.width / Screen.height;

        if (currentAspect > refAspect)
        {
            // 펼쳤을 때 (4:3, 1:1에 가까운 정사각형/태블릿 비율) -> 세로 높이 고정
            _scaler.matchWidthOrHeight = 1.0f;
        }
        else
        {
            // 접었을 때 (21:9, 23:9 초세로형) -> 가로 폭 고정
            _scaler.matchWidthOrHeight = 0.0f;
        }
    }
}