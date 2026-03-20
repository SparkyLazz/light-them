using UnityEngine;
// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedField.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Audio
{
    public enum AudioLayer
    {
        BGM,
        Ambience,
        Sfx,
        Footstep,
        UI
    }

    [CreateAssetMenu(fileName = "NewSound", menuName = "Audio/Sound Data")]
    public class SoundData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique key used by SoundManager.Play(). Example: sfx_grass_walk, bgm_main, ui_click")]
        // ReSharper disable once InconsistentNaming
        public string id;
        
        [Header("Clips")]
        [Tooltip("Multiple clips for random variation. At least one required.")]
        public AudioClip[] clips;

        [Header("Playback")]
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        [Tooltip("Random pitch offset range. Final pitch = pitch ± randomPitch")]
        [Range(0f, 0.5f)] public float randomPitch;
        public bool loop;

        [Header("Layer")]
        public AudioLayer layer = AudioLayer.Sfx;

        [Header("Spatial")]
        [Tooltip("0 = fully 2D, 1 = fully 3D positional")]
        [Range(0f, 1f)] public float spatialBlend;
        [Tooltip("Max distance for 3D falloff (only matters if spatialBlend > 0)")]
        public float maxDistance = 25f;

        public SoundData(string id)
        {
            this.id = id;
        }

        /// <summary>
        /// Returns a random clip from the clips array, or null if empty.
        /// </summary>
        public AudioClip GetClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        /// <summary>
        /// Returns pitch with random variation applied.
        /// </summary>
        public float GetPitch()
        {
            return pitch + Random.Range(-randomPitch, randomPitch);
        }
    }
}