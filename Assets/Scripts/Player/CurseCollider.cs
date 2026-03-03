using UnityEngine;

namespace Player
{
    public class CurseCollider : MonoBehaviour
    {
        private static readonly int ChromaticStrength =
            Shader.PropertyToID("_ChromaticStrength");
        private static readonly int ChromaticPulse =
            Shader.PropertyToID("_ChromaticPulse");

        [Header("Speeds")]
        public float toHalfSpeed = 0.5f;
        public float toZeroSpeed = 2f;
        public float extremeSpeed = 0.1f;

        [Header("Shader Target")]
        [SerializeField] private Material curseMaterial;

        [Header("Chromatic Settings")]
        [SerializeField] private float maxChromaticStrength = 0.03f;
        [SerializeField] private float maxChromaticPulse = 1f;

        [Header("Effect Timing")]
        [SerializeField] private float chromaticRampDuration = 5f;

        public float CurrentCurse { get; private set; }

        private int _zoneCounter;
        private bool IsInsideZone => _zoneCounter > 0;

        private float _chromaticProgress; // 0 → 1 ramp

        private void Update()
        {
            HandleCurse();
            UpdateChromatic();
        }

        private void HandleCurse()
        {
            if (IsInsideZone)
            {
                CurrentCurse = Mathf.MoveTowards(
                    CurrentCurse,
                    0f,
                    toZeroSpeed * Time.deltaTime
                );
            }
            else
            {
                if (CurrentCurse < 0.5f)
                {
                    CurrentCurse = Mathf.MoveTowards(
                        CurrentCurse,
                        0.5f,
                        toHalfSpeed * Time.deltaTime
                    );
                }
                else
                {
                    CurrentCurse = Mathf.MoveTowards(
                        CurrentCurse,
                        1f,
                        extremeSpeed * Time.deltaTime
                    );
                }
            }
        }

        private void UpdateChromatic()
        {
            if (curseMaterial == null) return;

            // Only activate after 0.75 curse
            if (CurrentCurse >= 0.75f)
            {
                _chromaticProgress = Mathf.MoveTowards(
                    _chromaticProgress,
                    1f,
                    Time.deltaTime / chromaticRampDuration
                );
            }
            else
            {
                _chromaticProgress = Mathf.MoveTowards(
                    _chromaticProgress,
                    0f,
                    Time.deltaTime / chromaticRampDuration
                );
            }

            float strength = _chromaticProgress * maxChromaticStrength;
            float pulse = _chromaticProgress * maxChromaticPulse;

            curseMaterial.SetFloat(ChromaticStrength, strength);
            curseMaterial.SetFloat(ChromaticPulse, pulse);
        }

        public void EnterZone() => _zoneCounter++;
        public void ExitZone() => _zoneCounter = Mathf.Max(0, _zoneCounter - 1);
    }
}