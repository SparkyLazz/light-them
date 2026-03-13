using UnityEngine;

namespace SFX
{
    public enum SoundCategory { Sfx, Ambient, Music, UI }
    [CreateAssetMenu(fileName = "Sound", menuName = "SFX", order = 0)]
    public class SoundDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string soundId;
        public SoundCategory category = SoundCategory.Sfx;
 
        [Header("Clips — add multiple for randomized variants")]
        public AudioClip[] variants;
 
        [Header("Playback")]
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
        public bool loop;
        public float fadeInDuration;
        public float fadeOutDuration;
 
        [Header("Priority & Limits")]
        [Range(0, 10)] public int priority = 5;   // higher = more important, steals pool first
        public float cooldown;          // minimum seconds between plays (0 = no limit)
        public int maxConcurrent   = 3;           // max simultaneous instances of this sound
 
        // ── Returns a random clip from variants ──────────────────────────
        public AudioClip GetRandomClip()
        {
            if (variants == null || variants.Length == 0) return null;
            return variants[Random.Range(0, variants.Length)];
        }
    }
}