using UnityEngine;

namespace Jointure
{
    public class ControllerRig : MonoBehaviour
    {
        [Header("Transforms")]
        public Transform CameraOffsetTransform;
        public Transform CameraTransform;
        public Transform MenuCameraTransform;
        public Transform LeftControllerTransform;
        public Transform RightControllerTransform;
        public Transform FloorOffsetTransform;
        [Header("IK Targets")]
        public Transform HeadTarget;
        public Transform LeftHandTarget;
        public Transform RightHandTarget;

        private void Awake()
        {
            CameraTransform.GetComponent<Camera>().cullingMask = ~LayerMask.GetMask("JointureMenu");
            MenuCameraTransform.GetComponent<Camera>().cullingMask = LayerMask.GetMask("JointureMenu");
        }
    }
}
