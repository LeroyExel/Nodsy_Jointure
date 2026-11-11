using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Jointure.Samples
{
    public class Target : MonoBehaviour, IDamageable
    {
        public UnityEvent DamagedEvent;

        [SerializeField] private Renderer _meshRenderer1;
        [SerializeField] private Renderer _meshRenderer2;
        [SerializeField] private AudioSource _audioSource;
        private bool _cooldown = false;

        public void TakeDamage(float damageAmount)
        {
            if (_cooldown)
                return;

            _cooldown = true;
            StartCoroutine(TargetShot());
        }

        private IEnumerator TargetShot()
        {
            Color originalColor = _meshRenderer1.material.color;
            _meshRenderer1.material.color = Color.red;
            _meshRenderer2.material.color = Color.red;
            _audioSource.Play();
            DamagedEvent?.Invoke();
            yield return new WaitForSeconds(0.25f);
            _meshRenderer1.material.color = originalColor;
            _meshRenderer2.material.color = originalColor;
            _cooldown = false;
        }
    }
}
