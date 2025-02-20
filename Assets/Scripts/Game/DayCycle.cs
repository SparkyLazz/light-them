using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace Game
{
    public class DayCycle : MonoBehaviour
    {
        [Header("Color Settings")] 
        public Gradient dayCycleColor;
        public ParticleSystem rainEffect;
        public Color lightRain;
        public Color nightRain;

        [Header("Light Settings")] 
        public Light2D globalLight;

        [Header("Debug Settings")] 
        public float dayDuration;
        public bool isRaining;
        private float _cycleTimer;

        private void Start()
        {
            StartCoroutine(RandomRainCycle());
        }

        private void Update()
        {
            //Timer by real time clock
            _cycleTimer += Time.deltaTime;
            
            //Percentage of day (0% -> 100%)
            var timeProgress = (_cycleTimer % dayDuration) / dayDuration;
            
            //Apply next Color to Global light by gradient
            Color targetColor = dayCycleColor.Evaluate(timeProgress);
            
            //If raining change the rain color
            if (isRaining)
            {
                targetColor = Color.Lerp(targetColor, timeProgress < 0.5 ? lightRain : nightRain, 0.5f); //Daytime Rain
            }
            //Color has applied by here
            globalLight.color = targetColor;
        }

        IEnumerator RandomRainCycle()
        {
            while (true)
            {
                //Wait a random time before next rain event (1 -> 3 minutes)
                float waitTime = Random.Range(60f, 180f);
                yield return new WaitForSeconds(waitTime);
                
                //Start rain for a random duration (30 - 90  seconds)
                rainEffect.Play();
                isRaining = true;
                yield return new WaitForSeconds(Random.Range(30f, 90f));
                
                //Stop rain
                rainEffect.Stop();
                isRaining = false;
            }
        }
    }
}