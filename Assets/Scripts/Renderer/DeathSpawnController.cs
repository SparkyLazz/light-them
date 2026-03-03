using System.Collections;
using UnityEngine;

namespace Renderer
{
    public class DeathRespawnEffect : MonoBehaviour
    {
        [Header("Material")]
        public Material deathRespawnMaterial;

        [Header("Timing")]
        public float deathDuration    = 1.8f;   // how long death animation takes
        public float holdBlackTime    = 0.4f;   // how long to hold on black
        public float respawnDuration  = 1.2f;   // how long respawn animation takes

        [Header("References")]
        public Transform playerTransform;
        public UnityEngine.Camera    mainCamera;

        // ---- events you can hook into ----
        public System.Action OnDeathAnimationComplete;
        public System.Action OnRespawnAnimationComplete;

        // cached property IDs
        static readonly int PropDeathProgress   = Shader.PropertyToID("_DeathProgress");
        static readonly int PropRespawnProgress = Shader.PropertyToID("_RespawnProgress");
        static readonly int PropRespawnOrigin   = Shader.PropertyToID("_RespawnOrigin");

        bool _isPlaying;

        void Awake()
        {
            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;

            // start clean
            if (deathRespawnMaterial != null) {
                deathRespawnMaterial.SetFloat(PropDeathProgress,   0f);
                deathRespawnMaterial.SetFloat(PropRespawnProgress, 0f);
            }
        }

        // ---- Call this when player dies ----
        public void PlayDeath()
        {
            if (_isPlaying) return;
            StartCoroutine(DeathSequence());
        }

        // ---- Call this when player respawns ----
        public void PlayRespawn()
        {
            if (_isPlaying) return;
            StartCoroutine(RespawnSequence());
        }

        // ---- Or playful sequence: death → hold → respawn ----
        public void PlayFullDeathRespawn()
        {
            if (_isPlaying) return;
            StartCoroutine(FullSequence());
        }

        IEnumerator DeathSequence()
        {
            _isPlaying = true;

            float t = 0f;
            while (t < deathDuration) {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / deathDuration);
                // ease in — slow start, accelerates at end
                float eased = 1f - Mathf.Pow(1f - progress, 2.5f);
                deathRespawnMaterial.SetFloat(PropDeathProgress, eased);
                yield return null;
            }

            deathRespawnMaterial.SetFloat(PropDeathProgress, 1f);
            OnDeathAnimationComplete?.Invoke();
            _isPlaying = false;
        }

        IEnumerator RespawnSequence()
        {
            _isPlaying = true;

            // set respawn origin to player screen position
            SetRespawnOrigin();

            // reset death, start respawn from 0
            deathRespawnMaterial.SetFloat(PropDeathProgress,   0f);
            deathRespawnMaterial.SetFloat(PropRespawnProgress, 0f);

            float t = 0f;
            while (t < respawnDuration) {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / respawnDuration);
                // ease out — fast start, slows at end
                float eased = 1f - Mathf.Pow(1f - progress, 2f);
                deathRespawnMaterial.SetFloat(PropRespawnProgress, eased);
                yield return null;
            }

            deathRespawnMaterial.SetFloat(PropRespawnProgress, 0f);
            OnRespawnAnimationComplete?.Invoke();
            _isPlaying = false;
        }

        IEnumerator FullSequence()
        {
            _isPlaying = true;

            // ---- Death ----
            float t = 0f;
            while (t < deathDuration) {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / deathDuration);
                float eased    = 1f - Mathf.Pow(1f - progress, 2.5f);
                deathRespawnMaterial.SetFloat(PropDeathProgress, eased);
                yield return null;
            }
            deathRespawnMaterial.SetFloat(PropDeathProgress, 1f);
            OnDeathAnimationComplete?.Invoke();

            // ---- Hold on black ----
            yield return new WaitForSeconds(holdBlackTime);

            // ---- Respawn ----
            SetRespawnOrigin();
            deathRespawnMaterial.SetFloat(PropDeathProgress,   0f);
            deathRespawnMaterial.SetFloat(PropRespawnProgress, 0f);

            t = 0f;
            while (t < respawnDuration) {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / respawnDuration);
                float eased    = 1f - Mathf.Pow(1f - progress, 2f);
                deathRespawnMaterial.SetFloat(PropRespawnProgress, eased);
                yield return null;
            }

            deathRespawnMaterial.SetFloat(PropRespawnProgress, 0f);
            OnRespawnAnimationComplete?.Invoke();
            _isPlaying = false;
        }

        void SetRespawnOrigin()
        {
            if (!playerTransform) return;

            deathRespawnMaterial.SetVector(PropRespawnOrigin,
                new Vector4(playerTransform.position.x,
                    playerTransform.position.y,
                    0, 0));
        }
        
        public void PlayVoidDeath()
        {
            if (_isPlaying) return;
            StartCoroutine(VoidDeathSequence());
        }

        IEnumerator VoidDeathSequence()
        {
            _isPlaying = true;

            // instant black — no slow drain
            float t = 0f;
            float quickDuration = 0.25f;
            while (t < quickDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / quickDuration);
                deathRespawnMaterial.SetFloat(PropDeathProgress, progress);
                yield return null;
            }

            deathRespawnMaterial.SetFloat(PropDeathProgress, 1f);
            OnDeathAnimationComplete?.Invoke();

            // hold black
            yield return new WaitForSeconds(holdBlackTime);

            // respawn
            SetRespawnOrigin();
            deathRespawnMaterial.SetFloat(PropDeathProgress,   0f);
            deathRespawnMaterial.SetFloat(PropRespawnProgress, 0f);

            t = 0f;
            while (t < respawnDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / respawnDuration);
                float eased    = 1f - Mathf.Pow(1f - progress, 2f);
                deathRespawnMaterial.SetFloat(PropRespawnProgress, eased);
                yield return null;
            }

            deathRespawnMaterial.SetFloat(PropRespawnProgress, 0f);
            OnRespawnAnimationComplete?.Invoke();
            _isPlaying = false;
        }
    }
}