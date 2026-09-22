#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.Editor.Optimization
{
    public static class DrawCallOptimizationChecker
    {
        [MenuItem("Tools/Optimization/Check Raycast Targets")]
        public static void CheckRaycastTargets()
        {
            var graphics = Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int unneededCount = 0;

            foreach (var g in graphics)
            {
                // Button, Toggle, ScrollRect, InputField 등 상호작용 컴포넌트가 없는 경우 체크
                var isInteractive = g.GetComponent<Selectable>() != null;
                if (!isInteractive && g.raycastTarget)
                {
                    Debug.LogWarning($"[DrawCall-Raycast] '{g.gameObject.name}' has Raycast Target enabled without Selectable component.", g.gameObject);
                    unneededCount++;
                }
            }

            Debug.Log($"[DrawCall-Raycast Check] Complete. Found {unneededCount} unnecessary Raycast Targets in open scenes.");
        }

        [MenuItem("Tools/Optimization/Check 2D Sprite Z-Coordinates")]
        public static void CheckSpriteZPositions()
        {
            var renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int nonZeroZCount = 0;

            foreach (var r in renderers)
            {
                if (Mathf.Abs(r.transform.position.z) > 0.001f)
                {
                    Debug.LogWarning($"[DrawCall-SpriteZ] '{r.gameObject.name}' SpriteRenderer has non-zero Z position ({r.transform.position.z}), which may break 2D batching.", r.gameObject);
                    nonZeroZCount++;
                }
            }

            Debug.Log($"[DrawCall-SpriteZ Check] Complete. Found {nonZeroZCount} SpriteRenderers with Z != 0.");
        }
    }
}
#endif
