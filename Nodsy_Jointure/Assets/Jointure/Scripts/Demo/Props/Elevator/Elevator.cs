using UnityEngine;

namespace Jointure.Samples
{
    public class Elevator : MonoBehaviour
    {
        public float MoveSpeed = 2f;
        public float SmoothTime = 2f;

        [SerializeField]
        private Transform[] _floorTransforms;

        [SerializeField]
        private int _currentFloor;

        private Rigidbody _rigidbody;
        private Vector3 _currentVelocity;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void SetFloor(int floor)
        {
            if (floor >= _floorTransforms.Length)
                return;

            _currentFloor = floor;
        }

        public void CycleFloor()
        {
            _currentFloor++;

            if (_currentFloor >= _floorTransforms.Length)
                _currentFloor = 0;
        }

        private void FixedUpdate()
        {
            _rigidbody.MovePosition(
                Vector3.SmoothDamp(
                    _rigidbody.position,
                    _floorTransforms[_currentFloor].position,
                    ref _currentVelocity,
                    SmoothTime,
                    MoveSpeed
                )
            );
        }
    }
}
