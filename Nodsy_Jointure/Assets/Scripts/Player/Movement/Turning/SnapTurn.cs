using UnityEngine;

namespace Jointure
{
    public class SnapTurn : MonoBehaviour
    {
        private Player _player;
        private bool _isTurning;

        private void Awake()
        {
            _player = GetComponentInParent<Player>();
        }

        private void Update()
        {
            if (_player == null) _player = GetComponentInParent<Player>();
            if (_player == null || _player.InputReader == null) return;

            float turnInput = _player.InputReader.TurnInput;
            bool wantsTurn = Mathf.Abs(turnInput) >= 0.65f;

            if (!wantsTurn)
            {
                _isTurning = false;
                return;
            }

            if (_isTurning) return;

            _isTurning = true;
            float angle = Mathf.Sign(turnInput) * _player.SnapTurnIncrement;
            Vector3 pivot = _player.transform.position;
            if (_player.ControllerRig != null && _player.ControllerRig.CameraTransform != null)
            {
                pivot = _player.ControllerRig.CameraTransform.position;
            }
            _player.transform.RotateAround(pivot, Vector3.up, angle);
        }
    }
}