using UnityEngine;

namespace Jointure
{
    public class Player : MonoBehaviour
    {
        public InputReader InputReader;

        [Header("Rigs")]
        public ControllerRig ControllerRig;
        public PhysicsRig PhysicsRig; // Maintained for legacy scene compatibility
        public AnimationRig AnimationRig;
        public RootMotion.Dynamics.PuppetMaster PuppetMaster;
        public RootMotion.FinalIK.VRIK VRIK;

        [Header("Values")]
        public float WalkSpeed = 7.5f; //The speed of the player while walking
        public float RunSpeed = 15f; //The speed of the player while running
        public float SmoothTurnSpeed = 5f; //The speed that he player smooth turns at
        public float SnapTurnIncrement = 45f; //The number of degrees the player will snap-turn
        public float HeadsetStandingHeight = 1.65f; //The headset's default height above the floor
        public float FloorOffset = 0f; //The offset from the tracking floor to the real floor

        private Rigidbody _pelvisRigidbody;

        private void Awake()
        {
            Time.fixedDeltaTime = 1f / 144f;

            if (InputReader == null) InputReader = GetComponentInChildren<InputReader>();
            if (ControllerRig == null) ControllerRig = GetComponentInChildren<ControllerRig>();
            if (AnimationRig == null) AnimationRig = GetComponentInChildren<AnimationRig>();
            if (PuppetMaster == null) PuppetMaster = GetComponentInChildren<RootMotion.Dynamics.PuppetMaster>();
            if (VRIK == null) VRIK = GetComponentInChildren<RootMotion.FinalIK.VRIK>();

            // Disconnect PuppetMaster from parent transform to allow independent PhysX simulation
            if (PuppetMaster != null)
            {
                PuppetMaster.transform.SetParent(null, true);
                var footLoco = PuppetMaster.GetComponentInChildren<PhysicsFootLocomotion>();
                if (footLoco != null)
                {
                    footLoco.InputReader = InputReader;
                    footLoco.PuppetMaster = PuppetMaster;
                    if (ControllerRig != null)
                    {
                        footLoco.HeadTransform = ControllerRig.CameraTransform;
                        footLoco.MoveHandTransform = ControllerRig.LeftControllerTransform;
                    }
                    footLoco.HandOrientedLocomotion = true;
                    footLoco.FindReferences();
                }
                if (PuppetMaster.muscles != null && PuppetMaster.muscles.Length > 0 && PuppetMaster.muscles[0].rigidbody != null)
                {
                    _pelvisRigidbody = PuppetMaster.muscles[0].rigidbody;
                }
            }

            // Ensure VRIK root is bound to the animated character avatar rather than the full rig root
            if (VRIK != null)
            {
                if (AnimationRig != null && AnimationRig.CharacterTransform != null)
                {
                    VRIK.references.root = AnimationRig.CharacterTransform;
                }
                // Bonelab-style: rotate avatar root to face head yaw immediately
                VRIK.solver.spine.maxRootAngle = 0f;
                VRIK.solver.spine.moveBodyBackWhenCrouching = 0.25f;

                // Ensure procedural leg IK locomotion is enabled
                VRIK.solver.locomotion.weight = 1f;
                VRIK.solver.locomotion.mode = RootMotion.FinalIK.IKSolverVR.Locomotion.Mode.Procedural;
                VRIK.solver.locomotion.footDistance = 0.28f;
                VRIK.solver.locomotion.stepThreshold = 0.35f;
                VRIK.solver.locomotion.angleThreshold = 45f;
                VRIK.solver.locomotion.stepSpeed = 3.5f;

                // Ensure head and hand targets are properly hooked up
                if (ControllerRig != null)
                {
                    Transform headTarget = ControllerRig.HeadTarget != null ? ControllerRig.HeadTarget : ControllerRig.CameraTransform.Find("HeadIKTarget");
                    if (headTarget != null) VRIK.solver.spine.headTarget = headTarget;

                    Transform leftTarget = ControllerRig.LeftHandTarget != null ? ControllerRig.LeftHandTarget : ControllerRig.LeftControllerTransform.Find("LeftHandIKTarget");
                    if (leftTarget != null) VRIK.solver.leftArm.target = leftTarget;

                    Transform rightTarget = ControllerRig.RightHandTarget != null ? ControllerRig.RightHandTarget : ControllerRig.RightControllerTransform.Find("RightHandIKTarget");
                    if (rightTarget != null) VRIK.solver.rightArm.target = rightTarget;
                }
            }

            // Ensure RigCalibration is hooked up and applied
            var rigCal = GetComponentInChildren<RigCalibration>();
            if (rigCal != null)
            {
                rigCal.FindReferences();
                rigCal.ApplyRotations();
            }

            // Ensure turning components are present on player
            if (GetComponent<SmoothTurn>() == null) gameObject.AddComponent<SmoothTurn>();
            if (GetComponent<SnapTurn>() == null) gameObject.AddComponent<SnapTurn>();

            HeadsetStandingHeight = PlayerPrefs.GetFloat("HeadsetStandingHeight", 1.65f);
            HeadsetStandingHeight = Mathf.Clamp(HeadsetStandingHeight, 1f, 3f);
            FloorOffset = PlayerPrefs.GetFloat("FloorOffset", 0f);
            FloorOffset = Mathf.Clamp(FloorOffset, -1.35f, 0.65f);
            SmoothTurnSpeed = PlayerPrefs.GetFloat("SmoothTurnSpeed", 10f);
            SnapTurnIncrement = PlayerPrefs.GetFloat("SnapTurnIncrement", 45f);

            ApplyFloorOffset();
            AdjustCharacterScale();

            int turnType = PlayerPrefs.GetInt("TurnType", 1);
            SetTurnMode(turnType);
        }

