using System;
using UnityEngine;

namespace Jointure
{
    [CreateAssetMenu(fileName = "HandPose", menuName = "Jointure/Hand Pose")]
    public class HandPose : ScriptableObject
    {
        public ThumbPoses Thumb;
        public IndexPoses Index;
        public FingerPoses Middle, Ring, Little;
    }

    [Serializable]
    public struct FingerPose
    {
        public Quaternion RootBone, MiddleBone, TipBone;

        public FingerPose(FingerPose pose)
        {
            RootBone = pose.RootBone;
            MiddleBone = pose.MiddleBone;
            TipBone = pose.TipBone;
        }

        public FingerPose Mirrored()
        {
            FingerPose mirroredFingerPose = new FingerPose(this);

            mirroredFingerPose.RootBone.x *= -1;
            mirroredFingerPose.MiddleBone.x *= -1;
            mirroredFingerPose.TipBone.x *= -1;

            mirroredFingerPose.RootBone.w *= -1;
            mirroredFingerPose.MiddleBone.w *= -1;
            mirroredFingerPose.TipBone.w *= -1;

            return mirroredFingerPose;
        }
    }

    [Serializable]
    public class ThumbPoses
    {
        public FingerPose
            Idle,
            ThumbrestTouched,
            PrimaryTouched,
            PrimaryButton,
            SecondaryTouched,
            SecondaryButton,
            ThumbstickTouched;
    }

    [Serializable]
    public class FingerPoses
    {
        public FingerPose Open, Closed;
    }

    [Serializable]
    public class IndexPoses : FingerPoses
    {
        public FingerPose TriggerTouched;
    }
}
