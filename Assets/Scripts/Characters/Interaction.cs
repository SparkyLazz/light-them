using Game;
using UnityEngine;

namespace Characters
{
    public class Interaction : MonoBehaviour
    {
        public float interactRange = 2f;
        public GameObject interactIcon;
        public LayerMask interactableMask;
        
        private Transform _respawnArea;

        private void Start()
        {
            _respawnArea = GameObject.Find("RespawnArea").transform;
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * transform.localScale.x,
                    interactRange, interactableMask);

                if (hit.collider is not null && hit.collider.TryGetComponent(out IInteractable interactable))
                {
                    interactable.Interact();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Interaction"))
            {
                interactIcon.SetActive(true);
            }
            else
            {
                gameObject.transform.position = _respawnArea.position;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Interaction"))
            {
                interactIcon.SetActive(false);
            }
        }
    }
}