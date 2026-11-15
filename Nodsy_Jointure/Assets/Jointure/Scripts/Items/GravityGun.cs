using UnityEngine;

namespace Jointure.Samples
{
    public class GravityGun : MonoBehaviour
    {
        [Header("Bodies & Transforms")]
        [SerializeField] private ArticulationBody _gunBody;
        [SerializeField] private Transform _muzzleTransform;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private LineRenderer _beamRenderer;

        [Header("Tractor Beam Settings")]
        [SerializeField] private float _grabRange = 12f;
        [SerializeField] private float _holdDistance = 2.5f;
        [SerializeField] private float _positionSpring = 45f;
        [SerializeField] private float _positionDamper = 12f;
        [SerializeField] private float _rotationSpring = 55f;
        [SerializeField] private float _rotationDamper = 15f;
        [SerializeField] private float _maxLinearSpeed = 10f;
        [SerializeField] private float _maxAngularSpeed = 15f;
        [SerializeField] private LayerMask _interactionMask = ~0;

        [Header("Impulse Settings")]
        [SerializeField] private float _launchForce = 35f;
        [SerializeField] private float _recoilMultiplier = 6f;

        private Rigidbody _heldRigidbody;
        private ArticulationBody _heldArticulationBody;
        private Vector3 _localHitOffset;
        private Quaternion _targetRelativeRotation;
        private bool _isHolding;

        private void Awake()
        {
            if (!_gunBody)
                _gunBody = GetComponent<ArticulationBody>();

            if (!_muzzleTransform)
                _muzzleTransform = transform;
        }

        private void OnDisable()
        {
            DropObject();
        }

        private void Update()
        {
            UpdateBeam();
        }

        private void FixedUpdate()
        {
            if (!_isHolding)
                return;

            Transform muzzle = _muzzleTransform != null ? _muzzleTransform : transform;
            Vector3 targetAnchorPosition = muzzle.position + (muzzle.forward * _holdDistance);
            Quaternion targetRotation = muzzle.rotation * _targetRelativeRotation;

            if (_heldRigidbody != null)
            {
                DriveRigidbody(targetAnchorPosition, targetRotation, muzzle);
            }
            else if (_heldArticulationBody != null)
            {
                DriveArticulationBody(targetAnchorPosition, targetRotation, muzzle);
            }
            else
            {
                DropObject();
            }
        }

        public void OnTriggerDown()
        {
            if (!_isHolding)
            {
                TryGrabTarget();
            }
        }

        public void OnTriggerUp()
        {
            if (_isHolding)
            {
                DropObject();
            }
        }

        public void OnSecondaryDown()
        {
            if (_isHolding)
            {
                LaunchHeldObject();
            }
        }

        public void OnRelease()
        {
            DropObject();
        }

        private void TryGrabTarget()
        {
            Transform muzzle = _muzzleTransform != null ? _muzzleTransform : transform;
            Vector3 origin = muzzle.position;
            Vector3 direction = muzzle.forward;

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, _grabRange, _interactionMask, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
                return;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                Transform hitTransform = hit.transform;

                if (hitTransform.IsChildOf(transform.root))
                    continue;

                Rigidbody rb = hit.rigidbody != null ? hit.rigidbody : hitTransform.GetComponentInParent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    _heldRigidbody = rb;
                    _heldRigidbody.useGravity = false;
                    _heldRigidbody.linearVelocity = Vector3.zero;
                    _heldRigidbody.angularVelocity = Vector3.zero;
                    _heldRigidbody.maxAngularVelocity = _maxAngularSpeed;
                    _localHitOffset = _heldRigidbody.transform.InverseTransformPoint(hit.point);
                    _targetRelativeRotation = Quaternion.Inverse(muzzle.rotation) * _heldRigidbody.rotation;
                    _isHolding = true;
                    return;
                }

