using UnityEngine;
using UnityEngine.Events;

namespace Jointure.Samples
{
    [AddComponentMenu("Jointure/Touch Detector")]
    public class TouchDetector : MonoBehaviour
    {
        [SerializeField]
        private LayerMask _layerMask;

        [SerializeField]
        private UnityEvent _enterEvent;

        [SerializeField]
        private UnityEvent _exitEvent;

        [SerializeField]
        private bool _destroyOnTouch;

        private bool _touched;

        private void OnCollisionEnter(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & _layerMask) != 0)
                Enter();
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (((1 << collider.gameObject.layer) & _layerMask) != 0)
                Enter();
        }

        private void OnCollisionExit(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & _layerMask) != 0)
                Exit();
        }

        private void OnTriggerExit(Collider collider)
        {
            if (((1 << collider.gameObject.layer) & _layerMask) != 0)
                Exit();
        }

        private void Enter()
        {
            if (_destroyOnTouch)
            {
                Destroy(gameObject);

                if (_touched)
                    return;
            }

            _touched = true;

            _enterEvent.Invoke();
        }

        private void Exit()
        {
            _exitEvent.Invoke();
        }
    }
}