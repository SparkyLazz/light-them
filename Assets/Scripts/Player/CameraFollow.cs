using System.Collections.Generic;
using UnityEngine;

namespace Cameras
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Camera Follow")]
        public Transform player;
        public float smoothSpeed;
        public Vector3 offset;

        [Header("Renderer Setting")]
        private Camera _mainCamera;
        private void Start()
        {
            transform.position = new Vector3(player.position.x + offset.x, transform.position.y, transform.position.z);
            _mainCamera = Camera.main;
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