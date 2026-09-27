using System;
using UnityEngine;

namespace Jointure
{
    /// <summary>
    /// Provides live Inspector tuning, calibration, and automated alignment for hand and foot IK targets.
    /// Works in both Edit Mode and Play Mode with real-time visual feedback.
    /// </summary>
    [ExecuteAlways]
    public class RigCalibration : MonoBehaviour
    {
        [Header("Target References")]
        public Transform LeftArmTarget;
        public Transform RightArmTarget;
        public Transform LeftLegTarget;
        public Transform RightLegTarget;
        public Animator CharacterAnimator;

        [Header("Relative Offsets (Euler Degrees)")]
        [Tooltip("Additional rotation offset applied to Left Arm / Hand target.")]
        public Vector3 LeftHandOffset = Vector3.zero;

        [Tooltip("Additional rotation offset applied to Right Arm / Hand target.")]
        public Vector3 RightHandOffset = Vector3.zero;

        [Tooltip("Additional rotation offset applied to Left Leg / Foot target.")]
        public Vector3 LeftFootOffset = Vector3.zero;

        [Tooltip("Additional rotation offset applied to Right Leg / Foot target.")]
        public Vector3 RightFootOffset = Vector3.zero;

        [Header("Position Offsets (Meters)")]
        [Tooltip("Additional position offset applied to Left Arm / Hand target.")]
        public Vector3 LeftHandPosOffset = Vector3.zero;

        [Tooltip("Additional position offset applied to Right Arm / Hand target.")]
        public Vector3 RightHandPosOffset = Vector3.zero;

        [Header("Base Rotations (Pre-Offset)")]
        [SerializeField] private Vector3 _baseLeftHandEuler = new Vector3(285f, 0f, 90f);
        [SerializeField] private Vector3 _baseRightHandEuler = new Vector3(285f, 0f, 270f);
        [SerializeField] private Vector3 _baseLeftFootEuler = new Vector3(0.87f, 269.49f, 239.40f);
        [SerializeField] private Vector3 _baseRightFootEuler = new Vector3(0.87f, 90.51f, 120.60f);

        [Header("Base Positions (Pre-Offset)")]
        [SerializeField] private Vector3 _baseLeftHandPos = new Vector3(-0.01f, -0.04f, -0.08f);
        [SerializeField] private Vector3 _baseRightHandPos = new Vector3(0.01f, -0.04f, -0.08f);

        [SerializeField] private bool _isCalibrated = false;

        public bool IsCalibrated => _isCalibrated;

        public Vector3 BaseLeftHandEuler { get => _baseLeftHandEuler; set => _baseLeftHandEuler = value; }
        public Vector3 BaseRightHandEuler { get => _baseRightHandEuler; set => _baseRightHandEuler = value; }
        public Vector3 BaseLeftFootEuler { get => _baseLeftFootEuler; set => _baseLeftFootEuler = value; }
        public Vector3 BaseRightFootEuler { get => _baseRightFootEuler; set => _baseRightFootEuler = value; }
        public Vector3 BaseLeftHandPos { get => _baseLeftHandPos; set => _baseLeftHandPos = value; }
        public Vector3 BaseRightHandPos { get => _baseRightHandPos; set => _baseRightHandPos = value; }

        private void Reset()
        {
            FindReferences();
            CaptureBaseRotationsFromTargets();
        }

        private void Awake()
        {
            FindReferences();
        }

        private void OnEnable()
        {
            FindReferences();
            ApplyRotations();
        }

        private void OnValidate()
        {
            FindReferences();
            ApplyRotations();
        }

        private void Update()
        {
            ApplyRotations();
        }

        /// <summary>
        /// Automatically locates the target transforms in the Player Rig hierarchy if unassigned.
        /// </summary>
        public void FindReferences()
        {
            Transform root = transform;
            while (root.parent != null && root.GetComponent<Player>() == null && !root.name.Contains("[Jointure] Player Rig"))
            {
                root = root.parent;
            }

            var ctrlRig = root.GetComponentInChildren<ControllerRig>();
            if (ctrlRig != null)
            {
                if (LeftArmTarget == null && ctrlRig.LeftHandTarget != null) LeftArmTarget = ctrlRig.LeftHandTarget;
                if (RightArmTarget == null && ctrlRig.RightHandTarget != null) RightArmTarget = ctrlRig.RightHandTarget;

                if (LeftArmTarget == null && ctrlRig.LeftControllerTransform != null)
                    LeftArmTarget = ctrlRig.LeftControllerTransform.Find("LeftHandIKTarget");
                if (RightArmTarget == null && ctrlRig.RightControllerTransform != null)
                    RightArmTarget = ctrlRig.RightControllerTransform.Find("RightHandIKTarget");
            }

            var vrik = root.GetComponentInChildren<RootMotion.FinalIK.VRIK>();
            if (vrik != null)
            {
                if (LeftArmTarget == null && vrik.solver.leftArm.target != null) LeftArmTarget = vrik.solver.leftArm.target;
                if (RightArmTarget == null && vrik.solver.rightArm.target != null) RightArmTarget = vrik.solver.rightArm.target;
            }

            // Fallbacks
            if (LeftArmTarget == null)
            {
                LeftArmTarget = root.Find("ControllerRig/CameraOffset/FloorOffset/LeftController/LeftHandIKTarget")
                             ?? root.Find("PhysicsRig/LeftHand/LeftArmTarget")
                             ?? root.Find("PhysicsRig/LeftHand");
            }
            if (RightArmTarget == null)
            {
                RightArmTarget = root.Find("ControllerRig/CameraOffset/FloorOffset/RightController/RightHandIKTarget")
                              ?? root.Find("PhysicsRig/RightHand/RightArmTarget")
                              ?? root.Find("PhysicsRig/RightHand");
            }
            if (LeftLegTarget == null) LeftLegTarget = root.Find("AnimationRig/LeftFootAnchor/LeftLegTarget");
            if (RightLegTarget == null) RightLegTarget = root.Find("AnimationRig/RightFootAnchor/RightLegTarget");

            if (CharacterAnimator == null)
            {
                Transform charT = root.Find("AnimationRig/Character");
                if (charT != null)
                {
                    CharacterAnimator = charT.GetComponent<Animator>();
                }
                if (CharacterAnimator == null)
                {
                    CharacterAnimator = root.GetComponentInChildren<Animator>();
                }
            }
        }

