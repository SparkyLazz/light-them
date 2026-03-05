using System;
using System.Collections;
using Game;
using Renderer;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Attribute))]
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Effects")]
        public DeathRespawnEffect deathRespawnEffect;
        public PlayerSpawn          soulSpawn;

        [Header("Respawn")]
        public Transform respawnPoint;
        public float     respawnInvincibleTime = 1.5f;

        [Header("Void Death")]
        public float voidYThreshold = -10f;   // if player falls below this Y → death
        public bool  useVoidDeath   = true;

        // ---- state ----
        bool _isDead;
        bool _isInvincible;

        Attribute _attribute;

        private readonly Action _onDied;
        private readonly Action _onRespawned;

        public PlayerHealth(Action onRespawned, Action onDied)
        {
            _onRespawned = onRespawned;
            _onDied = onDied;
        }

        void Awake()
        {
            _attribute = GetComponent<Attribute>();

            if (soulSpawn == null)
                soulSpawn = GetComponent<PlayerSpawn>();

            if (deathRespawnEffect != null)
            {
                deathRespawnEffect.OnDeathAnimationComplete   = HandleDeathComplete;
                deathRespawnEffect.OnRespawnAnimationComplete = HandleRespawnComplete;
            }
        }

        void Update()
        {
            if (_isDead || _isInvincible) return;

            // ---- HP death ----
            if (_attribute.currentHp <= 0f)
            {
                Die(DeathReason.Hp);
                return;
            }

            // ---- Void death ----
            if (useVoidDeath && transform.position.y < voidYThreshold)
            {
                Die(DeathReason.Void);
            }
        }

        // ----------------------------------------------------------------
        private enum DeathReason { Hp, Void }

        void Die(DeathReason reason)
        {
            if (_isDead) return;
            _isDead = true;

            _onDied?.Invoke();

            // void death skips the slow drain animation — instant black
            if (reason == DeathReason.Void)
                deathRespawnEffect?.PlayVoidDeath();
            else
                deathRespawnEffect?.PlayFullDeathRespawn();

            SetPlayerActive(false);
        }

        void HandleDeathComplete()
        {
            if (respawnPoint != null)
                transform.position = respawnPoint.position;

            _attribute.currentHp = _attribute.maxHp;
        }

        void HandleRespawnComplete()
        {
            _isDead = false;
            soulSpawn?.TriggerSpawn();
            StartCoroutine(InvincibilityWindow());
            SetPlayerActive(true);
            _onRespawned?.Invoke();
        }

        IEnumerator InvincibilityWindow()
        {
            _isInvincible = true;
            yield return new WaitForSeconds(respawnInvincibleTime);
            _isInvincible = false;
        }

        void SetPlayerActive(bool active)
        {
            // stop physics
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (!active) rb.linearVelocity = Vector2.zero;
                rb.simulated = active;
            }

            // stop input — Movement checks this before reading any input
            var movement = GetComponent<Movement>();
            if (movement != null)
                movement.InputEnabled = active;
        }
    }
}