using UnityEngine;

namespace Game
{
    public class SoulSpawn : MonoBehaviour
    {
        public Material soulMaterial;

        MaterialPropertyBlock _mpb;
        UnityEngine.Renderer _renderer;

        static readonly int PropSpawnTime = Shader.PropertyToID("_SpawnTime");

        void Awake()
        {
            _renderer = GetComponent<UnityEngine.Renderer>();
            _mpb      = new MaterialPropertyBlock();

            // play on first spawn
            TriggerSpawn();
        }

        // call this on respawn too
        public void TriggerSpawn()
        {
            if (!_renderer) return;

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(PropSpawnTime, Time.time);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}