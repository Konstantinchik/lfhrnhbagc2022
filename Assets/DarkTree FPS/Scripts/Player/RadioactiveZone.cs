using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DarkTreeFPS
{
    public class RadioactiveZone : MonoBehaviour
    {
        public GameObject heigerSource;
        public GameObject heigerSourceHi;

        public Transform player;

        public float lowRadDistance = 15f;
        public float highRadDistance = 5f;
        
        PlayerStats playerStats;

        private void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
            playerStats = FindObjectOfType<PlayerStats>();
        }

        public float damageLow = 0.5f;
        public float damageHi = 2f;

        public float damage = 1;

        public float timeToDamage;
        private float timer;
        private float accumulatedDamage;

        private void Update()
        {
            if (player == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject == null)
                    return;

                player = playerObject.transform;
            }

            if (playerStats == null)
                playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats == null)
                return;

            if (timeToDamage <= 0)
                return;

            if (Time.time > timer + timeToDamage)
            {
                ApplyDamageTick(playerStats);
                timer = Time.time;
            }
        }

        private void ApplyDamageTick(PlayerStats target)
        {
            if (target == null)
                return;

            int damageToApply = CalculateDamageToApply(damage, ref accumulatedDamage);
            if (damageToApply <= 0)
                return;

            int previousHealth = target.health;
            target.health -= damageToApply;
#if UNITY_EDITOR
            Debug.Log("[Radiation] " + gameObject.name + " applied " + damageToApply + " HP damage (" + previousHealth + " -> " + target.health + ").", this);
#endif
        }

        internal static int CalculateDamageToApply(float damage, ref float accumulatedDamage)
        {
            if (damage <= 0)
                return 0;

            accumulatedDamage += damage;
            int damageToApply = Mathf.FloorToInt(accumulatedDamage);
            if (damageToApply <= 0)
                return 0;

            accumulatedDamage -= damageToApply;
            return damageToApply;
        }

        private void LateUpdate()
        {
            if (player != null)
            {
                if (Vector3.Distance(transform.position, player.position) < highRadDistance)
                {
                    heigerSourceHi.SetActive(true);
                    heigerSource.SetActive(false);
                    damage = damageHi;
                }
                else if (Vector3.Distance(transform.position, player.position) < lowRadDistance || Vector3.Distance(transform.position, player.position) > highRadDistance)
                {
                    heigerSource.SetActive(true);
                    heigerSourceHi.SetActive(false);
                    damage = damageLow;
                }
                if (Vector3.Distance(transform.position, player.position) > lowRadDistance)
                {
                    heigerSource.SetActive(false);
                    heigerSourceHi.SetActive(false);
                    damage = 0;
                }
            }
        }
    }
}
