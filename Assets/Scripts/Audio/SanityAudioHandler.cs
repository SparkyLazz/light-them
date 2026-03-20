using Player;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Reads the player's Attribute component and drives audio changes
    /// based on sanity level. Attach to the Player GameObject.
    /// 
    /// No modifications needed to Attribute.cs — this reads public fields only.
    /// </summary>
    public class SanityAudioHandler : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Auto-found if left empty")]
        public Attribute playerAttribute;

        [Header("BGM IDs (set in SoundLibrary)")]
        [Tooltip("Calm BGM that plays when sanity is high")]
        public string calmBGM = "bgm_calm";
        [Tooltip("Tense BGM that crossfades in when sanity is low")]
        public string tenseBGM = "bgm_tense";

        [Header("Ambience IDs")]
        [Tooltip("Heartbeat loop that fades in at low sanity")]
        public string heartbeatId = "amb_heartbeat";
        [Tooltip("Whisper/dread loop that layers at very low sanity")]
        public string whisperId = "amb_whisper";

        [Header("Stinger IDs (one-shot sounds at thresholds)")]
        public string stingerMidId = "sfx_sanity_mid";
        public string stingerLowId = "sfx_sanity_low";
        public string stingerDeathId = "sfx_sanity_death";

        [Header("Thresholds (0-100 sanity)")]
        [Tooltip("Below this: crossfade to tense BGM")]
        public float tenseBGMThreshold = 60f;
        [Tooltip("Below this: heartbeat starts")]
        public float heartbeatThreshold = 50f;
        [Tooltip("Below this: whisper layer starts")]
        public float whisperThreshold = 25f;

        [Header("Crossfade")]
        public float bgmCrossfadeDuration = 2f;

        // --- State tracking ---
        private enum SanityState { Calm, Mid, Low, Critical, Dead }
        private SanityState _currentState = SanityState.Calm;
        #pragma warning disable CS0414 // Field is assigned but its value is never used
        private bool _bgmStarted;
        #pragma warning restore CS0414 // Field is assigned but its value is never used

        private void Awake()
        {
            if (playerAttribute == null)
                playerAttribute = GetComponent<Attribute>();

            if (playerAttribute == null)
                playerAttribute = FindAnyObjectByType<Attribute>();
        }

        private void Start()
        {
            // Start with calm BGM
            if (SoundManager.Instance != null && !string.IsNullOrEmpty(calmBGM))
            {
                SoundManager.Instance.PlayBGM(calmBGM, 0f);
                _bgmStarted = true;
            }
        }

        private void Update()
        {
            if (playerAttribute == null || SoundManager.Instance == null) return;

            float sanity = playerAttribute.currentSanity;
            SanityState newState = EvaluateState(sanity);

            if (newState != _currentState)
            {
                OnStateChanged(_currentState, newState, sanity);
                _currentState = newState;
            }

            // Continuously scale heartbeat volume based on intensity
            UpdateHeartbeatIntensity(sanity);
        }

        private SanityState EvaluateState(float sanity)
        {
            if (sanity <= 0f) return SanityState.Dead;
            if (sanity <= whisperThreshold) return SanityState.Critical;
            if (sanity <= heartbeatThreshold) return SanityState.Low;
            if (sanity <= tenseBGMThreshold) return SanityState.Mid;
            return SanityState.Calm;
        }

        private void OnStateChanged(SanityState from, SanityState to, float sanity)
        {
            var sm = SoundManager.Instance;

            // --- BGM crossfade ---
            if (to == SanityState.Calm && from != SanityState.Calm)
            {
                sm.PlayBGM(calmBGM, bgmCrossfadeDuration);
            }
            else if (to != SanityState.Calm && from == SanityState.Calm)
            {
                sm.PlayBGM(tenseBGM, bgmCrossfadeDuration);
            }

            // --- Heartbeat layer ---
            if (to >= SanityState.Low && from < SanityState.Low)
            {
                // Entering low sanity — start heartbeat
                sm.PlayAmbience(heartbeatId, 1.5f);
            }
            else if (to < SanityState.Low && from >= SanityState.Low)
            {
                // Recovering — stop heartbeat
                sm.StopAmbience(heartbeatId, 2f);
            }

            // --- Whisper layer ---
            if (to >= SanityState.Critical && from < SanityState.Critical)
            {
                sm.PlayAmbience(whisperId, 2f);
            }
            else if (to < SanityState.Critical && from >= SanityState.Critical)
            {
                sm.StopAmbience(whisperId, 2f);
            }

            // --- One-shot stingers ---
            switch (to)
            {
                case SanityState.Mid when from == SanityState.Calm:
                    sm.Play(stingerMidId);
                    break;
                case SanityState.Low when from < SanityState.Low:
                    sm.Play(stingerLowId);
                    break;
                case SanityState.Dead:
                    sm.StopAmbience(heartbeatId, 0.5f);
                    sm.StopAmbience(whisperId, 0.5f);
                    sm.Play(stingerDeathId);
                    break;
            }
        }

        /// <summary>
        /// Dynamically scale heartbeat volume: louder as sanity drops lower.
        /// </summary>
        private void UpdateHeartbeatIntensity(float sanity)
        {
            if (sanity >= heartbeatThreshold) return;

            // Map sanity [heartbeatThreshold → 0] to volume [0.3 → 1.0]
            float t = 1f - (sanity / heartbeatThreshold);
            float volume = Mathf.Lerp(0.3f, 1f, t);
            SoundManager.Instance.SetAmbienceVolume(heartbeatId, volume);
        }
    }
}