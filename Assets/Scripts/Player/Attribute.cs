using System;
using UnityEngine;

namespace Player
{
    public class Attribute : MonoBehaviour
    {
        [Header("Sanity Attributes")]
        public float currentSanity;
        private const float MaxSanity = 100f;

        [Header("Void Settings")] 
        public float yPosition;
        private Transform _respawnPoint;
        

        private void Awake()
        {
            _respawnPoint = GameObject.FindGameObjectWithTag("Respawn").transform;
        }

        private void Start()
        {
            currentSanity = MaxSanity;
            yPosition = -10f;
        }

        private void Update()
        {
            CheckVoid();
        }
        
        private void CheckVoid()
        {
            if (gameObject.transform.position.y < yPosition)
            {
                //Play animation needed before lerp player position into respawn Point
                transform.position = _respawnPoint.position;
            }
        }
    }
}