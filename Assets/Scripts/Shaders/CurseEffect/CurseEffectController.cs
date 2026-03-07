using UnityEngine;

namespace Shaders.CurseEffect
{
    public class CurseEffectController : MonoBehaviour
    {
        private static readonly int Intensity = Shader.PropertyToID("_Intensity");
        public Material curseEffectMaterial;

        public void ChangeIntensity(float intensity)
        {
            curseEffectMaterial.SetFloat(Intensity, intensity);
        }
    }
}