using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

namespace ElementalBlacksmithStory
{
    public class RuntimeDrawCallLogger : MonoBehaviour
    {
        private static readonly string LogPath = "Temp/RuntimeDrawCalls.txt";
        private float _timer = 0f;

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer >= 1.0f)
            {
                _timer = 0f;
                LogStats();
            }
        }

        private void LogStats()
        {
            try
            {
                // Active cameras and graphics stats
                var cam = Camera.main;
                string camName = cam != null ? cam.name : "None";

                // Count active graphics and sprite renderers
                var spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                var graphics = FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

                string content = $"[Runtime Render Info]\n" +
                                 $"Time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                 $"MainCamera: {camName}\n" +
                                 $"Active SpriteRenderers: {spriteRenderers.Length}\n" +
                                 $"Active UI Graphics (Image/Text): {graphics.Length}\n" +
                                 $"Active Canvases: {canvases.Length}\n";

                File.WriteAllText(LogPath, content);
            }
            catch (System.Exception ex)
            {
                File.WriteAllText(LogPath, $"Error: {ex.Message}");
            }
        }
    }
}
