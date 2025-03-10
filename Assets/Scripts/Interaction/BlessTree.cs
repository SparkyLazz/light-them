using System.Collections;
using Game;
using UnityEngine;

namespace Interaction
{
    public class BlessTree : MonoBehaviour, IInteractable
    {
        public float blessAmount;
        public ParticleSystem blessEffect;
        private Characters.Movement _interaction;

        private void Start()
        {
            _interaction = FindFirstObjectByType<Characters.Movement>().GetComponent<Characters.Movement>();
            blessEffect = GameObject.Find("BlessEffect").GetComponent<ParticleSystem>();
        }

        public void Interact()
        {
            if (!_interaction.isBlessed) StartCoroutine(PlayerBless());
        }

        IEnumerator PlayerBless()
        {
            blessEffect.Play();
            _interaction.isBlessed = true;
            _interaction.moveSpeed += 8;
            _interaction.jumpForce += 4;
            yield return new WaitForSeconds(blessAmount);
            _interaction.moveSpeed -= 8;
            _interaction.jumpForce -= 4;
            _interaction.isBlessed = false;
            blessEffect.Stop();
        }
    }
}