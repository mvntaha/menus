using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathDungeon.Menus
{
    /// <summary>
    /// Slow emissive pulse for a carved rune. Each one gets a random phase so
    /// the wall breathes unevenly rather than blinking in unison.
    ///
    /// On a TMP text this drives the underlay colour, which is the glow the
    /// builder sets up — driving the text colour instead would fade the letters
    /// themselves rather than their halo. Falls back to plain graphic colour for
    /// anything that is not TMP.
    ///
    /// Runs on unscaled time, so the pause menu keeps breathing while
    /// Time.timeScale is 0.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class RuneGlow : MonoBehaviour
    {
        [SerializeField] float minAlpha = 0.16f;
        [SerializeField] float maxAlpha = 0.55f;
        [SerializeField] float period = 6f;
        [SerializeField] bool randomisePhase = true;

        Graphic graphic;
        TMP_Text text;
        Material material;
        float phase;

        void Awake()
        {
            graphic = GetComponent<Graphic>();
            text = GetComponent<TMP_Text>();

            // fontMaterial returns this text's own material instance, so the
            // pulse never leaks into every other text sharing the font asset.
            if (text != null) material = text.fontMaterial;

            phase = randomisePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
        }

        void Update()
        {
            if (period <= 0f) return;

            float t = (Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / period) + phase) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

            if (material != null)
            {
                var c = material.GetColor(ShaderUtilities.ID_UnderlayColor);
                c.a = alpha;
                material.SetColor(ShaderUtilities.ID_UnderlayColor, c);
            }
            else if (graphic != null)
            {
                var c = graphic.color;
                c.a = alpha;
                graphic.color = c;
            }
        }
    }
}
