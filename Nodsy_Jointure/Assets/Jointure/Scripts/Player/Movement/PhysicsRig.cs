using UnityEngine;

namespace Jointure
{
    public class PhysicsRig : MonoBehaviour
    {
        public LocomotionSphere LocomotionSphere;

        public Rigidbody
            LocomotionSphereRigidbody,
            FenderRigidbody,
            PelvisRigidbody,
            HeadRigidbody,
            LeftHandRigidbody,
            RightHandRigidbody;

        public ConfigurableJoint
            FenderPelvisJoint,
            PelvisHeadJoint,
            PelvisHeadColliderJoint,
            LeftHandJoint,
            RightHandJoint;

        public float
            AirAcceleration = 3f,
            FenderPelvisOffset = 0.55f,
            RealFenderPelvisOffset = 0.55f; //The multiplier for air acceleration

        public enum JumpStates
        {
            NotJumping,
            Anticipation,
            PushingGround,
            Ascending,
            Descending
        }
        public JumpStates JumpState; //What state of a jump the player is in

        private void Awake()
        {
            int playerLayer = LayerMask.NameToLayer("JointureRig");
            Physics.IgnoreLayerCollision(playerLayer, playerLayer);

            ApplyLayerToHierarchy(gameObject, LayerMask.NameToLayer("JointureRig"));
        }

        private void ApplyLayerToHierarchy(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.layer = layer;

                Transform hasChildren = child.GetComponentInChildren<Transform>();
                if (!hasChildren)
                    continue;

                ApplyLayerToHierarchy(child.gameObject, layer);
            }
        }

        private void SetLayerRecursive(GameObject gameObject, int layer) => ApplyLayerToHierarchy(gameObject, layer);
    }
}