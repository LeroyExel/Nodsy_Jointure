using UnityEngine;
using Jointure;

namespace Jointure.Samples
{
    [RequireComponent(typeof(AudioSource))]
    public class RandomSound : MonoBehaviour
    {
        [SerializeField]
        private AudioClip[] _audioClips;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void PlayRandomSound()
        {
            _audioSource.PlayOneShot(Utilities.RandomAudioClip(_audioClips));
        }
    }
}
