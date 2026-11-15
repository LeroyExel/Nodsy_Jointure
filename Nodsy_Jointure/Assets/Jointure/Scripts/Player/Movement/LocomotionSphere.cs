using UnityEngine;

namespace Jointure
{
    public class LocomotionSphere : MonoBehaviour
    {
        public bool IsGrounded;
        public Component GroundBody;

        [SerializeField]
        float _maxSlopeAngle = 50f;

        [SerializeField]
        private PhysicsMaterial _highFriction, _lowFriction;

        private Rigidbody _locomotionSphereRigidbody;
        private SphereCollider _locomotionSphereCollider;

        private void Start()
        {
            _locomotionSphereRigidbody = GetComponent<Rigidbody>();
            _locomotionSphereCollider = GetComponent<SphereCollider>();
        }

        private void OnCollisionStay(Collision collision)
        {
            ProcessContactPoints(collision);
        }

        private void OnCollisionExit()
        {
            IsGrounded = false;
            _locomotionSphereCollider.material = _lowFriction;
        }

        private void ProcessContactPoints(Collision collision)
        {
            _locomotionSphereCollider.material = _lowFriction;
            ContactPoint[] contactPoints = new ContactPoint[collision.contactCount];
            collision.GetContacts(contactPoints);
            foreach (ContactPoint contactPoint in contactPoints)
            {
                if (_maxSlopeAngle + 1f < Vector3.Angle(contactPoint.normal, -Physics.gravity.normalized))
                    continue;

                //Gather data
                Vector3 groundNormal = contactPoint.normal;
                IsGrounded = true;
                _locomotionSphereCollider.material = _highFriction;
                GroundBody = (collision.body as Rigidbody);
                if (!GroundBody)
                    GroundBody = (collision.body as ArticulationBody);
                Vector3 reactionForce = contactPoint.impulse / Time.fixedDeltaTime;

                //Apply anti-roll force
                Vector3 alongPlaneVector = Vector3.Cross(groundNormal, -Physics.gravity.normalized);
                Vector3 upPlaneVector = Vector3.Cross(alongPlaneVector, groundNormal);

                float cosIncline = Vector3.Dot(groundNormal, -Physics.gravity.normalized);
                float characterWeight = 0f;

                if (cosIncline != 0)
                    characterWeight = reactionForce.magnitude / cosIncline;

                if (collision.collider.material.dynamicFriction < 0.1f)
                    characterWeight = 0f;

                _locomotionSphereRigidbody.AddForce(upPlaneVector * characterWeight, ForceMode.Force);
                contactPoint.otherCollider.attachedRigidbody?.AddForceAtPosition(-upPlaneVector * characterWeight, contactPoint.point, ForceMode.Force);
            }
        }
    }
}