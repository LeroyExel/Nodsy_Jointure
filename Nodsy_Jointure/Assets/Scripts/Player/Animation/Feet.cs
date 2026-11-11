using System.Collections;
using UnityEngine;

namespace Jointure
{
    public class Feet : MonoBehaviour
    {
        private Player _player;

        [SerializeField]
        private AudioClip[] _walkSounds, _runSounds;

        private Foot _currentFoot, _leftFoot, _rightFoot;

        private Rigidbody _pelvisRigidbody;
        private Vector3 _pelvisVelocity, _groundVelocity;
        private bool _isMoving, _isStepping;
        private float _stepTime = 0.1f, _stepLength = 0.1f, _stepHeight = 0.1f;

        private LayerMask _mask;

        [Header("Foot Rotation Offsets")]
        public Vector3 LeftFootRotationOffset = Vector3.zero;
        public Vector3 RightFootRotationOffset = Vector3.zero;

        private Transform _leftLegTarget, _rightLegTarget;
        private Quaternion _baseLeftTargetRot = Quaternion.identity, _baseRightTargetRot = Quaternion.identity;

        private void Awake()
        {
            _player = GetComponentInParent<Player>();
        }

        private void Start()
        {
            _leftFoot = new Foot(_player.AnimationRig.LeftFootAnchorTransform, _player.AnimationRig.LeftFootTargetTransform, -0.08f);
            _rightFoot = new Foot(_player.AnimationRig.RightFootAnchorTransform, _player.AnimationRig.RightFootTargetTransform, 0.08f);
            _currentFoot = _rightFoot;
            _pelvisRigidbody = _player.PhysicsRig.PelvisRigidbody;
            _mask = ~LayerMask.GetMask("JointureRig");

            if (_player.AnimationRig.LeftFootAnchorTransform != null)
            {
                _leftLegTarget = _player.AnimationRig.LeftFootAnchorTransform.Find("LeftLegTarget");
                if (_leftLegTarget != null) _baseLeftTargetRot = _leftLegTarget.localRotation;
            }
            if (_player.AnimationRig.RightFootAnchorTransform != null)
            {
                _rightLegTarget = _player.AnimationRig.RightFootAnchorTransform.Find("RightLegTarget");
                if (_rightLegTarget != null) _baseRightTargetRot = _rightLegTarget.localRotation;
            }
        }

        private void Update()
        {
            if (_leftLegTarget != null)
                _leftLegTarget.localRotation = _baseLeftTargetRot * Quaternion.Euler(LeftFootRotationOffset);
            if (_rightLegTarget != null)
                _rightLegTarget.localRotation = _baseRightTargetRot * Quaternion.Euler(RightFootRotationOffset);

            Component groundBody = _player.PhysicsRig.LocomotionSphere.GroundBody;
            _groundVelocity = Vector3.zero;

            if (groundBody as Rigidbody)
                _groundVelocity = (groundBody as Rigidbody).linearVelocity;

            if (groundBody as ArticulationBody)
                _groundVelocity = (groundBody as ArticulationBody).linearVelocity;

            _pelvisVelocity = Vector3.ProjectOnPlane(_pelvisRigidbody.linearVelocity - _groundVelocity, Vector3.up);
            _leftFoot.Transform.position += _groundVelocity * Time.deltaTime;
            _rightFoot.Transform.position += _groundVelocity * Time.deltaTime;
            UpdateTarget(_leftFoot);
            UpdateTarget(_rightFoot);
            if (!_player.PhysicsRig.LocomotionSphere.IsGrounded) //Take air pose if off ground
            {
                SnapFootToTarget(_leftFoot);
                SnapFootToTarget(_rightFoot);
            }
            else
            {
                if ((_currentFoot.Transform.position - _currentFoot.Target.position).magnitude > _stepLength) //Step if foot far enough from target
                {
                    StartCoroutine(Step());
                }
                if (_pelvisVelocity.magnitude < 0.1f)
                {
                    if (_isMoving) //Readjust the feet if only just stopped moving
                    {
                        StartCoroutine(Step(true));
                        StartCoroutine(Step(true));
                    }
                    _isMoving = false;
                }
                else
                {
                    _isMoving = true;
                }
            }
        }

