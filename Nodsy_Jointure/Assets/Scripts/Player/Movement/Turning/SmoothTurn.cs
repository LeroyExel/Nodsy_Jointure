using UnityEngine;

namespace Jointure
{
    public class SmoothTurn : MonoBehaviour
    {
        private Player _player;

        private void Awake()
        {
            _player = GetComponentInParent<Player>();
        }

        private void Update()
        {
            if (_player == null) _player = GetComponentInParent<Player>();
            if (_player == null || _player.InputReader == null) return;

            float turnInput = _player.InputReader.TurnInput;
            if (Mathf.Abs(turnInput) < 0.15f) return;

            float speed = turnInput * _player.SmoothTurnSpeed * 15f * Time.deltaTime;
            Vector3 pivot = _player.transform.position;
            if (_player.ControllerRig != null && _player.ControllerRig.CameraTransform != null)
            {
                pivot = _player.ControllerRig.CameraTransform.position;
            }
            _player.transform.RotateAround(pivot, Vector3.up, speed);
        }
    }
}