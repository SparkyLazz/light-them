using System.Collections;
using UnityEngine;

namespace Shaders.RainDrop
{
    public enum RainType { None, Light, Normal, Heavy }

    public class RainDropController : MonoBehaviour
    {
        [Header("Material Settings")]
        public Material rainMaterial;
        private static readonly int RainAmount = Shader.PropertyToID("_RainAmount");
        private static readonly int TimeScale  = Shader.PropertyToID("_TimeScale");

        [Header("Rain Settings")]
        public float minDuration        = 15f;
        public float maxDuration        = 45f;
        public float transitionDuration = 3f;

        // ReSharper disable once NotAccessedField.Local
        private RainType _currentRain = RainType.None;
        private float    _timeUntilNext;

        [Header("Shader Settings")]
        private static readonly float[] RainAmounts = { 0f, 0.3f, 0.5f, 0.7f };
        private static readonly float[] TimeScales  = { 0f, 1f,   1.5f, 2f   };

        private float _fromAmount;
        private float _toAmount;
        private float _toTimeScale;

        private void Start()
        {
            ApplyImmediate(RainType.None);
            StartCoroutine(WeatherLoop());
        }

        private IEnumerator WeatherLoop()
        {
            while (true)
            {
                _timeUntilNext = Random.Range(minDuration, maxDuration);
                yield return new WaitForSeconds(_timeUntilNext);

                RainType next = (RainType)Random.Range(0, 4);
                yield return StartCoroutine(TransitionTo(next));

                _currentRain = next;
            }
            // ReSharper disable once IteratorNeverReturns
        }

        private IEnumerator TransitionTo(RainType next)
        {
            _toTimeScale = TimeScales[(int)next];
            _toAmount    = RainAmounts[(int)next];
            _fromAmount  = rainMaterial.GetFloat(RainAmount);

            if (next == RainType.None)
            {
                // None: RainAmount lerps to 0 first, then TimeScale instant
                float elapsed = 0f;
                while (elapsed < transitionDuration)
                {
                    elapsed += Time.deltaTime;
                    float t  = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
                    ChangeRainAmount(Mathf.Lerp(_fromAmount, _toAmount, t));
                    yield return null;
                }
                ChangeRainAmount(_toAmount);
                ChangeTime(_toTimeScale);
            }
            else
            {
                // All others: TimeScale instant first, then RainAmount lerps
                ChangeTime(_toTimeScale);

                float elapsed = 0f;
                while (elapsed < transitionDuration)
                {
                    elapsed += Time.deltaTime;
                    float t  = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
                    ChangeRainAmount(Mathf.Lerp(_fromAmount, _toAmount, t));
                    yield return null;
                }
                ChangeRainAmount(_toAmount);
            }
        }

        private void ApplyImmediate(RainType type)
        {
            ChangeTime(TimeScales[(int)type]);
            ChangeRainAmount(RainAmounts[(int)type]);
        }

        private void ChangeRainAmount(float amount)
        {
            if (rainMaterial)
                rainMaterial.SetFloat(RainAmount, amount);
        }

        private void ChangeTime(float time)
        {
            if (rainMaterial)
                rainMaterial.SetFloat(TimeScale, time);
        }
    }
}