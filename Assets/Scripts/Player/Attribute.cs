using UnityEngine;

namespace Player
{
    public class Attribute : MonoBehaviour
    {
        [Header("Health")]
        public float maxHp = 100f;
        public float currentHp;

        [Header("Drain Settings")]
        public float baseDrainPerSecond = 5f;
        public float regenPerSecond = 8f;

        private CurseSystem _curse;

        public float NormalizedHp => currentHp / maxHp;

        private void Awake()
        {
            _curse = GetComponent<CurseSystem>();
            currentHp = maxHp;
        }

        private void Update()
        {
            float curseAmount = _curse.CurrentCurse;

            if (curseAmount < 0.2f)
            {
                currentHp += regenPerSecond * Time.deltaTime;
            }
            else
            {
                float multiplier = 1f;

                if (curseAmount > 0.5f)
                {
                    multiplier += (curseAmount - 0.5f) * 6f;
                }

                float drain =
                    baseDrainPerSecond *
                    multiplier *
                    Time.deltaTime;

                currentHp -= drain;
            }

            currentHp = Mathf.Clamp(currentHp, 0f, maxHp);
        }
    }
}