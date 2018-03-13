﻿using System;

using OpenTK;

namespace OpenTKMinecraft.Components
{
    using static System.Math;


    public interface IMovableCamera
    {
        void MoveForwards(float dist);
        void MoveRight(float dist);
        void MoveUp(float dist);
        void MoveBackwards(float dist);
        void MoveLeft(float dist);
        void MoveDown(float dist);
    }

    public abstract class Camera
    {
        public CameraView ViewType { set; get; } = CameraView.Perspective;
        public virtual Vector3 Position { protected set; get; }
        public virtual float FocalDistance { protected set; get; }
        public Matrix4 Projection { private set; get; }
        public float FieldOfView { private set; get; }
        public virtual Vector3 Direction { protected set; get; }

        public float ZoomFactor
        {
            get => FieldOfView;
            set => FieldOfView = Max(2, Min(value, 135));
        }


        public Camera() => ResetZoom();

        protected abstract void Update(double time, double delta);

        internal void Update(double time, double delta, float aspectratio)
        {
            Update(time, delta);

            Projection = Matrix4.LookAt(Position, Position + Direction, Vector3.UnitY);

            if (ViewType == CameraView.Perspective)
                Projection *= Matrix4.CreatePerspectiveFieldOfView((float)(FieldOfView * PI / 180), aspectratio, .01f, 10000f);
        }

        public void ResetZoom() => ZoomFactor = 60;
    }

    public abstract class TargetedCamera
        : Camera
    {
        private Vector3 _targ;

        public sealed override Vector3 Direction => Vector3.Normalize(Target - Position);
        public virtual Vector3 Target
        {
            protected set
            {
                _targ = value;
                FocalDistance = (value - Position).Length;
            }
            get => _targ;

        }
    }

    public sealed class FixedCamera
        : TargetedCamera
    {
        public FixedCamera(Vector3 pos, Vector3 targ)
        {
            Target = targ;
            Position = pos;
        }

        protected override void Update(double time, double delta)
        {
        }
    }

    public sealed class ThirdPersonCamera
        : TargetedCamera
    {
        public GameObject TargetObject { set; get; }
        public Vector3 Offset { set; get; }


        public ThirdPersonCamera(GameObject target)
            : this(target, Vector3.Zero)
        {
        }

        public ThirdPersonCamera(GameObject target, Vector3 offset)
        {
            TargetObject = target;
            Offset = offset;
        }

        protected override void Update(double time, double delta)
        {
            Target = new Vector3(TargetObject.Position);
            Position = new Vector3(TargetObject.Position) + Offset * new Vector3(TargetObject.Direction);
        }
    }

    public sealed class FixedTrackingCamera
        : TargetedCamera
    {
        public GameObject TargetObject { set; get; }


        public FixedTrackingCamera(GameObject target)
            : this(target, Vector3.Zero)
        {
        }

        public FixedTrackingCamera(GameObject target, Vector3 position)
        {
            TargetObject = target;
            Position = position;
        }

        protected override void Update(double time, double delta) => Target = new Vector3(TargetObject.Position);
    }

    [Obsolete("Use 'OpenTKMinecraft::Components::PlayerCamera' instead.")]
    public sealed class MovableCamera
        : TargetedCamera
        , IMovableCamera
    {
        private Vector3 _pos, _targ;
        private float _vθ, _hθ;


        public MovableCameraHint CameraHint { set; get; }

        public Vector3 Up { get; } = Vector3.UnitY;

        public Vector3 View => -W;

        public Vector3 W => _pos == _targ ? Vector3.UnitX : Vector3.Normalize(_pos - _targ);

        public Vector3 U => Vector3.Normalize(Vector3.Cross(Up, W));

        public Vector3 V => Vector3.Normalize(Vector3.Cross(W, U));

        public Vector3 H => Vector3.Normalize(new Vector3(View.X, 0, View.Z));

        /// <summary>
        /// ranging from [-1 ... 1] for [down ... up]
        /// </summary>
        public float VerticalAngle
        {
            get => _vθ;
            internal set
            {
                value = Min(1, Max(value, -1));

                _vθ = value;

                // if (CameraHint.)

                // update position + target
            }
        }

        /// <summary>
        /// ranging from [0 ... 2π] for [N ... W ... S ... E (... N)]
        /// </summary>
        public float HorizontalAngle
        {
            get => _hθ;
            internal set
            {
                value = (float)((value + 2 * PI) % (2 * PI));

                _hθ = value;


                // update position + target
            }
        }

        public override Vector3 Position
        {
            get => _pos;
            protected set
            {
                if (CameraHint == MovableCameraHint.LockAngles)
                    _targ += value - _pos;

                _pos = value;

                UpdateAngles();
            }
        }

        public override Vector3 Target
        {
            get => _targ;
            protected set
            {
                if (CameraHint == MovableCameraHint.LockAngles)
                    _pos += value - _targ;

                _targ = value;

                UpdateAngles();
            }
        }


        public MovableCamera()
            : this(new Vector3(0, 1, 1), Vector3.Zero)
        {
        }

        public MovableCamera(Vector3 pos)
            : this(pos, Vector3.Zero)
        {
        }

        public MovableCamera(Vector3 pos, Vector3 targ)
        {
            Position = pos;
            Target = targ;
        }

        private void UpdateAngles()
        {
            double vangle = Acos(Vector3.Dot(Up, V));
            double hangle = Atan2(H.Z, H.X);

            _vθ = (float)(vangle / PI);
            _vθ = (float)hangle;
        }

        public void Focus(GameObject obj)
        {
            if (obj?.Position is Vector4 v)
                Target = v.Xyz;
        }

        public void MoveForwards(float dist) => Position += dist * H;

        public void MoveRight(float dist) => Position += dist * U;

        public void MoveUp(float dist) => Position += new Vector3(0, dist, 0);

        public void MoveBackwards(float dist) => MoveForwards(-dist);

        public void MoveLeft(float dist) => MoveRight(-dist);

        public void MoveDown(float dist) => MoveUp(-dist);

        public void SetHorizontalPositionAngle(float angle)
        {

        }

        public void SetVerticalPositionAngle(float angle)
        {

        }

        protected override void Update(double time, double delta)
        {
        }
    }

