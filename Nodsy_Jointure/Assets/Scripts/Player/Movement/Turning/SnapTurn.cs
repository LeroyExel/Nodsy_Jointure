using UnityEngine;

namespace Jointure
{
    public class SnapTurn : MonoBehaviour
    {
        private Player _player;

        private bool _isTurning; // Prevents multiple turns when only one is wanted

        private void Awake()
        {
            _player = GetComponentInParent<Player>();
        }

        private void Update()
        {
            bool wasTurning = _isTurning;
            _isTurning = Mathf.Abs(_player.InputReader.TurnInput) >= 0.75f;

            if (wasTurning || !_isTurning)
                return;

            float normalisedTurnInput = _player.InputReader.TurnInput / Mathf.Abs(_player.InputReader.TurnInput);
            _player.ControllerRig.transform.Rotate(0f, normalisedTurnInput * _player.SnapTurnIncrement, 0f); //Rotates player
        }
    }
}