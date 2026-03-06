using UnityEngine;

namespace Parallax
{
    
    public class InfiniteParallax : MonoBehaviour
    {
        private Transform _cameraTransform;
        public float chunkWidth = 27f;

        private Transform _leftChunk;
        private Transform _middleChunk;
        private Transform _rightChunk;

        private void Awake()
        {
            _cameraTransform = UnityEngine.Camera.main?.transform;
            _leftChunk = transform.GetChild(0);
            _middleChunk = transform.GetChild(1);
            _rightChunk = transform.GetChild(2);
        }

        private void Update()
        {
            float cameraX = _cameraTransform.position.x;
            if (cameraX > _middleChunk.position.x + chunkWidth)
            {
                MoveLeftToRight();
            }
            else if (cameraX < _middleChunk.position.x - chunkWidth)
            {
                MoveRightToLeft();
            }
        }

        private void MoveLeftToRight()
        {
            Transform oldLeft = _leftChunk;
            _leftChunk = _middleChunk;
            _middleChunk = _rightChunk;
            _rightChunk = oldLeft;

            _rightChunk.position = _middleChunk.position + Vector3.right * chunkWidth;
        }

        private void MoveRightToLeft()
        {
            Transform oldRight = _rightChunk;
            _rightChunk = _middleChunk;
            _middleChunk = _leftChunk;
            _leftChunk = oldRight;

            _leftChunk.position = _middleChunk.position + Vector3.left * chunkWidth;
        }
    }
}