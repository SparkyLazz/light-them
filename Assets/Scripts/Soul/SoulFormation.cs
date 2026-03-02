using System.Collections.Generic;
using UnityEngine;

namespace Soul
{
    public class SoulFormation : MonoBehaviour
    {
        [Header("References")]
        public Transform player;
        public GameObject soulPrefab;

        [Header("Formation Settings")]
        public float spacing = 1f;

        private readonly List<Soul> _souls = new List<Soul>();
        public SoulVisualController visualController;

        private void LateUpdate()
        {
            UpdateFormation();
        }

        void UpdateFormation()
        {
            if (player == null) return;

            int soulsPerColumn = 3;   // how tall each column is
            float xSpacing = 1.2f;
            float ySpacing = 0.8f;

            for (int i = 0; i < _souls.Count; i++)
            {
                if (_souls[i] == null) continue;

                int column = i / soulsPerColumn;  // which column (0,1,2...)
                int row = i % soulsPerColumn;     // position inside column

                // Center rows vertically
                float yOffset = (row - (soulsPerColumn - 1) / 2f) * ySpacing;

                Vector3 offset = new Vector3(
                    -(column + 1) * xSpacing,  // extend left
                    yOffset,
                    0
                );

                Vector3 targetPos = player.position + offset;

                _souls[i].SetTarget(targetPos);
            }
        }

        // Call this when player collects a soul
        public void AddSoul()
        {
            GameObject newSoul = Instantiate(soulPrefab, player.position, Quaternion.identity);
            Soul soulComponent = newSoul.GetComponent<Soul>();

            _souls.Add(soulComponent);

            // Register for color update
            SpriteRenderer sr = newSoul.GetComponent<SpriteRenderer>();
            visualController.RegisterSoul(sr);
        }

        public void RemoveSoul()
        {
            if (_souls.Count == 0) return;

            Soul lastSoul = _souls[^1];

            SpriteRenderer sr = lastSoul.GetComponent<SpriteRenderer>();
            visualController.UnregisterSoul(sr);

            _souls.RemoveAt(_souls.Count - 1);
            Destroy(lastSoul.gameObject);
        }

        public int GetSoulCount()
        {
            return _souls.Count;
        }
    }
}