using UnityEngine;

namespace Soul
{
    public class Soul : MonoBehaviour
    {
        public float followSmooth = 8f;

        private Vector3 _targetPosition;

        public void SetTarget(Vector3 target)
        {
            _targetPosition = target;
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(
                transform.position,
                _targetPosition,
                followSmooth * Time.deltaTime
            );
        }
    }
}