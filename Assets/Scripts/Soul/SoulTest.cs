using Player;
using UnityEngine;

namespace Soul
{
    public class SoulTester : MonoBehaviour
    {
        public SoulFormation soulManager;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                soulManager.AddSoul();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                soulManager.RemoveSoul();
            }
        }
    }
}