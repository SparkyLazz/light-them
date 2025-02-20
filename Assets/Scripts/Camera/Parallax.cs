using UnityEngine;

namespace Camera
{
    public class Parallax : MonoBehaviour
    {
        [Header("Parallax Settings")]
        private float _length, _startPos;
        public GameObject cameraMain;
        public float parallaxEffect;

        private void Start()
        {
            _startPos = transform.position.x;
            _length = GetComponent<SpriteRenderer>().bounds.size.x;
        }

        private void LateUpdate()
        {
            float temp = (cameraMain.transform.position.x * (1 - parallaxEffect));
            float dist = (cameraMain.transform.position.x * parallaxEffect);
            
            transform.position = new Vector3(_startPos + dist, transform.position.y, transform.position.z);
            if (temp > _startPos + _length) _startPos += _length;
            else if (temp < _startPos - _length) _startPos -= _length;
        }
    }
}