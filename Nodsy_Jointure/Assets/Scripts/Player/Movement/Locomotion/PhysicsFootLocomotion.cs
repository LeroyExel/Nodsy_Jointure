using UnityEngine;
using RootMotion.Dynamics;

namespace Jointure
{
    /// <summary>
    /// Physics-based locomotion controller operating strictly via physical contact between
    /// feet colliders and ground surfaces using push-off forces and friction modulation.
    /// Strictly forbids setting kinematic velocities or translating transforms directly.
    /// </summary>
    [AddComponentMenu("Jointure/Movement/Physics Foot Locomotion")]
    public class PhysicsFootLocomotion : MonoBehaviour
    {
        [Header("Rig References")]
        [Tooltip("The PuppetMaster managing the physical ragdoll.")]
        public PuppetMaster PuppetMaster;

        [Tooltip("The player input reader providing MoveVector and TurnInput.")]
        public InputReader InputReader;

        [Tooltip("Head / HMD transform for movement direction reference.")]
        public Transform HeadTransform;

        [Tooltip("Movement direction reference transform (typically Left Controller). When HandOrientedLocomotion is true, movement follows where the hand points.")]
        public Transform MoveHandTransform;

        [Tooltip("If true, movement forward direction follows where the hand points. If false, follows head direction.")]
        public bool HandOrientedLocomotion = true;

        [Tooltip("Left foot Rigidbody.")]
        public Rigidbody LeftFootRigidbody;

        [Tooltip("Right foot Rigidbody.")]
        public Rigidbody RightFootRigidbody;

        [Tooltip("Pelvis / Hips Rigidbody.")]
        public Rigidbody PelvisRigidbody;

        [Header("Locomotion Tuning")]
        [Tooltip("Maximum directional push-off force applied when grounded.")]
        public float PushForce = 850f;

        [Tooltip("Braking / counter-drift friction applied to grounded feet when stopping.")]
        public float BrakingFriction = 350f;

        [Tooltip("Maximum horizontal speed before push-off force scales down.")]
        public float MaxSpeed = 4.5f;

        [Tooltip("Torque applied to rotate the hips/pelvis for turning.")]
        public float TurnTorque = 220f;

        [Tooltip("Restorative spring torque aligning hips yaw to head/target direction.")]
        public float YawAlignSpring = 160f;

        [Tooltip("Angular damping to prevent yaw oscillation when turning.")]
        public float YawAlignDamper = 20f;

        [Header("Ground Detection")]
        [Tooltip("Layer mask for walkable surfaces.")]
        public LayerMask GroundLayers = ~0;

        [Tooltip("Downward raycast distance from foot center.")]
        public float GroundRayDistance = 0.35f;

        [Tooltip("Spherecast radius for foot ground detection.")]
        public float FootRadius = 0.08f;

        [Header("Gait & Step Transitions")]
        [Tooltip("Cadence / duration for each step transition in seconds.")]
        public float StepDuration = 0.35f;

        [Tooltip("Vertical lift force applied to the swing foot during transition.")]
        public float StepLiftForce = 120f;

        [Tooltip("Forward swing force applied to the swing foot during transition.")]
        public float StepSwingForce = 75f;

        [Header("Balance & Slope Recovery")]
        [Tooltip("Upright restorative torque spring to maintain posture on slopes.")]
        public float BalanceTorqueSpring = 300f;

        [Tooltip("Angular damping to prevent oscillation when uprighting.")]
        public float BalanceTorqueDamper = 35f;

        [Tooltip("Maximum slope angle in degrees where traction is fully maintained.")]
        public float MaxWalkableSlopeAngle = 45f;

        // Public queries
        public bool IsLeftGrounded { get; private set; }
        public bool IsRightGrounded { get; private set; }
        public bool IsGrounded => IsLeftGrounded || IsRightGrounded;

        public RaycastHit LeftGroundHit => _leftHit;
        public RaycastHit RightGroundHit => _rightHit;

        private RaycastHit _leftHit;
        private RaycastHit _rightHit;

        private float _stepTimer;
        private bool _isLeftStance = true; // Alternating gait cycle

        private void Reset()
        {
            FindReferences();
        }

        private void Awake()
        {
            FindReferences();
        }

