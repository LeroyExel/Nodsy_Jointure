using UnityEngine;

namespace Jointure
{
    public class GripChecker : MonoBehaviour
    {
        public Hand Hand;

        private bool _isGrabbing;

        private void Update()
        {
            if (Hand == null || Hand.HandInputReader == null || Hand.GrabHandler == null)
                return;

            bool wasGrabbing = _isGrabbing;

            _isGrabbing = Hand.HandInputReader.Grip >= 0.5f;

            if (!wasGrabbing && _isGrabbing)
                Hand.GrabHandler.AttemptGrab();

            if (wasGrabbing && !_isGrabbing)
                Hand.GrabHandler.AttemptRelease();
        }
    }
}