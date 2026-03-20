using System.Collections.Generic;
using UnityEngine;
// ReSharper disable UnassignedField.Global
// ReSharper disable InconsistentNaming

namespace Audio
{
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [Tooltip("All registered sound entries. Each must have a unique id.")]
        public SoundData[] Sounds;

        private Dictionary<string, SoundData> _lookup;

        /// <summary>
        /// Builds the lookup dictionary. Called once on first access.
        /// </summary>
        private void BuildLookup()
        {
            _lookup = new Dictionary<string, SoundData>();

            if (Sounds == null) return;

            foreach (var sound in Sounds)
            {
                if (sound == null || string.IsNullOrEmpty(sound.id)) continue;

                if (!_lookup.TryAdd(sound.id, sound))
                {
                    Debug.LogWarning($"[SoundLibrary] Duplicate sound id: \"{sound.id}\". Skipping.");
                }
            }
        }

        /// <summary>
        /// Get a SoundData by its string id. Returns null if not found.
        /// </summary>
        public SoundData Get(string id)
        {
            if (_lookup == null) BuildLookup();
            _lookup!.TryGetValue(id, out var data);
            return data;
        }

        /// <summary>
        /// Force rebuild of the lookup (useful after hot-reload in editor).
        /// </summary>
        public void Refresh()
        {
            _lookup = null;
        }
    }
}