    public sealed class PlayerCamera
        : Camera
        , IMovableCamera
    {
        private const float ε = 1e-4f;
        private float _hθ, _vθ;


        public bool FixedFocus { set; get; }
        public Vector3 Up { get; } = Vector3.UnitY;
        public Vector3 W => Vector3.Normalize(-Direction);
        public Vector3 U { private set; get; }
        public Vector3 V { private set; get; }
        public Vector3 H { private set; get; }

        /// <summary>
        /// ranging from [-1 ... 1] for [down ... up]
        /// </summary>
        public float VerticalAngle
        {
            get => _vθ;
            internal set
            {
                value = Min(1- ε, Max(value, ε - 1));

                _vθ = value;

                UpdateDirections();
            }
        }

        /// <summary>
        /// ranging from [0 ... 2π] for [N ... W ... S ... E (... N)]
        /// </summary>
        public float HorizontalAngle
        {
            get => _hθ;
            internal set
            {
                value = (float)((value + (2 * PI)) % (2 * PI));

                _hθ = value;

                UpdateDirections();
            }
        }


        // https://en.wikibooks.org/wiki/OpenGL_Programming/Depth_of_Field
        public PlayerCamera()
            : this(Vector3.Zero)
        {
        }

        public PlayerCamera(Vector3 pos)
            : this(pos, 0, 0)
        {
        }

        public PlayerCamera(Vector3 pos, float hangle, float vangle)
        {
            Position = pos;
            FixedFocus = false;
            VerticalAngle = vangle;
            HorizontalAngle = hangle;
        }

        private void UpdateDirections()
        {
            Matrix3 rotY = Matrix3.CreateRotationY((float)(_hθ * PI));

            Direction = rotY * Matrix3.CreateRotationZ((float)((_vθ - .5f) * PI)) * Vector3.UnitX;
            U = Vector3.Normalize(Vector3.Cross(Up, W)); // rotY * Vector3.UnitZ
            V = Vector3.Normalize(Vector3.Cross(W, U));
            H = rotY * Vector3.UnitX;

            if (!FixedFocus)
                AutoFocus();
        }

        public void CenterObject(GameObject obj)
        {
            Vector3 targ = obj.Position.Xyz;


            // TODO
        }

        public void AutoFocus()
        {
            // TODO
        }

        public void SetFocus(float distance) => FocalDistance = Max(.01f, distance);

        protected override void Update(double time, double delta)
        {
        }

        public void RotateUp(float delta_degree) => VerticalAngle += delta_degree / 90;

        public void RotateDown(float delta_degree) => RotateUp(-delta_degree);

        public void RotateLeft(float delta_degree) => HorizontalAngle += (float)(delta_degree / 180 * PI);

        public void RotateRight(float delta_degree) => RotateLeft(-delta_degree);

        public void MoveForwards(float dist) => Position += H * dist;

        public void MoveRight(float dist) => Position += U * dist;

        public void MoveUp(float dist) => Position += Up * dist;

        public void MoveBackwards(float dist) => MoveForwards(-dist);

        public void MoveLeft(float dist) => MoveRight(-dist);

        public void MoveDown(float dist) => MoveUp(-dist);

        public void MoveTo(Vector3 loc) => Position = loc;

        public void ResetAngles()
        {
            VerticalAngle = 0;
            HorizontalAngle = 0;
        }
    }

    public enum MovableCameraHint
    {
        LockTarget,
        LockAngles,
    }

    public enum CameraView
    {
        Orthogonal,
        Perspective
    }
}