                ArticulationBody ab = hit.articulationBody != null ? hit.articulationBody : hitTransform.GetComponentInParent<ArticulationBody>();
                if (ab != null && !ab.isRoot)
                {
                    _heldArticulationBody = ab;
                    _heldArticulationBody.useGravity = false;
                    _heldArticulationBody.linearVelocity = Vector3.zero;
                    _heldArticulationBody.angularVelocity = Vector3.zero;
                    _heldArticulationBody.maxAngularVelocity = _maxAngularSpeed;
                    _localHitOffset = _heldArticulationBody.transform.InverseTransformPoint(hit.point);
                    _targetRelativeRotation = Quaternion.Inverse(muzzle.rotation) * _heldArticulationBody.transform.rotation;
                    _isHolding = true;
                    return;
                }
            }
        }

        private void DriveRigidbody(Vector3 targetAnchorPosition, Quaternion targetRotation, Transform muzzle)
        {
            Vector3 currentHitPoint = _heldRigidbody.transform.TransformPoint(_localHitOffset);
            Vector3 positionDelta = targetAnchorPosition - currentHitPoint;
            Vector3 targetLinearVel = positionDelta * _positionSpring;
            Vector3 pointVelocity = _heldRigidbody.GetPointVelocity(currentHitPoint);
            Vector3 linearAccel = (targetLinearVel - pointVelocity) * _positionDamper;

            _heldRigidbody.AddForceAtPosition(linearAccel, currentHitPoint, ForceMode.Acceleration);

            if (_heldRigidbody.linearVelocity.magnitude > _maxLinearSpeed)
            {
                _heldRigidbody.linearVelocity = _heldRigidbody.linearVelocity.normalized * _maxLinearSpeed;
            }

            Quaternion deltaRot = targetRotation * Quaternion.Inverse(_heldRigidbody.rotation);
            deltaRot.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

            if (angleInDegrees > 180f)
                angleInDegrees -= 360f;

            if (Mathf.Abs(angleInDegrees) > 0.01f && !float.IsNaN(rotationAxis.x))
            {
                Vector3 angularError = rotationAxis.normalized * (angleInDegrees * Mathf.Deg2Rad);
                Vector3 targetAngularVel = angularError * _rotationSpring;
                Vector3 angularAccel = (targetAngularVel - _heldRigidbody.angularVelocity) * _rotationDamper;

                _heldRigidbody.AddTorque(angularAccel, ForceMode.Acceleration);
            }
            else
            {
                _heldRigidbody.AddTorque(-_heldRigidbody.angularVelocity * _rotationDamper, ForceMode.Acceleration);
            }

            if (_heldRigidbody.angularVelocity.magnitude > _maxAngularSpeed)
            {
                _heldRigidbody.angularVelocity = _heldRigidbody.angularVelocity.normalized * _maxAngularSpeed;
            }

            if (Vector3.Distance(currentHitPoint, muzzle.position) > _grabRange * 1.5f)
            {
                DropObject();
            }
        }

        private void DriveArticulationBody(Vector3 targetAnchorPosition, Quaternion targetRotation, Transform muzzle)
        {
            Vector3 currentHitPoint = _heldArticulationBody.transform.TransformPoint(_localHitOffset);
            Vector3 positionDelta = targetAnchorPosition - currentHitPoint;
            Vector3 targetLinearVel = positionDelta * _positionSpring;
            Vector3 linearAccel = (targetLinearVel - _heldArticulationBody.linearVelocity) * _positionDamper;

            _heldArticulationBody.AddForceAtPosition(linearAccel, currentHitPoint, ForceMode.Acceleration);

            if (_heldArticulationBody.linearVelocity.magnitude > _maxLinearSpeed)
            {
                _heldArticulationBody.linearVelocity = _heldArticulationBody.linearVelocity.normalized * _maxLinearSpeed;
            }

            Quaternion deltaRot = targetRotation * Quaternion.Inverse(_heldArticulationBody.transform.rotation);
            deltaRot.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

            if (angleInDegrees > 180f)
                angleInDegrees -= 360f;

            if (Mathf.Abs(angleInDegrees) > 0.01f && !float.IsNaN(rotationAxis.x))
            {
                Vector3 angularError = rotationAxis.normalized * (angleInDegrees * Mathf.Deg2Rad);
                Vector3 targetAngularVel = angularError * _rotationSpring;
                Vector3 angularAccel = (targetAngularVel - _heldArticulationBody.angularVelocity) * _rotationDamper;

                _heldArticulationBody.AddTorque(angularAccel, ForceMode.Acceleration);
            }
            else
            {
                _heldArticulationBody.AddTorque(-_heldArticulationBody.angularVelocity * _rotationDamper, ForceMode.Acceleration);
            }

            if (_heldArticulationBody.angularVelocity.magnitude > _maxAngularSpeed)
            {
                _heldArticulationBody.angularVelocity = _heldArticulationBody.angularVelocity.normalized * _maxAngularSpeed;
            }

            if (Vector3.Distance(currentHitPoint, muzzle.position) > _grabRange * 1.5f)
            {
                DropObject();
            }
        }

        public void DropObject()
        {
            if (!_isHolding && _heldRigidbody == null && _heldArticulationBody == null)
                return;

            if (_heldRigidbody != null)
            {
                _heldRigidbody.useGravity = true;
                _heldRigidbody.linearVelocity = Vector3.ClampMagnitude(_heldRigidbody.linearVelocity, 2f);
                _heldRigidbody.angularVelocity = Vector3.ClampMagnitude(_heldRigidbody.angularVelocity, 2f);
                _heldRigidbody = null;
            }

            if (_heldArticulationBody != null)
            {
                _heldArticulationBody.useGravity = true;
                _heldArticulationBody.linearVelocity = Vector3.ClampMagnitude(_heldArticulationBody.linearVelocity, 2f);
                _heldArticulationBody.angularVelocity = Vector3.ClampMagnitude(_heldArticulationBody.angularVelocity, 2f);
                _heldArticulationBody = null;
            }

            _isHolding = false;
        }

        private void LaunchHeldObject()
        {
            Transform muzzle = _muzzleTransform != null ? _muzzleTransform : transform;
            Vector3 launchVelocity = muzzle.forward * _launchForce;

            if (_heldRigidbody != null)
            {
                _heldRigidbody.useGravity = true;
                _heldRigidbody.linearVelocity = launchVelocity;
                _heldRigidbody.angularVelocity = Vector3.zero;
                _heldRigidbody = null;
            }

            if (_heldArticulationBody != null)
            {
                _heldArticulationBody.useGravity = true;
                _heldArticulationBody.linearVelocity = launchVelocity;
                _heldArticulationBody.angularVelocity = Vector3.zero;
                _heldArticulationBody = null;
            }

            _isHolding = false;
            ApplyRecoil(muzzle);
        }

        private void ApplyRecoil(Transform muzzle)
        {
            if (_gunBody != null)
            {
                Vector3 recoilForce = -muzzle.forward * _recoilMultiplier;
                _gunBody.AddForceAtPosition(recoilForce, muzzle.position, ForceMode.Impulse);
            }
        }

        private void UpdateBeam()
        {
            if (_beamRenderer == null)
                return;

            Transform muzzle = _muzzleTransform != null ? _muzzleTransform : transform;

            if (_isHolding)
            {
                _beamRenderer.enabled = true;
                _beamRenderer.SetPosition(0, muzzle.position);

                Vector3 targetPos = _heldRigidbody != null
                    ? _heldRigidbody.transform.TransformPoint(_localHitOffset)
                    : _heldArticulationBody.transform.TransformPoint(_localHitOffset);

                _beamRenderer.SetPosition(1, targetPos);
            }
            else
            {
                _beamRenderer.enabled = false;
            }
        }

        private void OnDrawGizmos()
        {
            Transform muzzle = _muzzleTransform != null ? _muzzleTransform : transform;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(muzzle.position, muzzle.forward * _grabRange);
        }
    }
}