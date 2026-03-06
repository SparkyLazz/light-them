using UnityEngine;

namespace Parallax
{
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("References")]
        private Transform _cameraTransform;

        [Header("Parallax Settings")]
        [Range(0, 1)] public float parallaxMultiplierX = 0.5f;
        [Range(0, 1)] public float parallaxMultiplierY = 0.5f;

        private Vector3 _lastCameraPosition;

        private void Awake()
        {
            _cameraTransform = UnityEngine.Camera.main?.transform;
        }

        private void Start()
        {
            if (_cameraTransform != null)
                _lastCameraPosition = _cameraTransform.position;
        }

        private void LateUpdate()
        {
            if (_cameraTransform is null) return;
            Vector3 deltaMovement = _cameraTransform.position - _lastCameraPosition;

            Vector3 parallax = new Vector3(
                deltaMovement.x * parallaxMultiplierX,
                deltaMovement.y * parallaxMultiplierY,
                0f
            );

            transform.position += parallax;
            _lastCameraPosition = _cameraTransform.position;
        }
    }
}