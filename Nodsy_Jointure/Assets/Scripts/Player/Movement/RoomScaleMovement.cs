using UnityEngine;

namespace Jointure
{
    public class RoomScaleMovement : MonoBehaviour
    {
        private Player _player;

        private void Awake()
        {
            _player = GetComponentInParent<Player>();
        }

        private void FixedUpdate()
        {
            //Vertical movement
            float headOffset;

            if (_player.PhysicsRig.JumpState != PhysicsRig.JumpStates.Ascending && _player.PhysicsRig.JumpState != PhysicsRig.JumpStates.Descending)
                headOffset = (_player.PhysicsRig.RealFenderPelvisOffset - 0.55f) / 0.55f * (1.65f - 1.025f);
            else
                headOffset = 0f;

            _player.PhysicsRig.PelvisHeadJoint.targetPosition = new Vector3(0f, _player.PhysicsRig.PelvisRigidbody.transform.position.y - _player.ControllerRig.CameraTransform.position.y, 0f);
            _player.PhysicsRig.PelvisHeadColliderJoint.targetPosition = new Vector3(0f, _player.PhysicsRig.PelvisRigidbody.transform.position.y - _player.ControllerRig.CameraTransform.position.y, 0f);

            //Lateral movement
            Vector3 deltaCameraOffset = _player.ControllerRig.CameraTransform.position - _player.ControllerRig.transform.position;
            deltaCameraOffset.y = 0f;

            MoveRigidbody(_player.PhysicsRig.FenderRigidbody, deltaCameraOffset * _player.ControllerRig.transform.localScale.x);
            MoveRigidbody(_player.PhysicsRig.PelvisRigidbody, deltaCameraOffset * _player.ControllerRig.transform.localScale.x);
            MoveRigidbody(_player.PhysicsRig.LocomotionSphereRigidbody, deltaCameraOffset * _player.ControllerRig.transform.localScale.x);
            MoveRigidbody(_player.PhysicsRig.HeadRigidbody, deltaCameraOffset * _player.ControllerRig.transform.localScale.x);
            if (_player.PhysicsRig.LocomotionSphere.IsGrounded)
            {
                MoveRigidbody(_player.PhysicsRig.LeftHandRigidbody, deltaCameraOffset * _player.ControllerRig.transform.localScale.x);
                MoveRigidbody(_player.PhysicsRig.RightHandRigidbody, deltaCameraOffset * _player.ControllerRig.transform.localScale.x);
            }

            Vector3 newCameraOffset = _player.ControllerRig.CameraOffsetTransform.localPosition;
            newCameraOffset += _player.ControllerRig.transform.InverseTransformDirection(-deltaCameraOffset);
            newCameraOffset.y = headOffset;
            _player.ControllerRig.CameraOffsetTransform.localPosition = newCameraOffset;
        }

        private void MoveRigidbody(Rigidbody rigidbody, Vector3 translation)
        {
            rigidbody.MovePosition(rigidbody.position + translation);
        }
    }
}
