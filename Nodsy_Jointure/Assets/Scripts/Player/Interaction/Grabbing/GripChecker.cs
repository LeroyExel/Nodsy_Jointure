using UnityEngine;

namespace Jointure
{
    public class GripChecker : MonoBehaviour
    {
        public Hand Hand;

        private bool _isGrabbing;

        private void Update()
        {
            bool wasGrabbing = _isGrabbing;

            _isGrabbing = Hand.HandInputReader.Grip >= 0.5f;

            if (!wasGrabbing && _isGrabbing)
                Hand.GrabHandler.AttemptGrab();

            if (wasGrabbing && !_isGrabbing)
                Hand.GrabHandler.AttemptRelease();
        }
    }
}