using UnityEngine;

namespace Player
{
    public class CurseSystem : MonoBehaviour
    {
        [Header("Speeds")]
        public float toHalfSpeed = 0.5f;       // outside → 0.5
        public float toZeroSpeed = 2f;         // safe zone cleanse
        public float extremeSpeed = 0.1f;      // 0.5 → 1 slow climb

        public float CurrentCurse { get; private set; }

        private int _zoneCounter;
        private bool IsInsideZone => _zoneCounter > 0;

        private void Update()
        {
            if (IsInsideZone)
            {
                // Cleanse toward 0 quickly
                CurrentCurse = Mathf.MoveTowards(
                    CurrentCurse,
                    0f,
                    toZeroSpeed * Time.deltaTime
                );
            }
            else
            {
                if (CurrentCurse < 0.5f)
                {
                    // Move smoothly to danger threshold
                    CurrentCurse = Mathf.MoveTowards(
                        CurrentCurse,
                        0.5f,
                        toHalfSpeed * Time.deltaTime
                    );
                }
                else
                {
                    // Slowly escalate to extreme corruption
                    CurrentCurse = Mathf.MoveTowards(
                        CurrentCurse,
                        1f,
                        extremeSpeed * Time.deltaTime
                    );
                }
            }
        }

        public void EnterZone() => _zoneCounter++;
        public void ExitZone() => _zoneCounter = Mathf.Max(0, _zoneCounter - 1);
    }
}