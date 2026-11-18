using UnityEngine;

namespace DarkTreeFPS.Tests
{
    /// <summary>
    /// Persistent event-driven test harness for the real Anomaly trigger on the scene's Dummy.
    /// It does not synthesize trigger callbacks or alter gameplay objects.
    /// </summary>
    public sealed class AnomalyZoneDebugTest : MonoBehaviour
    {
#if UNITY_EDITOR
        private const int ExpectedDamage = 20;
        private bool completed;
        private bool started;

        private void OnEnable()
        {
            Anomaly.PlayerTriggerObserved += OnPlayerTriggerObserved;
        }

        private void Start()
        {
            started = true;
            Debug.Log("[Anomaly test] TEST START. Waiting for the real Player trigger. Console file: " +
                UnityConsoleFileCapture.LogPath, this);
            if (!string.IsNullOrEmpty(UnityConsoleFileCapture.CaptureFailure))
                Debug.LogError("[Anomaly test] Console file capture failed: " +
                    UnityConsoleFileCapture.CaptureFailure, this);
        }

        private void OnDisable()
        {
            Anomaly.PlayerTriggerObserved -= OnPlayerTriggerObserved;
            if (started && !completed)
                Debug.LogWarning("[Anomaly test] NO TRIGGER OBSERVED: Play Mode ended before the player entered an anomaly zone.", this);
        }

        private void OnPlayerTriggerObserved(Anomaly zone, Collider playerCollider,
            int healthBefore, int healthAfter, bool cameraKickStateAvailable, bool reactionAudioAvailable)
        {
            if (completed)
                return;

            completed = true;
            int damageApplied = healthBefore - healthAfter;
            bool damagePassed = damageApplied == ExpectedDamage;
            bool cameraPassed = cameraKickStateAvailable;
            bool audioPassed = reactionAudioAvailable;

            if (damagePassed && cameraPassed && audioPassed)
            {
                Debug.Log("[Anomaly test] PASS: zone=" + zone.name +
                    " collider=" + playerCollider.name +
                    " damage=" + damageApplied +
                    " cameraKickStateAvailable=" + cameraPassed +
                    " reactionAudioAssigned=" + audioPassed + ".", this);
                Debug.Log("[Anomaly test] TEST END: PASS.", this);
            }
            else
            {
                Debug.LogError("[Anomaly test] FAIL: zone=" + zone.name +
                    " collider=" + playerCollider.name +
                    " expectedDamage=" + ExpectedDamage +
                    " actualDamage=" + damageApplied +
                    " cameraKickStateAvailable=" + cameraPassed +
                    " reactionAudioAssigned=" + audioPassed + ".", this);
                Debug.LogError("[Anomaly test] TEST END: FAIL.", this);
            }
        }
#endif
    }
}
