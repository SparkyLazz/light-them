using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Shaders
{
    public class CurseZoneDistortionFeature : ScriptableRendererFeature
    {
        [Tooltip("Material using Custom/2D/CurseEffect shader.")]
        public Material curseZoneMaterial;

        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        private CurseZonePass _pass;

        public override void Create()
        {
            _pass = new CurseZonePass(name)
            {
                renderPassEvent = renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (curseZoneMaterial == null)
            {
                Debug.LogWarning("[CurseZoneFeature] curseZoneMaterial is not assigned!");
                return;
            }

            _pass.Setup(curseZoneMaterial);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing) => _pass?.Dispose();
    }

    public sealed class CurseZonePass : ScriptableRenderPass, IDisposable
    {
        private sealed class PassData
        {
            public TextureHandle Src;
            public Material      Mat;
        }

        private readonly string _tag;
        private Material _mat;

        public CurseZonePass(string tag)
        {
            _tag = tag;
            requiresIntermediateTexture = true;
        }

        public void Setup(Material mat) => _mat = mat;

        public override void RecordRenderGraph(RenderGraph rg, ContextContainer frameData)
        {
            var res = frameData.Get<UniversalResourceData>();

            if (res.isActiveTargetBackBuffer)
            {
                Debug.LogError("[CurseZonePass] Set 'Intermediate Texture' = Always on URP Renderer!");
                return;
            }

            TextureHandle src  = res.activeColorTexture;
            var           desc = rg.GetTextureDesc(src);
            desc.name        = "_CurseZoneDst";
            desc.clearBuffer = false;
            TextureHandle dst = rg.CreateTexture(desc);

            using var builder = rg.AddRasterRenderPass<PassData>(_tag, out var data);
            data.Src = src;
            data.Mat = _mat;

            builder.UseTexture(src);
            builder.SetRenderAttachment(dst, 0);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (PassData d, RasterGraphContext ctx) =>
                Blitter.BlitTexture(ctx.cmd, d.Src, new Vector4(1f, 1f, 0f, 0f), d.Mat, 0));

            res.cameraColor = dst;
        }

        public void Dispose() { }
    }
}