        /// <summary>
        /// Flips the specified hand by 180 degrees roll.
        /// </summary>
        public void FlipHandRoll(bool isLeft)
        {
            if (isLeft)
            {
                LeftHandOffset.z = (LeftHandOffset.z + 180f) % 360f;
            }
            else
            {
                RightHandOffset.z = (RightHandOffset.z + 180f) % 360f;
            }
            ApplyRotations();
        }

        /// <summary>
        /// Captures the current target transform local rotations and positions as the base values.
        /// </summary>
        public void CaptureBaseRotationsFromTargets()
        {
            if (LeftArmTarget != null)
            {
                _baseLeftHandEuler = LeftArmTarget.localEulerAngles;
                _baseLeftHandPos = LeftArmTarget.localPosition;
            }
            if (RightArmTarget != null)
            {
                _baseRightHandEuler = RightArmTarget.localEulerAngles;
                _baseRightHandPos = RightArmTarget.localPosition;
            }
            if (LeftLegTarget != null) _baseLeftFootEuler = LeftLegTarget.localEulerAngles;
            if (RightLegTarget != null) _baseRightFootEuler = RightLegTarget.localEulerAngles;
        }

        /// <summary>
        /// Applies the base rotations combined with the offset rotations to all target transforms.
        /// </summary>
        public void ApplyRotations()
        {
            if (LeftArmTarget != null)
            {
                LeftArmTarget.localRotation = Quaternion.Euler(_baseLeftHandEuler) * Quaternion.Euler(LeftHandOffset);
                LeftArmTarget.localPosition = _baseLeftHandPos + LeftHandPosOffset;
            }
            if (RightArmTarget != null)
            {
                RightArmTarget.localRotation = Quaternion.Euler(_baseRightHandEuler) * Quaternion.Euler(RightHandOffset);
                RightArmTarget.localPosition = _baseRightHandPos + RightHandPosOffset;
            }
            if (LeftLegTarget != null)
            {
                LeftLegTarget.localRotation = Quaternion.Euler(_baseLeftFootEuler) * Quaternion.Euler(LeftFootOffset);
            }
            if (RightLegTarget != null)
            {
                RightLegTarget.localRotation = Quaternion.Euler(_baseRightFootEuler) * Quaternion.Euler(RightFootOffset);
            }
        }

        /// <summary>
        /// Sets base target rotations and marks the rig as calibrated.
        /// </summary>
        public void SetBaseRotations(Vector3 leftHand, Vector3 rightHand, Vector3 leftFoot, Vector3 rightFoot)
        {
            _baseLeftHandEuler = leftHand;
            _baseRightHandEuler = rightHand;
            _baseLeftFootEuler = leftFoot;
            _baseRightFootEuler = rightFoot;
            _isCalibrated = true;
            ApplyRotations();
        }

        /// <summary>
        /// Bakes the current combined (base + offset) rotations directly into the base rotations, resetting offsets to zero.
        /// </summary>
        public void BakeOffsets()
        {
            if (LeftArmTarget != null) _baseLeftHandEuler = LeftArmTarget.localEulerAngles;
            if (RightArmTarget != null) _baseRightHandEuler = RightArmTarget.localEulerAngles;
            if (LeftLegTarget != null) _baseLeftFootEuler = LeftLegTarget.localEulerAngles;
            if (RightLegTarget != null) _baseRightFootEuler = RightLegTarget.localEulerAngles;

            LeftHandOffset = Vector3.zero;
            RightHandOffset = Vector3.zero;
            LeftFootOffset = Vector3.zero;
            RightFootOffset = Vector3.zero;

            ApplyRotations();
        }

        /// <summary>
        /// Resets all relative offsets to zero.
        /// </summary>
        public void ResetOffsets()
        {
            LeftHandOffset = Vector3.zero;
            RightHandOffset = Vector3.zero;
            LeftFootOffset = Vector3.zero;
            RightFootOffset = Vector3.zero;
            ApplyRotations();
        }

        /// <summary>
        /// Adds a rotation step to the specified target offset.
        /// </summary>
        public void NudgeOffset(ref Vector3 offset, Vector3 delta)
        {
            offset += delta;
            ApplyRotations();
        }
    }
}
