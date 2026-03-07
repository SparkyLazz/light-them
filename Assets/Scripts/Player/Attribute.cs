using Shaders.CurseEffect;
using UnityEngine;

namespace Player
{
    public class Attribute : MonoBehaviour
    {
        [Header("Sanity Attributes")]
        public float currentSanity;
        [Tooltip("Total seconds a player survives outside the safe zone")]
        public float maxUnsafeTime = 180f;
        [Tooltip("How fast sanity recovers per second when safe")]
        public float healRate = 10f;
        private const float MaxSanity = 100f;
        private float _unsafeTime;
        private float _drainConstant;

        [Header("Void Settings")] 
        public float yPosition;
        private Transform _respawnPoint;

        [Header("Player State")] 
        public bool isSafe;

        [Header("Shader Settings")] 
        public float effectFadeOutSpeed = 0.5f;
        public float effectFadeInSpeed = 0.3f;
        private float _currentIntensity;
        private CurseEffectController _curseEffectController;
        

        private void Awake()
        {
            _respawnPoint = GameObject.FindGameObjectWithTag("Respawn").transform;
            _curseEffectController = FindAnyObjectByType<CurseEffectController>();
        }

        private void Start()
        {
            currentSanity = MaxSanity;
            yPosition = -10f;
            
            // K = 200 / T^2
            _drainConstant = 200f / (maxUnsafeTime * maxUnsafeTime);
        }

        private void Update()
        {
            CheckVoid();
            PerformSanity();
            UpdateCurseEffect();
        }
        
        private void CheckVoid()
        {
            if (gameObject.transform.position.y < yPosition)
            {
                //Play animation needed before lerp player position into respawn Point
                transform.position = _respawnPoint.position;
            }
        }
        
        private void PerformSanity()
        {
            if (isSafe)
            {
                _unsafeTime = 0f;
                currentSanity = Mathf.MoveTowards(currentSanity, MaxSanity, healRate * Time.deltaTime);
            }
            else
            {
                _unsafeTime += Time.deltaTime;
                
                //Drain rate  = k * t -> grows the longer you stay unsafe
                float drainRate = _drainConstant * _unsafeTime;
                currentSanity -= drainRate * Time.deltaTime;
                currentSanity = Mathf.Max(currentSanity, 0);
                
                //Handle player death at 0 sanity
                if (currentSanity <= 0f)
                {
                    OnSanityEmpty();
                }
            }
        }

        private void UpdateCurseEffect()
        {
            if (!_curseEffectController) return;
            float target;

            if (isSafe)
            {
                target = 0f;
                //Slowly safe out when back into safe zone
                _currentIntensity = Mathf.MoveTowards(_currentIntensity, target, effectFadeOutSpeed * Time.deltaTime);
            }
            else
            {
                float normalized = _unsafeTime / maxUnsafeTime;
                target = Mathf.Clamp01(normalized * normalized);
                
                _currentIntensity = Mathf.MoveTowards(_currentIntensity, target, effectFadeInSpeed * Time.deltaTime);
            }
            _curseEffectController.ChangeIntensity(_currentIntensity);
        }
        
        private void OnSanityEmpty()
        {
            //Died Action
            Debug.Log("Sanity Empty");
        }
    }
}