        public void FindReferences()
        {
            if (PuppetMaster == null) PuppetMaster = GetComponentInParent<PuppetMaster>() ?? GetComponentInChildren<PuppetMaster>();
            if (InputReader == null) InputReader = GetComponentInParent<InputReader>() ?? GetComponentInChildren<InputReader>();

            Player player = GetComponentInParent<Player>() ?? GetComponentInChildren<Player>() ?? UnityEngine.Object.FindAnyObjectByType<Player>();
            if (player != null)
            {
                if (InputReader == null) InputReader = player.InputReader;
                if (PuppetMaster == null) PuppetMaster = player.PuppetMaster;
                if (HeadTransform == null && player.ControllerRig != null)
                    HeadTransform = player.ControllerRig.CameraTransform;
                if (MoveHandTransform == null && player.ControllerRig != null)
                    MoveHandTransform = player.ControllerRig.LeftControllerTransform;
            }

            if (PuppetMaster != null && PuppetMaster.muscles != null)
            {
                foreach (var muscle in PuppetMaster.muscles)
                {
                    if (muscle == null || muscle.joint == null) continue;
                    string n = muscle.name.ToLower();
                    if (muscle.props.group == Muscle.Group.Hips || n.Contains("pelvis") || n.Contains("hip"))
                    {
                        if (PelvisRigidbody == null) PelvisRigidbody = muscle.joint.GetComponent<Rigidbody>();
                    }
                    else if (muscle.props.group == Muscle.Group.Foot || n.Contains("foot"))
                    {
                        if (n.Contains("left") || n.Contains("l ") || n.Contains("l_") || n.Contains("_l") || n.Contains("lfoot"))
                        {
                            if (LeftFootRigidbody == null) LeftFootRigidbody = muscle.joint.GetComponent<Rigidbody>();
                        }
                        else if (n.Contains("right") || n.Contains("r ") || n.Contains("r_") || n.Contains("_r") || n.Contains("rfoot"))
                        {
                            if (RightFootRigidbody == null) RightFootRigidbody = muscle.joint.GetComponent<Rigidbody>();
                        }
                    }
                }
            }

            // Exclude ragdoll & target layers from ground detection
            int puppetLayer = LayerMask.NameToLayer("PuppetRig");
            int targetLayer = LayerMask.NameToLayer("TargetRig");
            int jointureLayer = LayerMask.NameToLayer("JointureRig");
            int ignoreMask = 0;
            if (puppetLayer >= 0) ignoreMask |= (1 << puppetLayer);
            if (targetLayer >= 0) ignoreMask |= (1 << targetLayer);
            if (jointureLayer >= 0) ignoreMask |= (1 << jointureLayer);
            GroundLayers &= ~ignoreMask;
        }

        private void FixedUpdate()
        {
            if (PuppetMaster == null || PelvisRigidbody == null || LeftFootRigidbody == null || RightFootRigidbody == null)
            {
                FindReferences();
                if (PelvisRigidbody == null || LeftFootRigidbody == null || RightFootRigidbody == null) return;
            }

            // 1. Detect ground contact and surface normals
            UpdateGroundDetection();

            // 2. Process physical foot locomotion and gait transitions
            UpdateFootPushOff();

            // 3. Process turning torque
            UpdateTurning();

            // 4. Balance recovery on slopes or uneven terrain
            UpdateBalanceRecovery();
        }

        private void UpdateGroundDetection()
        {
            IsLeftGrounded = CheckFootGrounded(LeftFootRigidbody, out _leftHit);
            IsRightGrounded = CheckFootGrounded(RightFootRigidbody, out _rightHit);
        }

