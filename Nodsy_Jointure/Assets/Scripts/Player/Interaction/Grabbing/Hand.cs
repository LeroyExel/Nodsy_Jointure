using UnityEngine;

namespace Jointure
{
    public class Hand : MonoBehaviour
    {
        public HandAnimator HandAnimator;
        public Grab CurrentGrab;
        public HandInputReader HandInputReader;
        public Transform PalmTransform;
        public PhysicsHand PhysicsHand;
        public Transform PhysicsHandTransform;
        public GrabHandler GrabHandler;
        public bool IsLeftHand;
        public Hand otherHand;
        public Collider PhysicsHandCollider;
    }
}
