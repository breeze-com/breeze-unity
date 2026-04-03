using UnityEngine;

namespace BreezeSdk.BreezeDemo
{
    public class RotatingCube : MonoBehaviour
    {
        [SerializeField] Vector3 _rotationAxis = Vector3.up;

        float _rotationSpeed;
        AnimationCurve _curve;
        float _duration;
        float _elapsed;
        bool _isRotating;

        void Update()
        {
            if (!_isRotating) return;

            _elapsed += Time.deltaTime;
            if (_elapsed >= _duration)
            {
                _isRotating = false;
                return;
            }

            float t = _elapsed / _duration;
            float curveMultiplier = _curve != null ? _curve.Evaluate(t) : 1f;
            float angle = _rotationSpeed * curveMultiplier * Time.deltaTime;
            transform.Rotate(_rotationAxis, angle, Space.Self);
        }

        /// <summary>
        /// Start rotating the cube.
        /// </summary>
        /// <param name="speed">Rotation speed in degrees per second.</param>
        /// <param name="curve">Easing curve over time (0–1). Null uses constant speed.</param>
        /// <param name="duration">How long the rotation runs in seconds.</param>
        public void StartRotating(float speed, AnimationCurve curve, float duration)
        {
            _rotationSpeed = speed;
            _curve = curve;
            _duration = Mathf.Max(0f, duration);
            _elapsed = 0f;
            _isRotating = _duration > 0f;
        }

        /// <summary>
        /// Start rotating with a given axis (default is Vector3.up).
        /// </summary>
        public void StartRotating(float speed, AnimationCurve curve, float duration, Vector3 axis)
        {
            _rotationAxis = axis.normalized;
            StartRotating(speed, curve, duration);
        }

        /// <summary>
        /// Stop any current rotation.
        /// </summary>
        public void StopRotating()
        {
            _isRotating = false;
        }

        public bool IsRotating => _isRotating;
    }
}