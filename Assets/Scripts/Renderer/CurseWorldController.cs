using UnityEngine;
using Player;

namespace Renderer
{
    public class CurseWorldController : MonoBehaviour
    {
        private static readonly int Amount =
            Shader.PropertyToID("_CurseAmount");

        [SerializeField] private Material curseMaterial;
        [SerializeField] private CurseSystem curse;

        private void Update()
        {
            curseMaterial.SetFloat(Amount, curse.CurrentCurse);
        }
    }
}