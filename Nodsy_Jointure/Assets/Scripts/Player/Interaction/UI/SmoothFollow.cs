using UnityEngine;

namespace Jointure
{
    public class SmoothFollow : MonoBehaviour
    {
        [Range(0, 1)]
        public float PositionDamping = 0.1f;
        [Range(0, 1)]
        public float RotationDamping = 0.1f;

        [SerializeField]
        private Transform _target;

        private void OnEnable()
        {
            transform.position = _target.position;
            transform.rotation = _target.rotation;
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, _target.position, PositionDamping);
            transform.rotation = Quaternion.Lerp(transform.rotation, _target.rotation, RotationDamping);
        }
    }
}
