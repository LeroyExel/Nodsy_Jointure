using UnityEngine;

namespace Jointure
{
    public class SmoothLocomotion : MonoBehaviour
    {
        private Player _player;

        private bool _isRunning = false;
        private float _currentSpeed;
        private float _liftoffVelocity = 0f;

        private void Awake()
        {
            _player = GetComponentInParent<Player>();
        }

        private void Start() //Resets important variables
        {
            _currentSpeed = _player.WalkSpeed;
            _player.PhysicsRig.LocomotionSphereRigidbody.maxAngularVelocity = Mathf.Infinity;
            _liftoffVelocity = 0f;
            _player.PhysicsRig.JumpState = PhysicsRig.JumpStates.NotJumping;
            _player.PhysicsRig.LocomotionSphereRigidbody.mass = 25f;
            SetFenderPelvisJointDrive(80000f, 10000f, 0f);
        }

        private void OnDisable() //Reset important variables
        {
            _isRunning = false;
            _currentSpeed = _player.WalkSpeed;
            _liftoffVelocity = 0f;
            _player.PhysicsRig.JumpState = PhysicsRig.JumpStates.NotJumping;
            _player.PhysicsRig.LocomotionSphereRigidbody.mass = 25f;
            SetFenderPelvisJointDrive(80000f, 10000f, 0f);
        }

        private void FixedUpdate()
        {
            Movement();

            if (_player.PhysicsRig.JumpState != PhysicsRig.JumpStates.NotJumping)
                Jumping();
        }

        private void Movement()
        {
            //Input
            Quaternion headYaw = Quaternion.LookRotation(Vector3.Cross(_player.ControllerRig.CameraTransform.right, Vector3.up)); //Gets player direction
            Vector3 moveVector = headYaw * new Vector3(_player.InputReader.MoveVector.x, 0f, _player.InputReader.MoveVector.y);

            //Running
            if (moveVector.magnitude < 0.1f)
                _isRunning = false;

            _currentSpeed = _isRunning ? _player.RunSpeed : _player.WalkSpeed; //Condensed if-else statement to set currentSpeed

            //Movement
            Vector3 targetAngularVelocity = Quaternion.Euler(0f, 90f, 0f) * moveVector;
            _player.PhysicsRig.LocomotionSphereRigidbody.angularVelocity = targetAngularVelocity * _currentSpeed; //Rolls at speed in direction

            //Air acceleration
            if (_player.PhysicsRig.LocomotionSphere.IsGrounded)
                return;

            AirAccelerate(_player.PhysicsRig.LocomotionSphereRigidbody, moveVector);
            AirAccelerate(_player.PhysicsRig.FenderRigidbody, moveVector);
            AirAccelerate(_player.PhysicsRig.PelvisRigidbody, moveVector);
            AirAccelerate(_player.PhysicsRig.HeadRigidbody, moveVector);
            AirAccelerate(_player.PhysicsRig.LeftHandRigidbody, moveVector);
            AirAccelerate(_player.PhysicsRig.RightHandRigidbody, moveVector);
        }

        private void Jumping()
        {
            float verticalVelocity = _player.PhysicsRig.PelvisRigidbody.linearVelocity.y;

            switch (_player.PhysicsRig.JumpState)
            {
                case PhysicsRig.JumpStates.PushingGround: //Pushing on ground to lift up
                    if (_player.PhysicsRig.LocomotionSphere.IsGrounded || verticalVelocity < 0f)
                        break;

                    _player.PhysicsRig.JumpState = PhysicsRig.JumpStates.Ascending; //Player is no longer grounded
                    _liftoffVelocity = verticalVelocity; //Get velocity player leaves ground at
                    _player.PhysicsRig.LocomotionSphereRigidbody.mass = 5f; //Set to small amount to prevent upkick
                    break;
                case PhysicsRig.JumpStates.Ascending: //In air, ascending
                    _player.PhysicsRig.FenderPelvisOffset += 0.65f * Physics.gravity.y * Time.fixedDeltaTime / _liftoffVelocity; //Retract legs

                    if (verticalVelocity < 0f)
                        _player.PhysicsRig.JumpState = PhysicsRig.JumpStates.Descending; //Player has passed maximum height

                    break;
                case PhysicsRig.JumpStates.Descending: //In air, descending
                    _player.PhysicsRig.FenderPelvisOffset -= 0.65f * Physics.gravity.y * Time.fixedDeltaTime / _liftoffVelocity; //Extend legs
                    if (_player.PhysicsRig.LocomotionSphere.IsGrounded)
                    {
                        SetFenderPelvisJointDrive(80000f, 10000f, 0f); //Reset joint
                        _player.PhysicsRig.LocomotionSphereRigidbody.mass = 25f;
                    }
                    if (_player.PhysicsRig.RealFenderPelvisOffset < 0.55f) //If player has returned to full standing height
                        break;

                    _player.PhysicsRig.JumpState = PhysicsRig.JumpStates.NotJumping;
                    SetFenderPelvisJointDrive(80000f, 10000f, 0f); //Reset joint
                    _player.PhysicsRig.LocomotionSphereRigidbody.mass = 25f; //Reset to original mass
                    break;
            }
        }

        public void Run() //Called when the run button is pressed
        {
            if (!enabled)
                return;

            _isRunning = !_isRunning;
        }

        public void AnticipateJump() //Called when the jump button is pressed
        {
            if (!enabled)
                return;

            if (!_player.PhysicsRig.LocomotionSphere.IsGrounded || _player.PhysicsRig.JumpState != PhysicsRig.JumpStates.NotJumping) //Player can only start jump if grounded
                return;

            SetFenderPelvisJointDrive(80000f, 9000f, 0f);
            _player.PhysicsRig.FenderPelvisOffset -= 0.2f;
            _player.PhysicsRig.JumpState = PhysicsRig.JumpStates.Anticipation; //Player is now anticipating
        }

        public void Jump() //Called when the jump button is released
        {
            if (!enabled)
                return;

            if (_player.PhysicsRig.JumpState != PhysicsRig.JumpStates.Anticipation)
                return;

            //Joint configuration
            float crouchingAmount = 0.55f - _player.PhysicsRig.RealFenderPelvisOffset;
            float spring = 41000f / crouchingAmount - 26000f; //Calculates spring of joint needed
            SetFenderPelvisJointDrive(spring, 2000f, 0f);

            //Jump
            _player.PhysicsRig.FenderPelvisOffset = 0.55f; //Return to full standing height
            _player.PhysicsRig.JumpState = PhysicsRig.JumpStates.PushingGround; //Player is now pushing on the ground
        }

        private void SetFenderPelvisJointDrive(float spring, float damper, float maxForce)
        {
            JointDrive jointDrive = _player.PhysicsRig.FenderPelvisJoint.yDrive;

            if (spring != 0f)
                jointDrive.positionSpring = spring;

            if (damper != 0f)
                jointDrive.positionDamper = damper;

            if (maxForce != 0f)
                jointDrive.maximumForce = maxForce;

            _player.PhysicsRig.FenderPelvisJoint.yDrive = jointDrive;
        }

        private void AirAccelerate(Rigidbody rigidbody, Vector3 direction)
        {
            Vector3 horizontalVelocity = new Vector3(rigidbody.linearVelocity.x, 0f, rigidbody.linearVelocity.z); //Flattens vector

            if (horizontalVelocity.magnitude < 1f) //Prevents infinite acceleration
                rigidbody.linearVelocity += direction * _player.PhysicsRig.AirAcceleration * Time.fixedDeltaTime;
        }
    }
}