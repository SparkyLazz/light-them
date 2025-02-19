using UnityEngine;

namespace Camera
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Settings")]
        public Transform target;
        public Vector3 offset;
        public float smoothSpeed;

        private void LateUpdate()
        {
            if (!target) return;
            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
    }
}