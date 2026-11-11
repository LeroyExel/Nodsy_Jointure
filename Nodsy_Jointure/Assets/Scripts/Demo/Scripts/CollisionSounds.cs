using UnityEngine;
using Jointure;

namespace Jointure.Samples
{
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("Jointure/Collision Sounds")]
    public class CollisionSounds : MonoBehaviour
    {
        [SerializeField]
        private float _thresholdImpulse = 1f;

        [SerializeField]
        private AnimationCurve _volumeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        private AudioClip[] _collisionClips;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.impulse.magnitude < _thresholdImpulse)
                return;

            _audioSource.volume = _volumeCurve.Evaluate(collision.impulse.magnitude - _thresholdImpulse);
            _audioSource.PlayOneShot(Utilities.RandomAudioClip(_collisionClips));
        }
    }
}
