using UnityEngine;

namespace Jointure.Samples
{
    [RequireComponent(typeof(LineRenderer))]
    public class Rope : MonoBehaviour
    {
        [Header("Rope Transforms")]
        public Transform StartPoint;
        public Transform EndPoint;
        [Header("Rope Settings")]
        public int LinePoints = 10;
        public float Stiffness = 10000f;
        public float Damping = 20f;
        public float RopeLength = 2f;
        public float RopeWidth = 0.01f;

        private float _currentValue;
        private float _currentVelocity;
        private float _targetValue;
        private float _valueThreshold = 0.01f;
        private float velocityThreshold = 0.01f;
        private LineRenderer _lineRenderer;

        private void Start()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _currentValue = GetMidPoint().y;
            SimulatePhysics();
        }

        private void Update()
        {
            SetSplinePoint();
        }

        public void SetRopeLength(float ropeLength)
        {
            RopeLength = ropeLength * 1.25f;
            _currentValue = GetMidPoint().y;
            RopeLength = ropeLength;
        }

        private void SetSplinePoint()
        {
            _lineRenderer.positionCount = LinePoints + 1;

            Vector3 mid = GetMidPoint();
            _targetValue = mid.y;
            mid.y = _currentValue;

            for (int i = 0; i < LinePoints; i++)
            {
                Vector3 p = GetBezierPoint(StartPoint.position, mid, EndPoint.position, i / (float) LinePoints);
                _lineRenderer.SetPosition(i, p);
            }

            _lineRenderer.SetPosition(LinePoints, EndPoint.position);
        }

        private Vector3 GetMidPoint()
        {
            var (startPointPosition, endPointPosition) = (StartPoint.position, EndPoint.position);
            Vector3 midpos = Vector3.Lerp(startPointPosition, endPointPosition, .5f);
            float yFactor = RopeLength - Mathf.Min(Vector3.Distance(startPointPosition, endPointPosition), RopeLength);
            midpos.y -= yFactor;
            return midpos;
        }

        private Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            Vector3 a = Vector3.Lerp(p0, p1, t);
            Vector3 b = Vector3.Lerp(p1, p2, t);
            Vector3 point = Vector3.Lerp(a, b, t);

            return point;
        }

        private void FixedUpdate()
        {
            SimulatePhysics();
        }

        private void SimulatePhysics()
        {
            float dampingFactor = Mathf.Max(0, 1 - Damping * Time.fixedDeltaTime);
            float acceleration = (_targetValue - _currentValue) * Stiffness * Time.fixedDeltaTime;
            _currentVelocity = _currentVelocity * dampingFactor + acceleration;
            _currentValue += _currentVelocity * Time.fixedDeltaTime;

            if (Mathf.Abs(_currentValue - _targetValue) < _valueThreshold && Mathf.Abs(_currentVelocity) < velocityThreshold)
            {
                _currentValue = _targetValue;
                _currentVelocity = 0f;
            }
        }
    }
}
