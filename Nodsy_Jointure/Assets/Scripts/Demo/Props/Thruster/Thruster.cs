using UnityEngine;

namespace Jointure.Samples
{
    public class Thruster : MonoBehaviour
    {
        public float Power = 3f;

        [SerializeField]
        private Transform _barrelTransform;

        [SerializeField]
        private ParticleSystem _muzzleFlash;

        private Rigidbody _thrusterRigidbody;

        private void Awake()
        {
            _thrusterRigidbody = GetComponent<Rigidbody>();
        }

        public void Tick()
        {
            if (!_muzzleFlash.isPlaying)
                return;

            _thrusterRigidbody.AddForce(-_barrelTransform.forward * Power, ForceMode.Impulse);

            RaycastHit hit;
            if (Physics.Raycast(_barrelTransform.position, _barrelTransform.forward, out hit, 5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.rigidbody)
                    return;

                hit.rigidbody.AddForceAtPosition(_barrelTransform.forward * Power / 10f, hit.point, ForceMode.Impulse);
            }
        }

        public void Trigger()
        {
            _muzzleFlash.Play();
        }

        public void Stop()
        {
            _muzzleFlash.Stop();
        }
    }
}