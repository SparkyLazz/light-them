using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace Shaders
{
    public class RaindropRenderFeature : ScriptableRendererFeature
    {
        [FormerlySerializedAs("RaindropMaterial")] [Tooltip("Material created from Custom/Raindrop_URP. Select it to edit all shader properties.")]
        public Material raindropMaterial;

        [Tooltip("Injection point in the URP frame.")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        private RaindropRenderPass _pass;

        public override void Create()
        {
            _pass = new RaindropRenderPass(name)
            {
                renderPassEvent = renderPassEvent,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (raindropMaterial == null)
            {
                Debug.LogWarning("[RaindropRenderFeature] Assign a material created from Custom/Raindrop_URP.", this);
                return;
            }

            _pass.Setup(raindropMaterial);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
        }
    }

    public sealed class RaindropRenderPass : ScriptableRenderPass, IDisposable
    {
        private sealed class PassData
        {
            public TextureHandle Src;
            public Material      Material;
        }

        private readonly string _tag;
        private          Material _material;

        public RaindropRenderPass(string tag)
        {
            _tag                        = tag;
            requiresIntermediateTexture = true;
        }

        public void Setup(Material material)
        {
            _material = material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogWarning("[RaindropRenderPass] Set 'Intermediate Texture' to Always on the URP Renderer asset.");
                return;
            }

            TextureHandle source = resourceData.activeColorTexture;

            var desc         = renderGraph.GetTextureDesc(source);
            desc.name        = "_RaindropOutput";
            desc.clearBuffer = false;
            TextureHandle dest = renderGraph.CreateTexture(desc);

            using var builder = renderGraph.AddRasterRenderPass<PassData>(_tag, out var data);

            data.Src      = source;
            data.Material = _material;

            builder.UseTexture(source);
            builder.SetRenderAttachment(dest, 0);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (PassData d, RasterGraphContext ctx) =>
                Blitter.BlitTexture(ctx.cmd, d.Src, new Vector4(1f, 1f, 0f, 0f), d.Material, 0));

            resourceData.cameraColor = dest;
        }

        public void Dispose() { }
    }
}