using Player;
using UnityEngine;

namespace Soul
{
    public class SoulEffectController : MonoBehaviour
    {
        public Material soulMaterial;
        private Attribute _soulAttribute;
        private static readonly int SanityID = Shader.PropertyToID("_Sanity");
        private static readonly int SpawnTimeID = Shader.PropertyToID("_SpawnTime");


        private void Awake()
        {
            _soulAttribute = FindAnyObjectByType<Attribute>();
        }
        private void Start()
        {
            // Trigger the spawn entrance animation
            Spawn();
        }

        // Call this any time you want to replay the entrance animation
        public void Spawn()
        {
            if (soulMaterial == null) return;
            soulMaterial.SetFloat(SpawnTimeID, Time.time);
        }
        private void Update()
        {
            if (!soulMaterial || !_soulAttribute) return;

            // currentSanity is 0..100, shader expects 0..1
            float sanity = _soulAttribute.currentSanity / 100f;
            soulMaterial.SetFloat(SanityID, sanity);
        }
    }
}