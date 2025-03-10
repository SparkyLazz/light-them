using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Game
{
    public class DayCycle : MonoBehaviour
    {
        [Header("Color Settings")] 
        public Gradient dayCycleColor;
        public Light2D globalLight;

        [Header("Debug Settings")] 
        public float dayDuration = 120f; // Default to 2 minutes per cycle
        private float _cycleTimer;
        
        private void Update()
        {
            // Timer by real time clock
            _cycleTimer += Time.deltaTime;
            
            // Percentage of day (0% -> 100%)
            float timeProgress = (_cycleTimer % dayDuration) / dayDuration;
            globalLight.color = dayCycleColor.Evaluate(timeProgress);

        }
    }
}
