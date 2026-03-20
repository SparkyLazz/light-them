using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable MemberCanBePrivate.Global

namespace Audio
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Library")]
        public SoundLibrary library;

        [Header("Volume Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float bgmVolume = 1f;
        [Range(0f, 1f)] public float ambienceVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float uiVolume = 1f;

        [Header("SFX Pool")]
        [Tooltip("How many SFX AudioSources to pre-create for overlapping sounds")]
        public int sfxPoolSize = 8;

        // --- Layer Sources ---
        private AudioSource _bgmSourceA;
        private AudioSource _bgmSourceB;
        private AudioSource _activeBgm;
        private Coroutine _crossfadeRoutine;

        private readonly List<AudioSource> _sfxPool = new();
        private AudioSource _uiSource;
        private AudioSource _footstepSource;

        // --- Ambience Layers (for stacking multiple loops) ---
        private readonly Dictionary<string, AudioSource> _ambienceLayers = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSources();
        }

        private void InitializeSources()
        {
            // BGM dual sources for crossfading
            _bgmSourceA = CreateSource("BGM_A", true);
            _bgmSourceB = CreateSource("BGM_B", true);
            _activeBgm = _bgmSourceA;

            // Main ambience
            CreateSource("Ambience", true);

            // SFX pool
            for (int i = 0; i < sfxPoolSize; i++)
            {
                _sfxPool.Add(CreateSource($"SFX_{i}", false));
            }

            // UI (ignores listener pause)
            _uiSource = CreateSource("UI", false);
            _uiSource.ignoreListenerPause = true;

            // Footsteps
            _footstepSource = CreateSource("Footstep", false);
        }

        private AudioSource CreateSource(string label, bool loop)
        {
            var go = new GameObject($"[AudioSource] {label}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            return src;
        }

        #region Public API

        // =====================================================================
        //  PLAY — one-shot SFX or UI by id
        // =====================================================================

        /// <summary>
        /// Play a one-shot sound by id. Respects the layer defined in SoundData.
        /// </summary>
        public void Play(string id)
        {
            var data = GetData(id);
            if (data == null) return;

            switch (data.layer)
            {
                case AudioLayer.UI:
                    PlayOnSource(_uiSource, data, uiVolume);
                    break;
                case AudioLayer.Footstep:
                    PlayOnSource(_footstepSource, data, sfxVolume);
                    break;
                default:
                    PlayFromPool(data);
                    break;
            }
        }

        /// <summary>
        /// Play a one-shot sound at a world position (3D positional).
        /// </summary>
        public void PlayAtPosition(string id, Vector3 position)
        {
            var data = GetData(id);
            if (data == null) return;

            var source = GetAvailablePoolSource();
            if (source == null) return;

            source.transform.position = position;
            ConfigureSource(source, data, sfxVolume);
            source.spatialBlend = data.spatialBlend;
            source.maxDistance = data.maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
        }

        // =====================================================================
        //  BGM
        // =====================================================================

        /// <summary>
        /// Play background music immediately, or crossfade if something is already playing.
        /// </summary>
        public void PlayBGM(string id, float crossfadeDuration = 1f)
        {
            var data = GetData(id);
            if (data == null) return;

            if (_activeBgm.isPlaying && crossfadeDuration > 0f)
            {
                CrossfadeBGM(data, crossfadeDuration);
            }
            else
            {
                ConfigureSource(_activeBgm, data, bgmVolume);
                _activeBgm.loop = true;
                _activeBgm.Play();
            }
        }

        /// <summary>
        /// Stop current BGM with optional fade out.
        /// </summary>
        public void StopBGM(float fadeOut = 1f)
        {
            if (fadeOut <= 0f)
            {
                _bgmSourceA.Stop();
                _bgmSourceB.Stop();
                return;
            }

            StartCoroutine(FadeOut(_activeBgm, fadeOut));
        }

        // =====================================================================
        //  AMBIENCE LAYERS — stackable loops (heartbeat, whispers, wind, etc.)
        // =====================================================================

        /// <summary>
        /// Start an ambience loop. If already playing, does nothing.
        /// </summary>
        public void PlayAmbience(string id, float fadeIn = 1f)
        {
            if (_ambienceLayers.ContainsKey(id)) return;

            var data = GetData(id);
            if (data == null) return;

            var go = new GameObject($"[Ambience] {id}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;

            ConfigureSource(src, data, 0f); // start silent
            src.loop = true;
            src.Play();

            _ambienceLayers[id] = src;
            StartCoroutine(FadeIn(src, data.volume * ambienceVolume * masterVolume, fadeIn));
        }

        /// <summary>
        /// Stop and remove an ambience loop.
        /// </summary>
        public void StopAmbience(string id, float fadeOut = 1f)
        {
            if (!_ambienceLayers.Remove(id, out var src)) return;
            StartCoroutine(FadeOutAndDestroy(src, fadeOut));
        }

        /// <summary>
        /// Set the volume of a playing ambience layer (0-1 normalized).
        /// Useful for dynamically scaling heartbeat intensity with sanity.
        /// </summary>
        public void SetAmbienceVolume(string id, float normalizedVolume)
        {
            if (!_ambienceLayers.TryGetValue(id, out var src)) return;
            src.volume = Mathf.Clamp01(normalizedVolume) * ambienceVolume * masterVolume;
        }

        // =====================================================================
        //  FOOTSTEP — dedicated channel
        // =====================================================================

        /// <summary>
        /// Play a footstep sound. Convention: "footstep_grass", "footstep_stone", etc.
        /// </summary>
        public void PlayFootstep(string surfaceType = "grass")
        {
            string id = $"footstep_{surfaceType}";
            var data = GetData(id);
            if (data == null) return;

            PlayOnSource(_footstepSource, data, sfxVolume);
        }

        // =====================================================================
        //  VOLUME — runtime control for settings menu
        // =====================================================================

        public void SetMasterVolume(float vol)
        {
            masterVolume = Mathf.Clamp01(vol);
            AudioListener.volume = masterVolume;
            PlayerPrefs.SetFloat("vol_master", masterVolume);
        }

        public void SetBGMVolume(float vol)
        {
            bgmVolume = Mathf.Clamp01(vol);
            _activeBgm.volume = bgmVolume * masterVolume;
            PlayerPrefs.SetFloat("vol_bgm", bgmVolume);
        }

        public void SetSfxVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat("vol_sfx", sfxVolume);
        }

        public void SetUIVolume(float vol)
        {
            uiVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat("vol_ui", uiVolume);
        }

        /// <summary>
        /// Load saved volume settings from PlayerPrefs.
        /// Call this on game start.
        /// </summary>
        public void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("vol_master", 1f);
            bgmVolume = PlayerPrefs.GetFloat("vol_bgm", 1f);
            sfxVolume = PlayerPrefs.GetFloat("vol_sfx", 1f);
            uiVolume = PlayerPrefs.GetFloat("vol_ui", 1f);
            AudioListener.volume = masterVolume;
        }

        #endregion

        #region Internal

        private SoundData GetData(string id)
        {
            if (library == null)
            {
                Debug.LogError("[SoundManager] SoundLibrary is not assigned!");
                return null;
            }

            var data = library.Get(id);
            if (data == null)
            {
                Debug.LogWarning($"[SoundManager] Sound not found: \"{id}\"");
            }
            return data;
        }

        private void ConfigureSource(AudioSource source, SoundData data, float layerVolume)
        {
            var clip = data.GetClip();
            if (clip == null) return;

            source.clip = clip;
            source.volume = data.volume * layerVolume * masterVolume;
            source.pitch = data.GetPitch();
            source.spatialBlend = data.spatialBlend;
        }

        private void PlayOnSource(AudioSource source, SoundData data, float layerVolume)
        {
            var clip = data.GetClip();
            if (clip == null) return;

            source.pitch = data.GetPitch();
            source.spatialBlend = data.spatialBlend;
            source.PlayOneShot(clip, data.volume * layerVolume * masterVolume);
        }

        private void PlayFromPool(SoundData data)
        {
            var source = GetAvailablePoolSource();
            if (source == null) return;

            PlayOnSource(source, data, sfxVolume);
        }

        private AudioSource GetAvailablePoolSource()
        {
            foreach (var src in _sfxPool)
            {
                if (!src.isPlaying) return src;
            }

            // All busy — steal the first one
            Debug.LogWarning("[SoundManager] SFX pool exhausted, reusing slot 0.");
            return _sfxPool[0];
        }

        // --- Crossfade ---

        private void CrossfadeBGM(SoundData newData, float duration)
        {
            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);

            var incoming = (_activeBgm == _bgmSourceA) ? _bgmSourceB : _bgmSourceA;
            ConfigureSource(incoming, newData, bgmVolume);
            incoming.loop = true;
            incoming.volume = 0f;
            incoming.Play();

            _crossfadeRoutine = StartCoroutine(CrossfadeRoutine(_activeBgm, incoming, duration));
            _activeBgm = incoming;
        }

        private IEnumerator CrossfadeRoutine(AudioSource outgoing, AudioSource incoming, float duration)
        {
            // Recalculate target from the data volume (was set to 0 before Play)
            // We use the incoming clip's intended volume
            float incomingTarget = bgmVolume * masterVolume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                outgoing.volume = Mathf.Lerp(outgoing.volume, 0f, t);
                incoming.volume = Mathf.Lerp(0f, incomingTarget, t);
                yield return null;
            }

            outgoing.Stop();
            outgoing.volume = 0f;
            incoming.volume = incomingTarget;
            _crossfadeRoutine = null;
        }

        // --- Fade helpers ---

        private IEnumerator FadeIn(AudioSource source, float targetVolume, float duration)
        {
            if (duration <= 0f)
            {
                source.volume = targetVolume;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
                yield return null;
            }
            source.volume = targetVolume;
        }

        private IEnumerator FadeOut(AudioSource source, float duration)
        {
            float start = source.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(start, 0f, elapsed / duration);
                yield return null;
            }
            source.Stop();
            source.volume = 0f;
        }

        private IEnumerator FadeOutAndDestroy(AudioSource source, float duration)
        {
            yield return FadeOut(source, duration);
            Destroy(source.gameObject);
        }

        #endregion
    }
}