using UnityEngine;

namespace Parallax
{
    /// <summary>
    /// Handles parallax movement AND infinite chunk recycling for a single layer.
    /// Uses position-based offset (not delta accumulation) to eliminate floating-point drift.
    /// 
    /// HIERARCHY SETUP:
    ///   ParallaxLayer_Mountains        ← attach this script
    ///     ├── Chunk_0  (SpriteRenderer) ← left
    ///     ├── Chunk_1  (SpriteRenderer) ← middle (camera starts here)
    ///     └── Chunk_2  (SpriteRenderer) ← right
    /// 
    /// All three chunks must use the same seamlessly-tiling sprite.
    /// The script auto-detects chunk width from the SpriteRenderer bounds.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Parallax Settings")]
        [Tooltip("0 = static background (sky), 1 = moves with camera (no parallax)")]
        [Range(0f, 1f)] public float parallaxFactorX = 0.5f;

        [Tooltip("Usually 0 for side-scrollers. Set > 0 for vertical depth.")]
        [Range(0f, 1f)] public float parallaxFactorY = 0f;

        [Header("Infinite Scrolling")]
        [Tooltip("Enable chunk recycling for infinite scrolling.")]
        public bool infiniteScroll = true;

        [Tooltip("Override chunk width. Leave 0 to auto-detect from SpriteRenderer.")]
        public float chunkWidthOverride = 0f;

        // ── Internal State ──────────────────────────────────────────────────
        private Transform _cam;
        private Vector3 _startPos;       // layer's initial world position
        private Vector3 _camStartPos;    // camera's initial world position

        // chunk recycling
        private Transform[] _chunks;     // always ordered: [left, middle, right]
        private float _chunkWidth;

        // ── Initialization ──────────────────────────────────────────────────
        private void Awake()
        {
            _cam = UnityEngine.Camera.main != null
                ? UnityEngine.Camera.main.transform
                : null;

            if (_cam == null)
            {
                Debug.LogError($"[ParallaxLayer] No MainCamera found! Disabling {gameObject.name}.");
                enabled = false;
                return;
            }

            _startPos = transform.position;
            _camStartPos = _cam.position;

            if (infiniteScroll)
                InitializeChunks();
        }

        private void InitializeChunks()
        {
            if (transform.childCount < 3)
            {
                Debug.LogError($"[ParallaxLayer] '{gameObject.name}' needs exactly 3 child chunks. Found {transform.childCount}. Disabling infinite scroll.");
                infiniteScroll = false;
                return;
            }

            // Detect chunk width from the first child's SpriteRenderer
            _chunkWidth = chunkWidthOverride;
            if (_chunkWidth <= 0f)
            {
                var sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    _chunkWidth = sr.bounds.size.x;
                }
                else
                {
                    Debug.LogError($"[ParallaxLayer] No SpriteRenderer on first chunk of '{gameObject.name}'. Set chunkWidthOverride manually.");
                    infiniteScroll = false;
                    return;
                }
            }

            // Sort children left-to-right by their initial X position
            _chunks = new Transform[3];
            var children = new Transform[3];
            for (int i = 0; i < 3; i++)
                children[i] = transform.GetChild(i);

            System.Array.Sort(children, (a, b) => a.position.x.CompareTo(b.position.x));
            _chunks[0] = children[0]; // left
            _chunks[1] = children[1]; // middle
            _chunks[2] = children[2]; // right
        }

        // ── Core Loop ───────────────────────────────────────────────────────
        // Everything in LateUpdate so camera has finished moving (Follow.cs also uses LateUpdate,
        // set this script's execution order AFTER the camera via Script Execution Order settings,
        // or use [DefaultExecutionOrder(100)] on this class).
        private void LateUpdate()
        {
            ApplyParallax();

            if (infiniteScroll)
                RecycleChunks();
        }

        // ── Position-Based Parallax (no drift) ─────────────────────────────
        // Instead of accumulating deltas, we compute the exact offset each frame:
        //   layerPos = startPos + cameraDelta * parallaxFactor
        // This means the layer "follows" the camera at a fraction of its movement.
        // parallaxFactor 0 → layer never moves (deep sky)
        // parallaxFactor 1 → layer moves 1:1 with camera (foreground, no parallax)
        private void ApplyParallax()
        {
            Vector3 cameraDelta = _cam.position - _camStartPos;

            float newX = _startPos.x + cameraDelta.x * parallaxFactorX;
            float newY = _startPos.y + cameraDelta.y * parallaxFactorY;

            transform.position = new Vector3(newX, newY, transform.position.z);
        }

        // ── Chunk Recycling ─────────────────────────────────────────────────
        // Compares camera position against the APPARENT middle chunk position.
        // Because the parent has parallax applied, chunk world positions already
        // account for the offset — so we compare directly.
        private void RecycleChunks()
        {
            float camX = _cam.position.x;
            float midX = _chunks[1].position.x;

            // Camera moved far enough right → recycle left chunk to the right
            if (camX - midX > _chunkWidth)
            {
                Transform recycled = _chunks[0];
                recycled.position = new Vector3(
                    _chunks[2].position.x + _chunkWidth,
                    recycled.position.y,
                    recycled.position.z
                );

                // Shift references: [left, mid, right] → [old_mid, old_right, recycled]
                _chunks[0] = _chunks[1];
                _chunks[1] = _chunks[2];
                _chunks[2] = recycled;
            }
            // Camera moved far enough left → recycle right chunk to the left
            else if (midX - camX > _chunkWidth)
            {
                Transform recycled = _chunks[2];
                recycled.position = new Vector3(
                    _chunks[0].position.x - _chunkWidth,
                    recycled.position.y,
                    recycled.position.z
                );

                // Shift references: [recycled, old_left, old_mid]
                _chunks[2] = _chunks[1];
                _chunks[1] = _chunks[0];
                _chunks[0] = recycled;
            }
        }

        // ── Public API ──────────────────────────────────────────────────────

        /// <summary>
        /// Call this after teleporting the camera to prevent a position jump.
        /// Resets the reference points so parallax recalculates from the new position.
        /// </summary>
        public void ResetOrigin()
        {
            if (_cam == null) return;
            _startPos = transform.position;
            _camStartPos = _cam.position;
        }

        /// <summary>
        /// Call from a manager to reset ALL layers after a camera teleport.
        /// Usage: FindObjectsOfType&lt;ParallaxLayer&gt;().ForEach(l => l.ResetOrigin());
        /// </summary>
        public static void ResetAllOrigins()
        {
            foreach (var layer in Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None))
                layer.ResetOrigin();
        }

#if UNITY_EDITOR
        // ── Editor Gizmos ───────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            if (!infiniteScroll || _chunks == null) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < _chunks.Length; i++)
            {
                if (_chunks[i] == null) continue;
                Vector3 center = _chunks[i].position;
                Vector3 size = new Vector3(_chunkWidth, 10f, 0.1f);
                Gizmos.DrawWireCube(center, size);
                UnityEditor.Handles.Label(center + Vector3.up * 5.5f,
                    i == 0 ? "L" : i == 1 ? "M" : "R",
                    new GUIStyle { fontSize = 14, normal = { textColor = Color.cyan } });
            }
        }
#endif
    }
}