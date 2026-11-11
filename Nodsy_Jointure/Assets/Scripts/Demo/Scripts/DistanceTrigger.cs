using UnityEngine;
using UnityEngine.Events;

namespace Jointure.Samples
{
    [AddComponentMenu("Jointure/Distance Trigger")]
    public class DistanceTrigger : MonoBehaviour
    {
        public UnityEvent OnActivate, OnDeactivate;
        public UpdateEvent OnUpdate;

        public float Value;

        [SerializeField]
        private float _threshold = 0.05f, _maxDistance = 0.5f;

        [SerializeField]
        private Transform _activatedTransform;

        private bool _isActivated;

        private void FixedUpdate()
        {
            Value = GetValue();
            OnUpdate.Invoke(Value);

            if (!_isActivated && Value + _threshold >= 1)
                Activated();

            if (_isActivated && Value - _threshold <= 0)
                Deactivated();
        }

        private float GetValue()
        {
            return 1 - Mathf.Clamp01(Vector3.Distance(transform.position, _activatedTransform.position) / _maxDistance);
        }

        private void Activated()
        {
            _isActivated = true;
            OnActivate.Invoke();
        }

        private void Deactivated()
        {
            _isActivated = false;
            OnDeactivate.Invoke();
        }

        [System.Serializable]
        public class UpdateEvent : UnityEvent<float> { }
    }
}