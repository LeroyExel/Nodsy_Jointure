using UnityEngine;

namespace Jointure
{
    [AddComponentMenu("Jointure/Grabs/Grab (Basic)")]
    public class Grab : MonoBehaviour
    {
        [HideInInspector]
        public Hand LeftHand, RightHand;

        public HandPose HandPose;
        public delegate void Release();
        public event Release ReleaseEvent;
        public bool IsLeftHanded = true, IsRightHanded = true;
        public Grab[] EnableGrabs = new Grab[0], DisableGrabs = new Grab[0];

        public const int PriorityEnvironment = -10;
        public const int PrioritySecondary = 5;
        public const int PriorityDefault = 10;
        public const int PriorityPrimary = 20;

        [Header("Priority & Detection")]
        [Tooltip("Priority tier for grabbing. Higher priority grabs take precedence over lower priority grabs within the grab bounds.\nSuggestions: Environment = -10, Secondary (slide/latches) = 5, Default/Props = 10, Primary Handles = 20")]
        public int Priority = PriorityDefault;

        [Tooltip("Optional maximum grab distance in meters from hand palm to grab collider. If > 0, grab will only be eligible if palm is within this distance (0 = no limit, uses grab bounds box).")]
        public float MaxGrabDistance = 0f;

        [Tooltip("Multiplier applied to the grab rank (reciprocal distance / orientation). Lower values make it grab 'less fast' / less eager.")]
        public float RankMultiplier = 1f;

        private Rigidbody _rigidBody;
        private ArticulationBody _articulationBody;
        private Transform _body;

        [HideInInspector]
        public Collider Collider;

        private void OnEnable()
        {
            EnsureBody();

            Collider = GetComponent<Collider>();
            if (Collider)
                return;

            CreateCollider();
        }

        public void EnsureBody()
        {
            if (_body)
                return;

            _body = Utilities.FindRigidBodyInHierarchy(transform, out _rigidBody, out _articulationBody);
            if (!_body)
            {
                Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                _rigidBody = rigidbody;
                _body = _rigidBody.transform;
            }
        }

        public virtual void CreateCollider()
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.01f;
            Collider = collider;
        }

        public virtual float EvaluateGrabRank(Transform handTransform) //Returned when in player grab range
        {
            if (!Collider)
                Collider = GetComponent<Collider>();

            if (!Collider)
                return 0f;

            if (Collider is MeshCollider)
                return (1f / 1000f) * RankMultiplier;

            float distance = Vector3.Distance(handTransform.position, Collider.ClosestPoint(handTransform.position));
            if (distance <= 0.0001f)
                return 10000f * RankMultiplier;

            return (1f / distance) * RankMultiplier; //Reciprocal of distance from hand to grab
        }

        public virtual float CalculateRank(Transform handTransform) => EvaluateGrabRank(handTransform);

        public virtual void OnGrab(Hand hand) //Triggered when player grabs the grab
        {
            hand.CurrentGrab = this;

            if (hand.IsLeftHand)
                LeftHand = hand;
            else
                RightHand = hand;

            hand.GrabHandler.ApplyGrabPose(HandPose); //Use the hand pose attached

            AlignHand(hand);
            CreateGrabJoint(hand);

            IgnoreCollision(hand, true);

            if (EnableGrabs != null)
            {
                foreach (Grab grab in EnableGrabs)
                {
                    if (grab)
                        grab.enabled = true;
                }
            }
            if (DisableGrabs != null)
            {
                foreach (Grab grab in DisableGrabs)
                {
                    if (grab)
                        grab.enabled = false;
                }
            }

            GetComponent<Interactable>()?.OnGrab();
        }

        public virtual void IgnoreCollision(Hand hand, bool ignore)
        {
            EnsureBody();
            if (!hand || !hand.PhysicsHandTransform || !_body)
                return;

            Collider[] bodyColliders = _body.GetComponentsInChildren<Collider>();
            Collider[] handColliders = hand.PhysicsHandTransform.GetComponentsInChildren<Collider>();

            foreach (Collider collider in bodyColliders)
            {
                foreach (Collider handCollider in handColliders)
                {
                    Physics.IgnoreCollision(collider, handCollider, ignore);
                }
            }
        }

        public virtual void AlignHand(Hand hand) { }

        private void CreateGrabJoint(Hand hand)
        {
            FixedJoint grabJoint = hand.PhysicsHandTransform.gameObject.AddComponent<FixedJoint>();
            grabJoint.enableCollision = false;
            if (_rigidBody)
                grabJoint.connectedBody = _rigidBody;
            if (_articulationBody)
                grabJoint.connectedArticulationBody = _articulationBody;
        }

        public void OnRelease(Hand hand, bool toggleGrabs) //Triggered when player releases the grab
        {
            if (!hand)
                return;

            DestroyGrabJoint(hand);

            if (toggleGrabs)
            {
                if (EnableGrabs != null)
                {
                    foreach (Grab grab in EnableGrabs)
                    {
                        if (grab)
                            grab.enabled = false;
                    }
                }
                if (DisableGrabs != null)
                {
                    foreach (Grab grab in DisableGrabs)
                    {
                        if (grab)
                            grab.enabled = true;
                    }
                }
            }

            GetComponent<Interactable>()?.OnRelease();

            hand.CurrentGrab = null;

            if (hand.IsLeftHand)
                LeftHand = null;
            else
                RightHand = null;

            ReleaseEvent?.Invoke();
        }

        public virtual void DestroyGrabJoint(Hand hand)
        {
            if (!hand)
                return;

            FixedJoint grabJoint = hand.PhysicsHandTransform.GetComponent<FixedJoint>(); //Gets the grab joint
            Destroy(grabJoint); //Deletes the joint, letting it go

            IgnoreCollision(hand, false);

            if (!gameObject.activeSelf)
                return;
        }

        private void OnDisable()
        {
            OnRelease(LeftHand, false);
            OnRelease(RightHand, false);
        }
    }
}