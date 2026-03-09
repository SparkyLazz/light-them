using System;
using UnityEngine;

namespace Soul
{
    public class Follow : MonoBehaviour
    {
        [Header("Player Follow")]
        private Transform _player;
        public float smoothSpeed;
        public Vector3 offset;
        private void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        private void Start()
        {
            transform.position = new Vector3(_player.position.x + offset.x, transform.position.y, transform.position.z);
        }

        private void LateUpdate()
        {
            float dir = Mathf.Approximately(_player.localScale.x, -1) ? -1f : 1f;
            Vector3 targetPosition = new Vector3(
                _player.position.x + offset.x * dir,
                _player.position.y + offset.y * dir,
                transform.position.z);
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
    }
    
}