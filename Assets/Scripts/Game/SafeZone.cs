using Player;
using UnityEngine;

namespace Game
{
    public class SafeZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Attribute attribute))
            {
                attribute.isSafe = true;
            }
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out Attribute attribute))
            {
                attribute.isSafe = false;
            }
        }
    }
}