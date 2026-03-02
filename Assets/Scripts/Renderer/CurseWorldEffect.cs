using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Renderer
{
    public class CursedWorldEffect : ScriptableRendererFeature
    {
        class CursedWorldPass : ScriptableRenderPass
        {
            private readonly Material _mat;
            private const string KPassName = "CursedWorldEffect";

            public CursedWorldPass(Material m)
            {
                _mat = m;
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
                requiresIntermediateTexture = true;
            }

            class PassData
            {
                public TextureHandle Src;
                public Material Mat;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph,
                                                   ContextContainer frameData)
            {
                if (_mat == null) return;

                var resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer) return;

                TextureHandle srcHandle = resourceData.activeColorTexture;

                var desc         = renderGraph.GetTextureDesc(srcHandle);
                desc.name        = "_CursedWorldTemp";
                desc.clearBuffer = false;
                TextureHandle tempHandle = renderGraph.CreateTexture(desc);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>
                                                (KPassName, out var passData))
                {
                    passData.Src = srcHandle;
                    passData.Mat = _mat;

                    builder.UseTexture(srcHandle);
                    builder.SetRenderAttachment(tempHandle, 0);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, data.Src,
                                            new Vector4(1, 1, 0, 0),
                                            data.Mat, 0);
                    });
                }

                resourceData.cameraColor = tempHandle;
            }
        }

        public Material cursedMaterial;
        private CursedWorldPass _pass;

        public override void Create()
        {
            _pass = new CursedWorldPass(cursedMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer,
                                             ref RenderingData data)
        {
            if (cursedMaterial == null) return;
            renderer.EnqueuePass(_pass);
        }
    }
}