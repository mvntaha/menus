using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MathDungeon.Menus
{
    /// <summary>
    /// Drop this prefab into any gameplay scene and pausing works — no wiring
    /// needed beyond the prefab itself.
    ///
    /// Reads the pause key under both input backends. Techwiz may be set to the
    /// new Input System, the legacy manager, or both, and calling the wrong API
    /// throws at runtime — so the call is compiled per backend rather than
    /// assumed.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] GameObject pausePanel;
        [SerializeField] CanvasGroup pauseRoot;
        [SerializeField] CanvasGroup settingsPanel;

        [Header("Scenes")]
        [SerializeField] string mainMenuScene = "MainMenu";

        [Header("Behaviour")]
        [Tooltip("Also pause audio, not just the clock.")]
        [SerializeField] bool pauseAudio = true;

        public bool IsPaused { get; private set; }

        void Awake()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            SetPanels(showSettings: false);

            // Without an EventSystem in the scene, uGUI receives no input at
            // all: the menu appears but nothing is clickable. That reads as a
            // broken prefab rather than a missing scene object, so say so.
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                Debug.LogWarning(
                    "[MathDungeon] No EventSystem in this scene, so the pause menu will not respond to " +
                    "clicks. Add one via GameObject > UI > Event System.", this);
            }
        }

        void Update()
        {
            if (PausePressedThisFrame())
            {
                if (IsPaused) Resume();
                else Pause();
            }
        }

        static bool PausePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;

            if (pausePanel != null) pausePanel.SetActive(true);
            SetPanels(showSettings: false);

            Time.timeScale = 0f;
            if (pauseAudio) AudioListener.pause = true;
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;

            if (pausePanel != null) pausePanel.SetActive(false);

            Time.timeScale = 1f;
            if (pauseAudio) AudioListener.pause = false;
        }

        public void Restart()
        {
            // Restore the clock BEFORE the load: a scene that loads at
            // timeScale 0 looks frozen and is a miserable bug to track down.
            Time.timeScale = 1f;
            AudioListener.pause = false;
            IsPaused = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ToMainMenu()
        {
            AudioListener.pause = false;
            IsPaused = false;
            MenuNavigator.TryLoad(mainMenuScene, this);   // restores timeScale itself
        }

        public void OnOpenSettings()  => SetPanels(showSettings: true);
        public void OnCloseSettings() => SetPanels(showSettings: false);

        void SetPanels(bool showSettings)
        {
            Show(pauseRoot, !showSettings);
            Show(settingsPanel, showSettings);
        }

        static void Show(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        // A disabled or destroyed pause menu must never leave the game frozen.
        void OnDisable()
        {
            if (!IsPaused) return;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            IsPaused = false;
        }
    }
}
