using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MathDungeon.Menus
{
    /// <summary>
    /// Hover and click sounds for a Selectable, played through a shared
    /// AudioSource so a menu full of buttons does not spawn an AudioSource each.
    ///
    /// Uses PlayOneShot with ignoreListenerPause on the source, so menu clicks
    /// are still audible while the game is paused and AudioListener.pause is on.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class UIButtonSfx : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] AudioSource source;
        [SerializeField] AudioClip hoverClip;
        [SerializeField] AudioClip clickClip;
        [Range(0f, 1f)]
        [SerializeField] float hoverVolume = 0.35f;
        [Range(0f, 1f)]
        [SerializeField] float clickVolume = 0.7f;

        Selectable selectable;

        void Awake() => selectable = GetComponent<Selectable>();

        public void OnPointerEnter(PointerEventData eventData) => Play(hoverClip, hoverVolume);

        public void OnPointerClick(PointerEventData eventData) => Play(clickClip, clickVolume);

        void Play(AudioClip clip, float volume)
        {
            if (clip == null || source == null) return;
            if (selectable != null && !selectable.IsInteractable()) return;
            source.PlayOneShot(clip, volume);
        }
    }
}
