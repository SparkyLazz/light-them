using UnityEngine;

namespace Camera
{
    public class Follow : MonoBehaviour
    {
        [Header("Camera Follow")]
            public Transform player;
            public float smoothSpeed;
            public Vector3 offset;
            private void Start()
            {
                transform.position = new Vector3(player.position.x + offset.x, transform.position.y, transform.position.z);
            }
            private void LateUpdate()
            {
                UpdateCameraPosition();
            }
            void UpdateCameraPosition()
            {
                Vector3 targetPosition = new Vector3(player.position.x + offset.x, player.position.y + offset.y, transform.position.z);
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
                transform.position = smoothedPosition;
            }
    }
}