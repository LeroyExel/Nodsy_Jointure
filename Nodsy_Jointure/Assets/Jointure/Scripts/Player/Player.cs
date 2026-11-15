using UnityEngine;

namespace Jointure
{
    public class Player : MonoBehaviour
    {
        public InputReader InputReader;

        [Header("Rigs")]
        public ControllerRig ControllerRig;
        public PhysicsRig PhysicsRig;
        public AnimationRig AnimationRig;

        [Header("Values")]
        public float WalkSpeed = 7.5f; //The speed of the player while walking
        public float RunSpeed = 15f; //The speed of the player while running
        public float SmoothTurnSpeed = 5f; //The speed that he player smooth turns at
        public float SnapTurnIncrement = 45f; //The number of degrees the player will snap-turn
        public float HeadsetStandingHeight = 1.65f; //The headset's default height above the floor
        public float FloorOffset = 0f; //The offset from the tracking floor to the real floor

        private void Awake()
        {
            Time.fixedDeltaTime = 1f / 144f;

            ControllerRig.transform.parent = PhysicsRig.PelvisRigidbody.transform;
            ControllerRig.FloorOffsetTransform.localPosition = new Vector3(0f, PlayerPrefs.GetFloat("FloorOffset", 0f), 0f);

            HeadsetStandingHeight = PlayerPrefs.GetFloat("HeadsetStandingHeight", 1.65f);
            HeadsetStandingHeight = Mathf.Clamp(HeadsetStandingHeight, 1f, 3f);
            FloorOffset = PlayerPrefs.GetFloat("FloorOffset", 0f);
            FloorOffset = Mathf.Clamp(FloorOffset, -1.35f, 0.65f);
            SmoothTurnSpeed = PlayerPrefs.GetFloat("SmoothTurnSpeed", 10f);
            SnapTurnIncrement = PlayerPrefs.GetFloat("SnapTurnIncrement", 45f);

            AdjustCharacterScale();
        }

        public void ApplyFloorOffset()
        {
            ControllerRig.FloorOffsetTransform.localPosition = new Vector3(0f, FloorOffset, 0f);
        }

        public void UpdateFloorOffset() => ApplyFloorOffset();

        public void AdjustCharacterScale()
        {
            float scaleFactor = 1.65f / HeadsetStandingHeight; //1.65 is eye height of character model
            ControllerRig.transform.localScale = Vector3.one * scaleFactor;
        }

        public void ScaleCharacter() => AdjustCharacterScale();
    }
}