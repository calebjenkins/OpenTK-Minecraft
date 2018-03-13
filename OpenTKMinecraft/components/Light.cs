﻿using System.Runtime.InteropServices;
using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics;
using OpenTK;

namespace OpenTKMinecraft.Components
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Light
    {
        private Vector4 _pos;
        private Vector4 _dir;
        private Color4 _col;
        private float _exp;
        private float _falloff;
        private LightMode _mode;
        private uint _is_act;


        public Vector3 Position
        {
            set => _pos = new Vector4(value, 1);
            get => _pos.Xyz;
        }

        public Vector3 Direction
        {
            set => _dir = new Vector4(value, 0);
            get => _dir.Xyz;
        }

        public Color4 Color
        {
            set => _col = new Color4(value.R, value.G, value.B, _col.A);
            get => new Color4(_col.R, _col.G, _col.B, 1);
        }

        public float Intensity
        {
            set => _col.A = value;
            get => _col.A;
        }

        public float Exponent
        {
            set => _exp = Math.Max(1, _exp);
            get => _exp;
        }

        public float Falloff
        {
            set => _falloff = Math.Max(0, _exp);
            get => _falloff;
        }

        public LightMode Mode
        {
            set => _mode = value;
            get => _mode;
        }

        public bool IsActive
        {
            set => _is_act = value ? 1u : 0;
            get => _is_act == 1;
        }


        public override string ToString() => $"({(IsActive ? "" : "in")}active) P:{Position}  D:{Direction}  C:{Color}  I:{Intensity}  E:{Exponent}  F:{Falloff}  M:{Mode}";

        public static Light CreateEnvironmentLight(Vector3 position, Color4 color, float intensity = 1) => new Light
        {
            Falloff = 0,
            Exponent = 0,
            Color = color,
            IsActive = true,
            Position = position,
            Intensity = intensity,
            Mode = LightMode.Ambient,
        };

        public static Light CreatePointLight(Vector3 position, Color4 color, float intensity = 1, float falloff = .01f) => new Light
        {
            Exponent = 0,
            Falloff = falloff,
            Color = color,
            IsActive = true,
            Position = position,
            Intensity = intensity,
            Mode = LightMode.PointLight,
        };
    }

    public enum LightMode
        : uint
    {
        Ambient = 0,
        PointLight = 1,
        SpotLight = 2,
        Directional = 3
    }

    public unsafe sealed class Lights
        : Renderable
    {
        public const int MAX_LIGHTS = 128;
        public readonly Light[] LightData = new Light[MAX_LIGHTS];
        private int _index, _buffer;


        public Lights(ShaderProgram program)
            : base(program, 0)
        {
            program.Use();

            _index = GL.GetUniformBlockIndex(program.ID, "LightBlock");

            GL.GetActiveUniformBlock(program.ID, _index, ActiveUniformBlockParameter.UniformBlockDataSize, out int datsz);

            if (datsz != sizeof(Light) * MAX_LIGHTS)
                throw new InvalidProgramException("Some internal error occured resulting in the shader not being capable of accepting the light data.");

            GL.UniformBlockBinding(program.ID, _index, 1);

            _buffer = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.UniformBuffer, _buffer);
            // GL.BindBufferBase(BufferRangeTarget.UniformBuffer, _index, _buffer);
            GL.BufferData(BufferTarget.UniformBuffer, datsz, LightData, BufferUsageHint.DynamicDraw);
            GL.BindBufferRange(BufferRangeTarget.UniformBuffer, _index, _buffer, IntPtr.Zero, datsz);
            // GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        public override void Bind()
        {
            GL.BindBuffer(BufferTarget.UniformBuffer, _buffer);
            // GL.BindBufferBase(BufferRangeTarget.UniformBuffer, _index, _buffer);
            // GL.BufferData(BufferTarget.UniformBuffer, sizeof(Light) * MAX_LIGHTS, LightData, BufferUsageHint.DynamicDraw);
            GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, sizeof(Light) * MAX_LIGHTS, LightData);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        protected override void Dispose(bool disposing)
        {
            GL.DeleteBuffer(_buffer);

            base.Dispose(disposing);
        }
    }
}
