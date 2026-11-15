using UnityEngine;

namespace Jointure
{
    public class AnimationRig : MonoBehaviour
    {
        public Feet Feet;

        [Header("Transforms")]
        public Transform CharacterTransform;
        public Transform ArmatureTransform;
        public Transform HipsTransform;
        public Transform LeftFootAnchorTransform;
        public Transform LeftFootTargetTransform;
        public Transform RightFootAnchorTransform;
        public Transform RightFootTargetTransform;
        public Transform HipIKTransform;
    }
}
