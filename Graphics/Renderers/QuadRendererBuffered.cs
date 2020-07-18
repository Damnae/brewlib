using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Renderers.PrimitiveStreamers;
using BrewLib.Graphics.Shaders;
using BrewLib.Graphics.Shaders.Snippets;
using BrewLib.Graphics.Textures;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;

namespace BrewLib.Graphics.Renderers
{
    public class QuadRendererBuffered : QuadRenderer
    {
        public const int VertexPerQuad = 4;
        public const string CombinedMatrixUniformName = "u_combinedMatrix";
        public const string TextureUniformName = "u_texture";

        public static readonly VertexDeclaration VertexDeclaration =
            new VertexDeclaration(VertexAttribute.CreatePosition2d(), VertexAttribute.CreateDiffuseCoord(0), VertexAttribute.CreateColor(true));

        public delegate int CustomTextureBinder(BindableTexture texture);
        public CustomTextureBinder CustomTextureBind;

        #region Default Shader

        public static Shader CreateDefaultShader()
        {
            var sb = new ShaderBuilder(VertexDeclaration);

            var combinedMatrix = sb.AddUniform(CombinedMatrixUniformName, "mat4");
            var texture = sb.AddUniform(TextureUniformName, "sampler2D");

            var color = sb.AddVarying("vec4");
            var textureCoord = sb.AddVarying("vec2");

            sb.VertexShader = new Sequence(
                new Assign(color, sb.VertexDeclaration.GetAttribute(AttributeUsage.Color)),
                new Assign(textureCoord, sb.VertexDeclaration.GetAttribute(AttributeUsage.DiffuseMapCoord)),
                new Assign(sb.GlPosition, () => $"{combinedMatrix.Ref} * vec4({sb.VertexDeclaration.GetAttribute(AttributeUsage.Position).Name}, 0, 1)")
            );
            sb.FragmentShader = new Sequence(
                new Assign(sb.GlFragColor, () => $"{color.Ref} * texture2D({texture.Ref}, {textureCoord.Ref})")
            );

            return sb.Build();
        }

        #endregion

        private Shader shader;
        private bool ownsShader;
        public Shader Shader => ownsShader ? null : shader;

        private Action flushAction;
        public Action FlushAction
        {
            get { return flushAction; }
            set { flushAction = value; }
        }

        private PrimitiveStreamer<QuadPrimitive> primitiveStreamer;
        private QuadPrimitive[] primitives;

        private Camera camera;
        public Camera Camera
        {
            get { return camera; }
            set
            {
                if (camera == value)
                    return;

                if (rendering) DrawState.FlushRenderer();
                camera = value;
            }
        }

        private Matrix4 transformMatrix = Matrix4.Identity;
        public Matrix4 TransformMatrix
        {
            get { return transformMatrix; }
            set
            {
                if (transformMatrix.Equals(value))
                    return;

                DrawState.FlushRenderer();
                transformMatrix = value;
            }
        }

        private int quadsInBatch;
        private readonly int maxQuadsPerBatch;

        private BindableTexture currentTexture;
        private int currentSamplerUnit;
        private bool rendering;

        private int currentLargestBatch;

        public int RenderedQuadCount { get; private set; }
        public int FlushedBufferCount { get; private set; }
        public int DiscardedBufferCount => primitiveStreamer.DiscardedBufferCount;
        public int BufferWaitCount => primitiveStreamer.BufferWaitCount;
        public int LargestBatch { get; private set; }

        public QuadRendererBuffered(Shader shader = null, Action flushAction = null, int maxQuadsPerBatch = 4096, int primitiveBufferSize = 0) :
            this(PrimitiveStreamerUtil<QuadPrimitive>.DefaultCreatePrimitiveStreamer, shader, flushAction, maxQuadsPerBatch, primitiveBufferSize)
        {
        }

        public QuadRendererBuffered(CreatePrimitiveStreamerDelegate<QuadPrimitive> createPrimitiveStreamer, Shader shader = null, Action flushAction = null, int maxQuadsPerBatch = 4096, int primitiveBufferSize = 0)
        {
            if (shader == null)
            {
                shader = CreateDefaultShader();
                ownsShader = true;
            }

            this.maxQuadsPerBatch = maxQuadsPerBatch;
            this.flushAction = flushAction;
            this.shader = shader;

            var primitiveBatchSize = Math.Max(maxQuadsPerBatch, primitiveBufferSize / (VertexPerQuad * VertexDeclaration.VertexSize));
            primitiveStreamer = createPrimitiveStreamer(VertexDeclaration, primitiveBatchSize * VertexPerQuad);

            primitives = new QuadPrimitive[maxQuadsPerBatch];
            Trace.WriteLine($"Initialized {nameof(QuadRenderer)} using {primitiveStreamer.GetType().Name}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (rendering)
                EndRendering();

            primitives = null;

            primitiveStreamer.Dispose();
            primitiveStreamer = null;

            if (ownsShader) shader.Dispose();
            shader = null;
        }

        public void BeginRendering()
        {
            if (rendering) throw new InvalidOperationException("Already rendering");

            shader.Begin();
            primitiveStreamer.Bind(shader);

            rendering = true;
        }

        public void EndRendering()
        {
            if (!rendering) throw new InvalidOperationException("Not rendering");

            primitiveStreamer.Unbind();
            shader.End();

            currentTexture = null;
            rendering = false;
        }

        private bool lastFlushWasBuffered = false;
        public void Flush(bool canBuffer = false)
        {
            if (quadsInBatch == 0)
                return;

            if (currentTexture == null)
                throw new InvalidOperationException("currentTexture is null");

            // When the previous flush was bufferable, draw state should stay the same.
            if (!lastFlushWasBuffered)
            {
                var combinedMatrix = transformMatrix * Camera.ProjectionView;
                GL.UniformMatrix4(shader.GetUniformLocation(CombinedMatrixUniformName), false, ref combinedMatrix);

                var samplerUnit = CustomTextureBind != null ? CustomTextureBind(currentTexture) : DrawState.BindTexture(currentTexture);
                if (currentSamplerUnit != samplerUnit)
                {
                    currentSamplerUnit = samplerUnit;
                    GL.Uniform1(shader.GetUniformLocation(TextureUniformName), currentSamplerUnit);
                }

                flushAction?.Invoke();
            }

            primitiveStreamer.Render(PrimitiveType.Quads, primitives, quadsInBatch, quadsInBatch * VertexPerQuad, canBuffer);

            currentLargestBatch += quadsInBatch;
            if (!canBuffer)
            {
                LargestBatch = Math.Max(LargestBatch, currentLargestBatch);
                currentLargestBatch = 0;
            }

            quadsInBatch = 0;
            FlushedBufferCount++;

            lastFlushWasBuffered = canBuffer;
        }

        public void Draw(ref QuadPrimitive quad, Texture2dRegion texture)
        {
            if (!rendering) throw new InvalidOperationException("Not rendering");
            if (texture == null) throw new ArgumentNullException(nameof(texture));

            if (currentTexture != texture.BindableTexture)
            {
                DrawState.FlushRenderer();
                currentTexture = texture.BindableTexture;
            }
            else if (quadsInBatch == maxQuadsPerBatch)
                DrawState.FlushRenderer(true);

            primitives[quadsInBatch] = quad;

            RenderedQuadCount++;
            quadsInBatch++;
        }
    }
}
