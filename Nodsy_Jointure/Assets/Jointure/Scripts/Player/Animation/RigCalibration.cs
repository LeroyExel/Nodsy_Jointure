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

        [Header("Base Rotations (Pre-Offset)")]
        [SerializeField] private Vector3 _baseLeftHandEuler = new Vector3(346.10f, 0f, 180f);
        [SerializeField] private Vector3 _baseRightHandEuler = new Vector3(346.10f, 0f, 180f);
        [SerializeField] private Vector3 _baseLeftFootEuler = new Vector3(0.87f, 269.49f, 239.40f);
        [SerializeField] private Vector3 _baseRightFootEuler = new Vector3(0.87f, 90.51f, 120.60f);
        [SerializeField] private bool _isCalibrated = false;

        public bool IsCalibrated => _isCalibrated;

        public Vector3 BaseLeftHandEuler { get => _baseLeftHandEuler; set => _baseLeftHandEuler = value; }
        public Vector3 BaseRightHandEuler { get => _baseRightHandEuler; set => _baseRightHandEuler = value; }
        public Vector3 BaseLeftFootEuler { get => _baseLeftFootEuler; set => _baseLeftFootEuler = value; }
        public Vector3 BaseRightFootEuler { get => _baseRightFootEuler; set => _baseRightFootEuler = value; }

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

        /// <summary>
        /// Automatically locates the target transforms in the Player Rig hierarchy if unassigned.
        /// </summary>
        public void FindReferences()
        {
            Transform root = transform;
            while (root.parent != null && root.GetComponent<Player>() == null && root.name != "[Jointure] Player Rig")
            {
                root = root.parent;
            }

            if (LeftArmTarget == null)
                LeftArmTarget = root.Find("PhysicsRig/LeftHand/LeftArmTarget");
            if (RightArmTarget == null)
                RightArmTarget = root.Find("PhysicsRig/RightHand/RightArmTarget");
            if (LeftLegTarget == null)
                LeftLegTarget = root.Find("AnimationRig/LeftFootAnchor/LeftLegTarget");
            if (RightLegTarget == null)
                RightLegTarget = root.Find("AnimationRig/RightFootAnchor/RightLegTarget");

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
        /// Captures the current target transform local rotations as the base rotations.
        /// </summary>
        public void CaptureBaseRotationsFromTargets()
        {
            if (LeftArmTarget != null) _baseLeftHandEuler = LeftArmTarget.localEulerAngles;
            if (RightArmTarget != null) _baseRightHandEuler = RightArmTarget.localEulerAngles;
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
            }
            if (RightArmTarget != null)
            {
                RightArmTarget.localRotation = Quaternion.Euler(_baseRightHandEuler) * Quaternion.Euler(RightHandOffset);
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
