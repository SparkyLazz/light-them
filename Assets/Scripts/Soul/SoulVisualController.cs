using System.Collections.Generic;
using UnityEngine;
using Player;

namespace Soul
{
    public class SoulVisualController : MonoBehaviour
    {
        [Header("References")]
        public Attribute playerAttribute;

        [Header("Visual Settings")]
        public Gradient hpGradient;

        private readonly List<SpriteRenderer> _soulSprites = new List<SpriteRenderer>();

        public void RegisterSoul(SpriteRenderer sprite)
        {
            _soulSprites.Add(sprite);
        }

        public void UnregisterSoul(SpriteRenderer sprite)
        {
            _soulSprites.Remove(sprite);
        }

        private void Update()
        {
            UpdateSoulColors();
        }

        void UpdateSoulColors()
        {
            if (!playerAttribute) return;

            float normalizedHp = playerAttribute.NormalizedHp;
            Color colorFinal = hpGradient.Evaluate(normalizedHp);

            foreach (var t in _soulSprites)
            {
                if (t)
                    t.color = colorFinal;
            }
        }
    }
}