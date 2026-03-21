using UnityEngine;

namespace Parallax
{
    /// <summary>
    /// Optional manager that coordinates all ParallaxLayer instances.
    /// Attach to an empty GameObject in the scene.
    /// 
    /// Provides:
    ///  - One-call teleport reset (call after moving the camera instantly)
    ///  - Runtime speed multiplier for all layers
    ///  - Auto-discovery of all layers in the scene
    /// </summary>
    public class ParallaxManager : MonoBehaviour
    {
        public static ParallaxManager Instance { get; private set; }

        [Tooltip("Global speed multiplier applied to all parallax factors at runtime.")]
        [Range(0f, 2f)] public float globalSpeedMultiplier = 1f;

        private ParallaxLayer[] _layers;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            RefreshLayers();
        }

        /// <summary>
        /// Re-discover all ParallaxLayer instances. Call if you spawn new layers at runtime.
        /// </summary>
        public void RefreshLayers()
        {
            _layers = Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None);
        }

        /// <summary>
        /// Call this AFTER teleporting the camera to a new position.
        /// Prevents all parallax layers from jumping.
        /// 
        /// Usage:
        ///   camera.transform.position = newPosition;
        ///   ParallaxManager.Instance.OnCameraTeleport();
        /// </summary>
        public void OnCameraTeleport()
        {
            if (_layers == null) return;
            foreach (var layer in _layers)
            {
                if (layer != null)
                    layer.ResetOrigin();
            }
        }
    }
}