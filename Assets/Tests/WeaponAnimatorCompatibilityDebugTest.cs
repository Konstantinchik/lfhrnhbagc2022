using UnityEngine;
using DarkTreeFPS;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

namespace DarkTreeFPS.Tests
{
    /// <summary>
    /// Scene-level regression check for the Shotgun Use parameter and Barrett aimed-shot fallback.
    /// Reads animator metadata only; it does not activate weapons or change animator state.
    /// </summary>
    public sealed class WeaponAnimatorCompatibilityDebugTest : MonoBehaviour
    {
#if UNITY_EDITOR
        private void Start()
        {
            Debug.Log("[Weapon animator test] TEST START. Console file: " +
                UnityConsoleFileCapture.LogPath, this);

            var shotgun = FindWeaponAnimator("Shotgun");
            var barrett = FindWeaponAnimator("Barret");
            var weaponHolder = GameObject.Find("Weapon holder");

            bool shotgunFound = shotgun != null;
            bool barrettFound = barrett != null;
            bool holderFound = weaponHolder != null && weaponHolder.GetComponent<Animator>() != null;
            bool shotgunUseAbsent = shotgunFound && !ControllerHasParameter(
                shotgun, "Use", AnimatorControllerParameterType.Bool);
            bool barrettUsePresent = barrettFound && ControllerHasParameter(
                barrett, "Use", AnimatorControllerParameterType.Bool);
            bool barrettShotPresent = barrettFound && ControllerHasState(barrett, "Shot");
            bool holderAimShotAbsent = holderFound && !ControllerHasState(
                weaponHolder.GetComponent<Animator>(), "AimShot");

            if (shotgunUseAbsent && barrettUsePresent && barrettShotPresent && holderAimShotAbsent)
            {
                Debug.Log("[Weapon animator test] PASS: Shotgun has no Use bool (guard required); " +
                    "Barrett has Use and Shot; shared Weapon holder has no AimShot (fallback required).", this);
                Debug.Log("[Weapon animator test] TEST END: PASS.", this);
            }
            else
            {
                Debug.LogWarning("[Weapon animator test] FAIL: shotgunFound=" + shotgunFound +
                    ", shotgunUseAbsent=" + shotgunUseAbsent +
                    ", barrettFound=" + barrettFound +
                    ", barrettUsePresent=" + barrettUsePresent +
                    ", barrettShotPresent=" + barrettShotPresent +
                    ", holderFound=" + holderFound +
                    ", holderAimShotAbsent=" + holderAimShotAbsent + ".", this);
                Debug.LogWarning("[Weapon animator test] TEST END: FAIL.", this);
            }
        }

        private static Animator FindWeaponAnimator(string weaponName)
        {
            var weapons = FindObjectsOfType<Weapon>(true);
            foreach (var weapon in weapons)
            {
                if (weapon.weaponName == weaponName)
                    return weapon.GetComponent<Animator>();
            }

            return null;
        }

        private static AnimatorController GetAnimatorController(Animator target)
        {
            if (target == null)
                return null;

            var runtimeController = target.runtimeAnimatorController;
            if (runtimeController is AnimatorOverrideController overrideController)
                runtimeController = overrideController.runtimeAnimatorController;

            return runtimeController as AnimatorController;
        }

        private static bool ControllerHasParameter(Animator target, string parameterName,
            AnimatorControllerParameterType parameterType)
        {
            var controller = GetAnimatorController(target);
            if (controller == null)
                return false;

            foreach (var parameter in controller.parameters)
            {
                if (parameter.name == parameterName && parameter.type == parameterType)
                    return true;
            }

            return false;
        }

        private static bool ControllerHasState(Animator target, string stateName)
        {
            var controller = GetAnimatorController(target);
            if (controller == null)
                return false;

            foreach (var layer in controller.layers)
            {
                if (StateMachineHasState(layer.stateMachine, stateName))
                    return true;
            }

            return false;
        }

        private static bool StateMachineHasState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == stateName)
                    return true;
            }

            foreach (var childMachine in stateMachine.stateMachines)
            {
                if (StateMachineHasState(childMachine.stateMachine, stateName))
                    return true;
            }

            return false;
        }
#endif
    }
}
