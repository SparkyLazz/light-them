using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game
{
    public class DayCycle : MonoBehaviour
    {
        [Header("Color Settings")] 
        public Gradient colorGradient;
        public Light2D spotLight;
        public AnimationCurve shadowIntensity;

        [Header("Rotation Settings")] 
        public Transform centerPoint;
        public float radius;
        public float rotationTime = 300f;

        [Header("Time Settings")] 
        private float _elapsedTime;
        
        private void Update()
        {
            _elapsedTime += Time.deltaTime;

            // Calculate percentage (0% - 100%) using modulus
            float progress = (_elapsedTime % rotationTime) / rotationTime; // Always stays between 0 and 1
            float angle = progress * 360f; // Convert progress to angle

            // Calculate circular position
            float x = centerPoint.position.x + radius * Mathf.Cos(angle * Mathf.Deg2Rad);
            float y = centerPoint.position.y + radius * Mathf.Sin(angle * Mathf.Deg2Rad);
            transform.position = new Vector3(x, y, transform.position.z);

            // Ensure light always faces the center
            Vector3 direction = centerPoint.position - transform.position;
            float rotationZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationZ - 90f); // Adjust alignment

            // Apply gradient and shadow changes
            spotLight.color = colorGradient.Evaluate(progress);
            spotLight.shadowIntensity = shadowIntensity.Evaluate(progress);
        }
    }
}