        private void UpdateTarget(Foot foot)
        {
            foot.Target.position = _player.AnimationRig.HipsTransform.position + _pelvisVelocity * _stepTime + _player.AnimationRig.CharacterTransform.right * foot.Offset;
            RaycastHit hit;
            if (Physics.Raycast(foot.Target.position, Vector3.down, out hit, 1.25f, _mask, QueryTriggerInteraction.Ignore) && _player.PhysicsRig.LocomotionSphere.IsGrounded)
            {
                foot.IsGrounded = true;
                foot.Target.position = hit.point;
                foot.Target.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(_player.AnimationRig.CharacterTransform.forward, hit.normal), hit.normal);
            }
            else
            {
                Vector3 target = Vector3.ProjectOnPlane(_player.AnimationRig.HipsTransform.position, Vector3.up);
                target += Vector3.up * (_player.PhysicsRig.LocomotionSphereRigidbody.transform.position.y - 0.2f);
                target += _pelvisVelocity * _stepTime + _player.AnimationRig.CharacterTransform.right * foot.Offset;
                foot.Target.position = target;
                foot.Target.rotation = Quaternion.LookRotation(_player.AnimationRig.CharacterTransform.forward);
            }
        }

        private IEnumerator Step(bool isMandatory = false)
        {
            if (!isMandatory && _isStepping || !_currentFoot.IsGrounded)
            {
                yield break;
            }
            while (_isStepping)
            {
                yield return null;
            }
            _isStepping = true;

            Quaternion startRot = _currentFoot.Transform.rotation;
            Vector3 startPoint = _currentFoot.Transform.position;

            float timeElapsed = 0;
            do
            {
                if (!isMandatory)
                {
                    _stepTime = Mathf.Clamp(-2 / 30f * _pelvisVelocity.magnitude + 0.3f, 0.1f, 0.2f);
                    _stepHeight = Mathf.Clamp(1 / 6f * _pelvisVelocity.magnitude, 0f, 0.5f);
                }
                else
                {
                    _stepTime = 0.1f;
                    _stepHeight = 0f;
                }

                startPoint += _groundVelocity * Time.deltaTime;

                Quaternion endRot = _currentFoot.Target.rotation;
                Vector3 endPoint = _currentFoot.Target.position;
                Vector3 centerPoint = (startPoint + endPoint) / 2;
                centerPoint += Vector3.up * _stepHeight;

                timeElapsed += Time.deltaTime;
                float normalizedTime = timeElapsed / (_stepTime * 2);
                _currentFoot.Transform.position = Vector3.Lerp(
                    Vector3.Lerp(startPoint, centerPoint, normalizedTime),
                    Vector3.Lerp(centerPoint, endPoint, normalizedTime),
                    normalizedTime);
                _currentFoot.Transform.rotation = Quaternion.Slerp(startRot, endRot, normalizedTime);
                yield return null;
            } while (timeElapsed < _stepTime * 2);
            if (!isMandatory)
            {
                PlayFootstepSound(); //Play the footstep sound
            }
            if (_currentFoot == _leftFoot)
            {
                _currentFoot = _rightFoot;
            }
            else
            {
                _currentFoot = _leftFoot;
            }
            _isStepping = false;
        }

        private void SnapFootToTarget(Foot foot)
        {
            foot.Transform.position = foot.Target.position;
            foot.Transform.rotation = foot.Target.rotation;
        }

        public void TeleportFeet()
        {
            UpdateTarget(_leftFoot);
            UpdateTarget(_rightFoot);
            SnapFootToTarget(_leftFoot);
            SnapFootToTarget(_rightFoot);
        }

        private void PlayFootstepSound()
        {
            AudioClip[] audioClips;
            if (_pelvisVelocity.magnitude < 2) //If player is running
            {
                audioClips = _walkSounds;
            }
            else
            {
                audioClips = _runSounds;
            }

            _currentFoot.Transform.GetComponent<AudioSource>().PlayOneShot(Utilities.RandomAudioClip(audioClips));
        }
    }

    class Foot
    {
        public Transform Transform;
        public Transform Target;
        public float Offset;
        public bool IsGrounded;

        public Foot(Transform transform, Transform target, float offset, bool isGrounded = false)
        {
            Transform = transform;
            Target = target;
            Offset = offset;
            IsGrounded = isGrounded;
        }
    }
}