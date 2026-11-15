using UnityEngine;

namespace Jointure
{
    [AddComponentMenu("Jointure/Grabs/Grab (Snap)")]
    public class SnapGrab : Grab
    {
        public override float EvaluateGrabRank(Transform handTransform)
        {
            float baseRank = base.EvaluateGrabRank(handTransform) * 3f;
            float dot = Vector3.Dot(handTransform.forward, transform.forward);
            float orientationScore = Mathf.Clamp01((dot + 1f) * 0.5f);
            return baseRank * (0.1f + 0.9f * orientationScore);
        }

        public override float CalculateRank(Transform handTransform) => EvaluateGrabRank(handTransform);

        public override void AlignHand(Hand hand)
        {
            hand.PhysicsHandTransform.position = transform.TransformPoint(hand.PalmTransform.InverseTransformPoint(hand.PhysicsHandTransform.position));
            hand.PhysicsHandTransform.rotation = transform.rotation * Quaternion.Inverse(hand.PalmTransform.rotation) * hand.PhysicsHandTransform.rotation;
        }
    }
}