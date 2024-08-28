using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BrewLib.Graphics
{
    public interface RenderState
    {
        void Apply();
    }

    public class RenderStates
    {
        public static readonly RenderStates Default = new RenderStates();

        public BlendingFactorState BlendingFactor = BlendingFactorState.Default;
        public BlendingEquationState BlendingEquation = BlendingEquationState.Default;
        public CullFaceState CullFace = CullFaceState.Default2d;
        public DepthState Depth = DepthState.Default2d;
        public PointSpriteState PointSprite = PointSpriteState.Default;

        private static readonly List<FieldInfo> fields = new List<FieldInfo>(typeof(RenderStates).GetFields());
        private static readonly Dictionary<Type, RenderState> currentStates = new Dictionary<Type, RenderState>();

        public void Apply()
        {
            var flushed = false;
            foreach (var field in fields)
            {
                if (field.IsStatic)
                    continue;

                var newState = (RenderState)field.GetValue(this);

                if (currentStates.TryGetValue(field.FieldType, out RenderState currentState) && currentState.Equals(newState))
                    continue;

                if (!flushed)
                {
                    DrawState.FlushRenderer();
                    flushed = true;
                }

                newState.Apply();
                currentStates[field.FieldType] = newState;
            }
        }

        public override string ToString() => string.Join("\n", currentStates.Values);

        public static void ClearStateCache()
            => currentStates.Clear();
    }

    public class BlendingFactorState : RenderState, IEquatable<BlendingFactorState>
    {
        private readonly bool enabled = true;
        private readonly BlendingFactorSrc src = BlendingFactorSrc.SrcAlpha;
        private readonly BlendingFactorDest dest = BlendingFactorDest.OneMinusSrcAlpha;
        private readonly BlendingFactorSrc alphaSrc = BlendingFactorSrc.SrcAlpha;
        private readonly BlendingFactorDest alphaDest = BlendingFactorDest.OneMinusSrcAlpha;

        public static BlendingFactorState Default = new BlendingFactorState();

        public BlendingFactorState() { }
        public BlendingFactorState(BlendingMode mode)
        {
            switch (mode)
            {
                case BlendingMode.Off:
                    enabled = false;
                    break;
                case BlendingMode.Alphablend:
                    src = alphaSrc = BlendingFactorSrc.SrcAlpha;
                    dest = alphaDest = BlendingFactorDest.OneMinusSrcAlpha;
                    break;
                case BlendingMode.Color:
                    src = BlendingFactorSrc.SrcAlpha;
                    dest = BlendingFactorDest.OneMinusSrcAlpha;
                    alphaSrc = BlendingFactorSrc.Zero;
                    alphaDest = BlendingFactorDest.One;
                    break;
                case BlendingMode.Additive:
                    src = alphaSrc = BlendingFactorSrc.SrcAlpha;
                    dest = alphaDest = BlendingFactorDest.One;
                    break;
                case BlendingMode.Premultiply:
                    src = BlendingFactorSrc.SrcAlpha;
                    dest = BlendingFactorDest.OneMinusSrcAlpha;
                    alphaSrc = BlendingFactorSrc.One;
                    alphaDest = BlendingFactorDest.OneMinusSrcAlpha;
                    break;
                case BlendingMode.BlendAdd:
                case BlendingMode.Premultiplied:
                    src = alphaSrc = BlendingFactorSrc.One;
                    dest = alphaDest = BlendingFactorDest.OneMinusSrcAlpha;
                    break;
            }
        }
        public BlendingFactorState(BlendingFactorSrc src, BlendingFactorDest dest)
        {
            this.src = alphaSrc = src;
            this.dest = alphaDest = dest;
        }
        public BlendingFactorState(BlendingFactorSrc src, BlendingFactorDest dest, BlendingFactorSrc alphaSrc, BlendingFactorDest alphaDest)
        {
            this.src = src;
            this.dest = dest;
            this.alphaSrc = alphaSrc;
            this.alphaDest = alphaDest;
        }

        public void Apply()
        {
            DrawState.SetCapability(EnableCap.Blend, enabled);
            if (enabled) GL.BlendFuncSeparate(src, dest, alphaSrc, alphaDest);
        }

        public bool Equals(BlendingFactorState other)
        {
            if (!enabled && !other.enabled)
                return true;

            return enabled == other.enabled && src == other.src && dest == other.dest &&
                alphaSrc == other.alphaSrc && alphaDest == other.alphaDest;
        }

        public override string ToString() => $"BlendingFactor src:{src}, dest:{dest}, alphaSrc:{alphaSrc}, alphaDest:{alphaDest}";
    }

    public class BlendingEquationState : RenderState, IEquatable<BlendingEquationState>
    {
        private readonly BlendEquationMode colorMode = BlendEquationMode.FuncAdd;
        private readonly BlendEquationMode alphaMode = BlendEquationMode.FuncAdd;

        public static BlendingEquationState Default = new BlendingEquationState();

        public BlendingEquationState() { }
        public BlendingEquationState(BlendEquationMode mode)
        {
            colorMode = alphaMode = mode;
        }
        public BlendingEquationState(BlendEquationMode colorMode, BlendEquationMode alphaMode)
        {
            this.colorMode = colorMode;
            this.alphaMode = alphaMode;
        }

        public void Apply() => GL.BlendEquationSeparate(colorMode, alphaMode);

        public bool Equals(BlendingEquationState other)
            => colorMode == other.colorMode && alphaMode == other.alphaMode;

        public override string ToString() => $"BlendingEquation colorMode:{colorMode}, alphaMode:{alphaMode}";
    }

    public class DepthState : RenderState, IEquatable<DepthState>
    {
        private readonly DepthFunction? test;
        private readonly bool write;

        public static DepthState Default2d = new DepthState(null, false);
        public static DepthState Default3dOpaque = new DepthState(DepthFunction.Less, true);
        public static DepthState Default3dTransparent = new DepthState(DepthFunction.Less, false);

        public DepthState(DepthFunction? test, bool write)
        {
            this.test = test;
            this.write = write;
        }

        public void Apply()
        {
            DrawState.SetCapability(EnableCap.DepthTest, test.HasValue);
            if (test.HasValue)
                GL.DepthFunc(test.Value);
            GL.DepthMask(write);
        }

        public bool Equals(DepthState other)
            => test == other.test && write == other.write && test == other.test;

        public override string ToString() => $"Depth test:{test}, write:{write}";
    }

    public class CullFaceState : RenderState, IEquatable<CullFaceState>
    {
        private readonly CullFaceMode? mode;

        public static CullFaceState Default2d = new CullFaceState(null);
        public static CullFaceState Default3d = new CullFaceState(CullFaceMode.Back);

        public CullFaceState(CullFaceMode? mode)
        {
            this.mode = mode;
        }

        public void Apply()
        {
            DrawState.SetCapability(EnableCap.CullFace, mode.HasValue);
            if (mode.HasValue) GL.CullFace(mode.Value);
        }

        public bool Equals(CullFaceState other)
            => mode == other.mode;

        public override string ToString() => $"CullFace mode:{mode}";
    }

    public class PointSpriteState : RenderState, IEquatable<PointSpriteState>
    {
        private readonly bool enabled;
        private readonly bool sizeEnabled;

        public static readonly PointSpriteState Default = new PointSpriteState(false, false);

        public PointSpriteState(bool enabled, bool sizeEnabled)
        {
            this.enabled = enabled;
            this.sizeEnabled = sizeEnabled;
        }

        public void Apply()
        {
            DrawState.SetCapability(EnableCap.PointSprite, enabled);
            DrawState.SetCapability(EnableCap.ProgramPointSize, sizeEnabled);
        }

        public bool Equals(PointSpriteState other)
            => enabled == other.enabled;

        public override string ToString() => $"PointSize mode:{enabled}, size:{sizeEnabled}";
    }
}