        private bool CheckFootGrounded(Rigidbody footRb, out RaycastHit hit)
        {
            if (footRb == null)
            {
                hit = default;
                return false;
            }

            float rayStartOffset = 0.15f;
            Vector3 origin = footRb.worldCenterOfMass + Vector3.up * rayStartOffset;
            float maxDist = rayStartOffset + GroundRayDistance;

            // 1. Elevated SphereCast to avoid start-surface overlap
            if (Physics.SphereCast(origin, FootRadius, Vector3.down, out hit, maxDist, GroundLayers, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            // 2. Direct downward Raycast fallback
            if (Physics.Raycast(origin, Vector3.down, out hit, maxDist, GroundLayers, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            hit = default;
            return false;
        }

        private void UpdateFootPushOff()
        {
            Vector2 input = InputReader != null ? InputReader.MoveVector : Vector2.zero;

            // Resolve forward direction based on hand orientation (or head fallback)
            Vector3 forward;
            if (HandOrientedLocomotion && MoveHandTransform != null)
            {
                Vector3 handFwd = MoveHandTransform.forward;
                Vector3 horizontalHandFwd = new Vector3(handFwd.x, 0f, handFwd.z);
                // Only fall back to head if hand is pointed almost straight down into the floor or straight up into ceiling
                if (horizontalHandFwd.sqrMagnitude < 0.04f && HeadTransform != null)
                {
                    Vector3 headFwd = HeadTransform.forward;
                    forward = new Vector3(headFwd.x, 0f, headFwd.z).normalized;
                }
                else
                {
                    forward = horizontalHandFwd.normalized;
                }
            }
            else if (HeadTransform != null)
            {
                Vector3 headFwd = HeadTransform.forward;
                forward = new Vector3(headFwd.x, 0f, headFwd.z).normalized;
            }
            else
            {
                Vector3 tFwd = PelvisRigidbody != null ? PelvisRigidbody.transform.forward : transform.forward;
                forward = new Vector3(tFwd.x, 0f, tFwd.z).normalized;
            }

            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3 desiredWorldDir = (forward * input.y + right * input.x);
            float inputMag = Mathf.Clamp01(desiredWorldDir.magnitude);
            if (inputMag > 0.001f) desiredWorldDir /= inputMag;

            // When idle or no input, apply solid braking to stop all movement immediately
            if (inputMag <= 0.05f)
            {
                _stepTimer = 0f;

                Vector3 pelvisAngularVel = Vector3.up * PelvisRigidbody.angularVelocity.y;

                if (LeftFootRigidbody != null && IsLeftGrounded)
                {
                    Vector3 leftTangent = Vector3.Cross(pelvisAngularVel, LeftFootRigidbody.position - PelvisRigidbody.position);
                    Vector3 leftDrift = Vector3.ProjectOnPlane(LeftFootRigidbody.linearVelocity - leftTangent, Vector3.up);
                    LeftFootRigidbody.AddForce(-leftDrift * BrakingFriction, ForceMode.Force);
                }

                if (RightFootRigidbody != null && IsRightGrounded)
                {
                    Vector3 rightTangent = Vector3.Cross(pelvisAngularVel, RightFootRigidbody.position - PelvisRigidbody.position);
                    Vector3 rightDrift = Vector3.ProjectOnPlane(RightFootRigidbody.linearVelocity - rightTangent, Vector3.up);
                    RightFootRigidbody.AddForce(-rightDrift * BrakingFriction, ForceMode.Force);
                }

                if (PelvisRigidbody != null)
                {
                    Vector3 pelvisHorizVel = Vector3.ProjectOnPlane(PelvisRigidbody.linearVelocity, Vector3.up);
                    PelvisRigidbody.AddForce(-pelvisHorizVel * (BrakingFriction * 1.5f), ForceMode.Force);

                    // Stop residual creep
                    if (pelvisHorizVel.sqrMagnitude < 0.005f)
                    {
                        PelvisRigidbody.linearVelocity = new Vector3(0f, PelvisRigidbody.linearVelocity.y, 0f);
                    }
                }

                return;
            }

            if (!IsGrounded) return;

            // Alternating gait step transitions
            _stepTimer += Time.fixedDeltaTime;
            if (_stepTimer >= StepDuration)
            {
                _stepTimer = 0f;
                _isLeftStance = !_isLeftStance;
            }

            // Determine stance and swing foot
            Rigidbody stanceFoot = _isLeftStance ? LeftFootRigidbody : RightFootRigidbody;
            Rigidbody swingFoot = _isLeftStance ? RightFootRigidbody : LeftFootRigidbody;
            bool stanceGrounded = _isLeftStance ? IsLeftGrounded : IsRightGrounded;
            bool swingGrounded = _isLeftStance ? IsRightGrounded : IsLeftGrounded;
            RaycastHit stanceHit = _isLeftStance ? _leftHit : _rightHit;

            // Fallback: If designated stance foot is not grounded but the other is, switch stance
            if (!stanceGrounded && swingGrounded)
            {
                _isLeftStance = !_isLeftStance;
                stanceFoot = _isLeftStance ? LeftFootRigidbody : RightFootRigidbody;
                swingFoot = _isLeftStance ? RightFootRigidbody : LeftFootRigidbody;
                stanceGrounded = true;
                stanceHit = _isLeftStance ? _leftHit : _rightHit;
            }

            if (!stanceGrounded) return;

            // Slope and traction modulation
            Vector3 groundNormal = stanceHit.normal.sqrMagnitude > 0.01f ? stanceHit.normal : Vector3.up;
            Vector3 groundMoveDir = Vector3.ProjectOnPlane(desiredWorldDir, groundNormal).normalized;

            float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
            float traction = Mathf.Clamp01(1f - (slopeAngle / MaxWalkableSlopeAngle));

            // Speed limit scaling
            Vector3 horizontalVel = Vector3.ProjectOnPlane(PelvisRigidbody.linearVelocity, Vector3.up);
            float currentSpeed = horizontalVel.magnitude;
            float speedRatio = Mathf.Clamp01(currentSpeed / MaxSpeed);
            float forceMultiplier = (1f - speedRatio * 0.85f) * inputMag * traction;

            // Physical directional push-off forces applied directly to the character core/pelvis
            Vector3 pushForce = groundMoveDir * (PushForce * forceMultiplier);
            PelvisRigidbody.AddForce(pushForce, ForceMode.Force);

            // Subtle swing foot assist to keep gait steps fluid across terrain
            if (swingFoot != null)
            {
                swingFoot.AddForce(Vector3.up * 25f + groundMoveDir * 20f, ForceMode.Force);
            }
        }

        private void UpdateTurning()
        {
            if (PelvisRigidbody == null) return;

            // Head / Avatar facing yaw alignment so physical pelvis smoothly and decisively tracks the VR look direction
            Vector3 headForward = HeadTransform != null ? HeadTransform.forward : PelvisRigidbody.transform.forward;
            headForward.y = 0f;
            if (headForward.sqrMagnitude > 0.001f)
            {
                headForward.Normalize();
                Vector3 currentForward = Vector3.ProjectOnPlane(PelvisRigidbody.transform.forward, Vector3.up).normalized;
                float yawAngle = Vector3.SignedAngle(currentForward, headForward, Vector3.up);

                // Responsive yaw alignment PD controller using acceleration
                float targetAngularSpeed = Mathf.Clamp(yawAngle * 8f, -6f, 6f);
                float angularError = targetAngularSpeed - PelvisRigidbody.angularVelocity.y;
                PelvisRigidbody.AddTorque(Vector3.up * (angularError * 20f), ForceMode.Acceleration);

                // Only assist foot pivots when stationary and turning in place to avoid fighting locomotion
                bool isLocomoting = InputReader != null && InputReader.MoveVector.sqrMagnitude > 0.001f;
                if (!isLocomoting && Mathf.Abs(yawAngle) > 20f)
                {
                    if (LeftFootRigidbody != null)
                    {
                        Vector3 leftPivot = Vector3.Cross(Vector3.up, LeftFootRigidbody.position - PelvisRigidbody.position).normalized * Mathf.Sign(yawAngle);
                        LeftFootRigidbody.AddForce(leftPivot * 25f, ForceMode.Force);
                    }
                    if (RightFootRigidbody != null)
                    {
                        Vector3 rightPivot = Vector3.Cross(Vector3.up, RightFootRigidbody.position - PelvisRigidbody.position).normalized * Mathf.Sign(yawAngle);
                        RightFootRigidbody.AddForce(rightPivot * 25f, ForceMode.Force);
                    }
                }
            }
        }

        private void UpdateBalanceRecovery()
        {
            if (!IsGrounded || PelvisRigidbody == null) return;

            // Calculate target upright normal from grounded feet
            Vector3 targetUp = Vector3.up;
            if (IsLeftGrounded && IsRightGrounded)
            {
                targetUp = (_leftHit.normal + _rightHit.normal).normalized;
            }
            else if (IsLeftGrounded)
            {
                targetUp = _leftHit.normal;
            }
            else if (IsRightGrounded)
            {
                targetUp = _rightHit.normal;
            }

            Vector3 currentUp = PelvisRigidbody.transform.up;
            Vector3 balanceAxis = Vector3.Cross(currentUp, targetUp);
            Vector3 balanceTorque = balanceAxis * BalanceTorqueSpring - PelvisRigidbody.angularVelocity * BalanceTorqueDamper;

            PelvisRigidbody.AddTorque(balanceTorque, ForceMode.Force);
        }
    }
}
