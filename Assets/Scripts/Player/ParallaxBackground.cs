using UnityEngine;

namespace Player
{
    [System.Serializable]
    public class ParallaxLayerGroup
    {
        public Transform[] tiles;     // The 3 tiles for this layer group
        public float parallaxFactor;  // 0 = far away, 1 = follow camera
        public float tileWidth;       // Width of each tile in world units
    }

    public class ParallaxBackground : MonoBehaviour
    {
        public ParallaxLayerGroup[] layers;
        private Transform cam;
        private Vector3 previousCamPos;

        void Start()
        {
            cam = Camera.main.transform;
            previousCamPos = cam.position;
        }

        void LateUpdate()
        {
            Vector3 delta = cam.position - previousCamPos;

            foreach (var layer in layers)
            {
                // Move all tiles with parallax
                foreach (var tile in layer.tiles)
                {
                    tile.position += new Vector3(delta.x * layer.parallaxFactor, 0, 0);
                }

                // Check if tiles need to be recycled
                for (int i = 0; i < layer.tiles.Length; i++)
                {
                    Transform tile = layer.tiles[i];
                    float camX = cam.position.x;

                    // Moving right
                    if (camX - tile.position.x > layer.tileWidth)
                    {
                        // Find leftmost tile
                        Transform leftmost = layer.tiles[0];
                        foreach (var t in layer.tiles)
                            if (t.position.x < leftmost.position.x)
                                leftmost = t;

                        // Find rightmost tile
                        Transform rightmost = layer.tiles[0];
                        foreach (var t in layer.tiles)
                            if (t.position.x > rightmost.position.x)
                                rightmost = t;

                        // Move leftmost to the right of rightmost
                        leftmost.position = rightmost.position + new Vector3(layer.tileWidth, 0, 0);
                    }

                    // Moving left
                    if (camX - tile.position.x < -layer.tileWidth)
                    {
                        // Find rightmost tile
                        Transform rightmost = layer.tiles[0];
                        foreach (var t in layer.tiles)
                            if (t.position.x > rightmost.position.x)
                                rightmost = t;

                        // Find leftmost tile
                        Transform leftmost = layer.tiles[0];
                        foreach (var t in layer.tiles)
                            if (t.position.x < leftmost.position.x)
                                leftmost = t;

                        // Move rightmost to the left of leftmost
                        rightmost.position = leftmost.position - new Vector3(layer.tileWidth, 0, 0);
                    }
                }
            }

            previousCamPos = cam.position;
        }
    }
}