        private void Start()
        {
            if (PuppetMaster != null && PuppetMaster.muscles != null)
            {
                foreach (var muscle in PuppetMaster.muscles)
                {
                    if (muscle == null) continue;
                    string n = muscle.name.ToLower();
                    if (n.Contains("hips") || muscle.props.group == RootMotion.Dynamics.Muscle.Group.Hips)
                    {
                        muscle.props.pinWeight = 0f;
                        muscle.ignoreTargetVelocity = true;
                        if (muscle.rigidbody != null)
                        {
                            _pelvisRigidbody = muscle.rigidbody;
                        }
                    }
                    else if (n.Contains("foot") || muscle.props.group == RootMotion.Dynamics.Muscle.Group.Foot)
                    {
                        muscle.props.pinWeight = 0.8f;
                        muscle.ignoreTargetVelocity = false;
                    }
                }
            }
        }

        public void SetTurnMode(int mode)
        {
            var smooth = GetComponent<SmoothTurn>();
            var snap = GetComponent<SnapTurn>();
            if (smooth != null) smooth.enabled = (mode == 1);
            if (snap != null) snap.enabled = (mode == 2);
        }

        private void FixedUpdate()
        {
            UpdateRigPosition();
        }

        private Vector3 _prevPelvisPos;
        private bool _hasPrevPelvisPos;

        private void UpdateRigPosition()
        {
            if (_pelvisRigidbody == null)
            {
                if (PuppetMaster != null && PuppetMaster.muscles != null && PuppetMaster.muscles.Length > 0)
                {
                    _pelvisRigidbody = PuppetMaster.muscles[0].rigidbody;
                }
                if (_pelvisRigidbody == null) return;
            }

            Vector3 pelvisPos = _pelvisRigidbody.position;
            if (!_hasPrevPelvisPos)
            {
                _prevPelvisPos = pelvisPos;
                _hasPrevPelvisPos = true;
            }

            // Ground detection for tracking origin elevation
            float groundY = transform.position.y;
            int ignoreMask = LayerMask.GetMask("PuppetRig", "TargetRig", "JointureRig");
            int mask = ~ignoreMask;
            if (Physics.Raycast(pelvisPos + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 3f, mask, QueryTriggerInteraction.Ignore))
            {
                groundY = hit.point.y;
            }
            else
            {
                groundY = pelvisPos.y - 0.95f;
            }

            Vector3 horizontalPelvisDelta = new Vector3(pelvisPos.x - _prevPelvisPos.x, 0f, pelvisPos.z - _prevPelvisPos.z);
            _prevPelvisPos = pelvisPos;

            bool isMoving = InputReader != null && InputReader.MoveVector.sqrMagnitude > 0.001f;

            if (isMoving)
            {
                // Advance the VR tracking space origin with the physical character's locomotion displacement
                transform.position = new Vector3(transform.position.x + horizontalPelvisDelta.x, groundY, transform.position.z + horizontalPelvisDelta.z);
            }
            else
            {
                // Idle resting state: keep horizontal position locked solid! Zero phantom drift!
                transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
            }
        }

        public void ApplyFloorOffset()
        {
            if (ControllerRig != null && ControllerRig.FloorOffsetTransform != null)
            {
                ControllerRig.FloorOffsetTransform.localPosition = new Vector3(0f, FloorOffset, 0f);
            }
        }

        public void UpdateFloorOffset() => ApplyFloorOffset();

        public void AdjustCharacterScale()
        {
            // Preserve strict 1:1 VR tracking scale
            if (ControllerRig != null)
            {
                ControllerRig.transform.localScale = Vector3.one;
            }

            // Adapt character avatar and VRIK solver scale to player height
            float ratio = Mathf.Clamp(HeadsetStandingHeight / 1.68f, 0.7f, 1.4f);
            if (AnimationRig != null && AnimationRig.CharacterTransform != null)
            {
                AnimationRig.CharacterTransform.localScale = Vector3.one * ratio;
            }

            if (VRIK != null)
            {
                VRIK.solver.scale = ratio;
            }
        }

        public void ScaleCharacter() => AdjustCharacterScale();
    }
}