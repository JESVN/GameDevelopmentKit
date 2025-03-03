#if UNITY_HOTFIX
using ToolbarExtension;
using UnityEditor;
using UnityEngine;

namespace ET.Editor
{
    sealed class CompileDllToolBar_ET
    {
        private static readonly GUIContent s_BuildReloadHotfixButtonGUIContent = new GUIContent("ETReload", "Compile And Reload ET.Hotfix Dll When Playing.");
        private static readonly GUIContent s_BuildHotfixModelButtonGUIContent = new GUIContent("ETCompile", "Compile All ET Dll.");
        private static bool s_IsReloading = false;

        [Toolbar(OnGUISide.Left, 0)]
        static void OnToolbarGUI()
        {
            EditorGUI.BeginDisabledGroup(!Application.isPlaying || s_IsReloading);
            {
                if (GUILayout.Button(s_BuildReloadHotfixButtonGUIContent))
                {
                    BuildAssemblyTool.Build();
                    ShowNotification("compile success!");

                    if (s_IsReloading)
                        return;
                    s_IsReloading = true;

                    async void ReloadAsync()
                    {
                        try
                        {
                            await CodeLoaderComponent.Instance.LoadHotfixAsync();
                            Game.Load();
                            ShowNotification("reload hotfix success!");
                        }
                        finally
                        {
                            s_IsReloading = false;
                        }
                    }

                    ReloadAsync();
                }
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(s_BuildHotfixModelButtonGUIContent))
            {
                BuildAssemblyTool.Build();
                ShowNotification("compile success!");
            }
        }

        private static void ShowNotification(string msg)
        {
            EditorWindow game = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            if (game != null) game.ShowNotification(new GUIContent(msg));
        }
    }
}
#endif