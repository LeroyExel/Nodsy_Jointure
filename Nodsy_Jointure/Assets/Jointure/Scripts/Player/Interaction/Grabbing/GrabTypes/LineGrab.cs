using UnityEngine;

namespace Jointure
{
    [AddComponentMenu("Jointure/Grabs/Grab (Line)")]
    public class LineGrab : SnapGrab
    {
        public Transform Start, End;

        [SerializeField]
        private bool _allowHandClipping;

        private int _mask;

        private void Awake()
        {
            _mask = ~LayerMask.GetMask("JointureRig");
        }

        public override float EvaluateGrabRank(Transform handTransform)
        {
            if (!_allowHandClipping)
                if (Physics.OverlapSphere(GetNearestPoint(handTransform.position), 0.01f, _mask, QueryTriggerInteraction.Ignore).Length > 0)
                    return 0f;

            float distance = Vector3.Distance(handTransform.position, GetNearestPoint(handTransform.position));
            if (distance <= 0.0001f)
                distance = 0.0001f;

            float dot = Vector3.Dot(handTransform.forward, transform.forward);
            float orientationScore = Mathf.Clamp01((dot + 1f) * 0.5f);

            return (3f / distance) * (0.1f + 0.9f * orientationScore);
        }

        public override float CalculateRank(Transform handTransform) => EvaluateGrabRank(handTransform);

        public override void AlignHand(Hand hand)
        {
            Vector3 point = GetNearestPoint(hand.PalmTransform.position);
            base.AlignHand(hand);
            hand.PhysicsHandTransform.position += point - transform.position;
        }

        public override void CreateCollider()
        {
            GameObject colliderObject = new GameObject("GrabCollider");
            colliderObject.transform.parent = transform;
            CapsuleCollider collider = colliderObject.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            colliderObject.transform.position = Vector3.Lerp(Start.position, End.position, 0.5f);
            colliderObject.transform.up = (Start.position - End.position).normalized;
            collider.radius = 0.01f;
            collider.height = (Start.position - End.position).magnitude;
            Collider = collider;
        }

        private Vector3 GetNearestPoint(Vector3 palmPosition)
        {
            Vector3 lineVector = End.position - Start.position;
            float lineLength = lineVector.magnitude;
            Vector3 lineDirection = lineVector.normalized;

            var v = palmPosition - Start.position;
            var d = Vector3.Dot(v, lineDirection);
            d = Mathf.Clamp(d, 0f, lineLength);
            return Start.position + lineDirection * d;
        }
    }
}