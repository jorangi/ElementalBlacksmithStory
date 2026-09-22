#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.IO;

namespace ElementalBlacksmithStory.Editor.Optimization
{
    [InitializeOnLoad]
    public static class DrawCallLiveInspector
    {
        private static double lastCheckTime;
        private static readonly string LogPath = "Temp/DrawCallLiveStats.txt";

        static DrawCallLiveInspector()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - lastCheckTime < 1.0) return;
            lastCheckTime = EditorApplication.timeSinceStartup;

            DumpStats();
        }

        [MenuItem("Tools/Optimization/Dump Current Frame Stats")]
        public static void DumpStats()
        {
            try
            {
                int batches = UnityStats.batches;
                int setPassCalls = UnityStats.setPassCalls;
                int drawCalls = UnityStats.drawCalls;
                int triangles = UnityStats.triangles;
                int vertices = UnityStats.vertices;
                bool isPlaying = Application.isPlaying;

                string content = $"IsPlaying: {isPlaying}\n" +
                                 $"Batches: {batches}\n" +
                                 $"SetPassCalls: {setPassCalls}\n" +
                                 $"DrawCalls: {drawCalls}\n" +
                                 $"Triangles: {triangles}\n" +
                                 $"Vertices: {vertices}\n" +
                                 $"Time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                File.WriteAllText(LogPath, content);
            }
            catch (System.Exception ex)
            {
                File.WriteAllText(LogPath, $"Error: {ex.Message}");
            }
        }
    }
}
#endif
