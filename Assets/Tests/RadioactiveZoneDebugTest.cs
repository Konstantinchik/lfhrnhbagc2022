using UnityEngine;

namespace DarkTreeFPS.Tests
{
    /// <summary>
    /// Persistent test component for the scene's designated Dummy object.
    /// Test execution is Editor-only; in player builds this component is inert.
    /// </summary>
    public sealed class RadioactiveZoneDebugTest : MonoBehaviour
    {
        private void Start()
        {
#if UNITY_EDITOR
            RunDamageAlgorithmTest();
#endif
        }

#if UNITY_EDITOR
        private void RunDamageAlgorithmTest()
        {
            Debug.Log("[RadioactiveZone test] TEST START. Console file: " + UnityConsoleFileCapture.LogPath, this);
            if (!string.IsNullOrEmpty(UnityConsoleFileCapture.CaptureFailure))
                Debug.LogError("[RadioactiveZone test] Console file capture failed: " + UnityConsoleFileCapture.CaptureFailure, this);

            float accumulatedDamage = 0f;
            int health = 100;

            health -= RadioactiveZone.CalculateDamageToApply(0.5f, ref accumulatedDamage);
            bool firstHalfPointKept = health == 100 && Mathf.Approximately(accumulatedDamage, 0.5f);

            health -= RadioactiveZone.CalculateDamageToApply(0.5f, ref accumulatedDamage);
            bool secondHalfPointApplied = health == 99 && Mathf.Approximately(accumulatedDamage, 0f);

            health -= RadioactiveZone.CalculateDamageToApply(2.5f, ref accumulatedDamage);
            bool wholeDamageApplied = health == 97 && Mathf.Approximately(accumulatedDamage, 0.5f);

            health -= RadioactiveZone.CalculateDamageToApply(0.5f, ref accumulatedDamage);
            bool fractionalRemainderRetained = health == 96 && Mathf.Approximately(accumulatedDamage, 0f);

            health -= RadioactiveZone.CalculateDamageToApply(0f, ref accumulatedDamage);
            bool zeroDamageIgnored = health == 96;

            health -= RadioactiveZone.CalculateDamageToApply(-1f, ref accumulatedDamage);
            bool negativeDamageIgnored = health == 96;

            if (firstHalfPointKept && secondHalfPointApplied && wholeDamageApplied
                && fractionalRemainderRetained && zeroDamageIgnored && negativeDamageIgnored)
            {
                Debug.Log("[RadioactiveZone test] PASS: fractional accumulation, whole damage, and non-positive damage checks passed.", this);
                Debug.Log("[RadioactiveZone test] TEST END.", this);
            }
            else
            {
                Debug.LogError(
                    "[RadioactiveZone test] FAIL: " +
                    "firstHalfPointKept=" + firstHalfPointKept + ", " +
                    "secondHalfPointApplied=" + secondHalfPointApplied + ", " +
                    "wholeDamageApplied=" + wholeDamageApplied + ", " +
                    "fractionalRemainderRetained=" + fractionalRemainderRetained + ", " +
                    "zeroDamageIgnored=" + zeroDamageIgnored + ", " +
                    "negativeDamageIgnored=" + negativeDamageIgnored + ".",
                    this);
                Debug.LogError("[RadioactiveZone test] TEST END: FAIL.", this);
            }
        }
#endif
    }
}
