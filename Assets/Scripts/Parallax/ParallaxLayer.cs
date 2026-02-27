using UnityEngine;

namespace Parallax
{
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        [Header("Parallax Settings")]
        [Range(0, 1)] public float parallaxMultiplierX = 0.5f;
        [Range(0, 1)] public float parallaxMultiplierY = 0.5f;

        private Vector3 _lastCameraPosition;

        private void Awake()
        {
            if (cameraTransform == null)
                cameraTransform = UnityEngine.Camera.main?.transform;
        }

        private void Start()
        {
            if (cameraTransform != null)
                _lastCameraPosition = cameraTransform.position;
        }

        private void LateUpdate()
        {
            if (cameraTransform is null) return;
            Vector3 deltaMovement = cameraTransform.position - _lastCameraPosition;

            Vector3 parallax = new Vector3(
                deltaMovement.x * parallaxMultiplierX,
                deltaMovement.y * parallaxMultiplierY,
                0f
            );

            transform.position += parallax;
            _lastCameraPosition = cameraTransform.position;
        }
    }
}