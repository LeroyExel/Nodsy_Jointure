using UnityEngine;

namespace Jointure
{
    public class Hips : MonoBehaviour
    {
        private Player _player;

        [SerializeField]
        private Transform _hipStandTransform, _hipCrouchTransform;

        private void Awake()
        {
            _player = GetComponentInParent<Player>();
        }

        void Update()
        {
            float standingPercent = (_player.ControllerRig.CameraTransform.position.y - _player.PhysicsRig.LocomotionSphereRigidbody.position.y + 0.2f) / 1.65f;
            transform.position = Vector3.Lerp(_hipCrouchTransform.position, _hipStandTransform.position, standingPercent);
        }
    }
}