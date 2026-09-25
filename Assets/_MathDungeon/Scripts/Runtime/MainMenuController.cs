using UnityEngine;

namespace MathDungeon.Menus
{
    /// <summary>
    /// Drives the main menu: swaps between the root button column and the
    /// settings panel, and hands scene loads to <see cref="MenuNavigator"/>.
    /// Wire the buttons' OnClick to the public methods here.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scenes")]
        [Tooltip("Name of the gameplay scene in Techwiz. Must be in Build Settings.")]
        [SerializeField] string gameplayScene = "Gameplay";
        [SerializeField] string levelSelectScene = "LevelSelect";

        [Header("Panels")]
        [SerializeField] CanvasGroup rootPanel;
        [SerializeField] CanvasGroup settingsPanel;

        [Header("Platform")]
        [Tooltip("Quit does nothing on WebGL, so the button is hidden there instead of lying to the player.")]
        [SerializeField] GameObject quitButton;

        void Start()
        {
            Time.timeScale = 1f;   // in case we arrived here from a paused game
            Show(rootPanel, true);
            Show(settingsPanel, false);

            if (quitButton != null && Application.platform == RuntimePlatform.WebGLPlayer)
                quitButton.SetActive(false);
        }

        public void OnPlay()        => MenuNavigator.TryLoad(gameplayScene, this);
        public void OnLevelSelect() => MenuNavigator.TryLoad(levelSelectScene, this);
        public void OnQuit()        => MenuNavigator.QuitGame();

        public void OnOpenSettings()
        {
            Show(rootPanel, false);
            Show(settingsPanel, true);
        }

        public void OnCloseSettings()
        {
            Show(settingsPanel, false);
            Show(rootPanel, true);
        }

        static void Show(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
