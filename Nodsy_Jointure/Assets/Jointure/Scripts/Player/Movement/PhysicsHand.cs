using UnityEngine;

namespace Jointure
{
    public class PhysicsHand : MonoBehaviour
    {
        public Transform Target, Controller;
        public Vector3 TargetOffsetPosition;
        public Quaternion TargetOffsetRotation;

        private Player _player;

        private ConfigurableJoint _handJoint;

        private void Awake()
        {
            _player = GetComponentInParent<Player>();

            GetComponent<Rigidbody>().solverIterations = 60;
            GetComponent<Rigidbody>().solverVelocityIterations = 10;

            TargetOffsetRotation = Quaternion.identity;
            _handJoint = GetComponent<ConfigurableJoint>();
        }

        private void FixedUpdate()
        {
            Vector3 targetPosition = Target.TransformPoint(TargetOffsetPosition);
            Vector3 headOffset = targetPosition - _player.PhysicsRig.HeadRigidbody.position;
            _handJoint.targetPosition = headOffset;

            //Rotation
            _handJoint.targetRotation = Target.rotation * TargetOffsetRotation;
        }
    }
}