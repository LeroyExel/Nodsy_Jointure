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
            if (Mathf.Abs(_player.InputReader.TurnInput) < 0.75f)
                return;

            float normalisedTurnInput = _player.InputReader.TurnInput / Mathf.Abs(_player.InputReader.TurnInput);
            _player.ControllerRig.transform.Rotate(0f, normalisedTurnInput * 20f * _player.SmoothTurnSpeed * Time.deltaTime, 0f); //Rotates player
        }
    }
}