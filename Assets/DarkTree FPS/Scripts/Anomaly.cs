using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkTreeFPS;

public class Anomaly : MonoBehaviour
{
    private Transform player;
    private AudioSource audioSource;
    private Animator headBobAnimator;

    public AudioClip anomalyEnterReaction;

#if UNITY_EDITOR
    internal static event Action<Anomaly, Collider, int, int, bool, bool> PlayerTriggerObserved;
#endif

    private void Start()
    {
        player = FindObjectOfType<DarkTreeFPS.FPSController>().transform;
        audioSource = GetComponent<AudioSource>();
        headBobAnimator = Camera.main.GetComponent<Animator>();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var playerStats = player.GetComponent<PlayerStats>();
            int healthBefore = playerStats.health;
            bool cameraKickAvailable = headBobAnimator != null
                && headBobAnimator.HasState(0, Animator.StringToHash("CameraKick"));
            bool reactionAudioAvailable = audioSource != null && anomalyEnterReaction != null;

            headBobAnimator.Play("CameraKick");
            playerStats.ApplyDamage(20);
            audioSource.PlayOneShot(anomalyEnterReaction);

            int healthAfter = playerStats.health;
#if UNITY_EDITOR
            Debug.Log("[Anomaly] TriggerEnter zone=" + name +
                " collider=" + other.name +
                " player=" + player.name +
                " health=" + healthBefore + "->" + healthAfter +
                " damage=" + (healthBefore - healthAfter) +
                " cameraKickState=" + cameraKickAvailable +
                " reactionAudio=" + reactionAudioAvailable + ".", this);
            try
            {
                PlayerTriggerObserved?.Invoke(this, other, healthBefore, healthAfter,
                    cameraKickAvailable, reactionAudioAvailable);
            }
            catch (Exception exception)
            {
                Debug.LogError("[Anomaly] Test observer failed: " + exception, this);
            }
#endif
        }

    }
}
