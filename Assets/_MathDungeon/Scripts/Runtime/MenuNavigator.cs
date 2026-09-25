using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathDungeon.Menus
{
    /// <summary>
    /// Scene loading for the menus. Kept in one place because this project has
    /// no gameplay scenes in it — the menus are built here and imported into
    /// Techwiz, where the real scene names live.
    ///
    /// Every load is guarded: if a scene is not in Build Settings, this logs a
    /// clear error naming the missing scene instead of throwing an opaque
    /// ArgumentException from SceneManager. That turns "the Play button does
    /// nothing" into an actionable message during the Techwiz integration.
    /// </summary>
    public static class MenuNavigator
    {
        public static bool TryLoad(string sceneName, Object context = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[MathDungeon] Scene name is empty. Set it in the Inspector.", context);
                return false;
            }

            if (!IsInBuildSettings(sceneName))
            {
                Debug.LogError(
                    $"[MathDungeon] Scene '{sceneName}' is not in Build Settings, so it cannot be loaded. " +
                    "Add it under File > Build Profiles > Scene List, or correct the name on this component.",
                    context);
                return false;
            }

            Time.timeScale = 1f;   // never carry a paused clock into a new scene
            SceneManager.LoadScene(sceneName);
            return true;
        }

        public static bool IsInBuildSettings(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(path), sceneName,
                        System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
