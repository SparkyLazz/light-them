using UnityEngine;

namespace Player
{
    public class Respawn : MonoBehaviour
    {
        private Transform _respawnPosition;

        private void Start()
        {
            _respawnPosition = GameObject.FindGameObjectWithTag("Respawn").transform;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.CompareTag("Obstacle"))
            {
                gameObject.transform.position = _respawnPosition.position;
            }
        }
    }
}