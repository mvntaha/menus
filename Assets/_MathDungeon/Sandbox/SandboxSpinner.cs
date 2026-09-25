using UnityEngine;

namespace MathDungeon.Sandbox
{
    /// <summary>
    /// Spins a cube so the pause sandbox has something that visibly stops.
    ///
    /// Deliberately uses scaled Time.deltaTime: the whole point is to show that
    /// Time.timeScale = 0 actually freezes gameplay while the menu itself keeps
    /// animating on unscaled time.
    ///
    /// This whole Sandbox folder is a test harness. Delete it after the Techwiz
    /// integration if you don't want it in the project.
    /// </summary>
    public class SandboxSpinner : MonoBehaviour
    {
        [SerializeField] Vector3 degreesPerSecond = new Vector3(20f, 45f, 15f);

        void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
