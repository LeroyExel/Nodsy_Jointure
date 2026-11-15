using UnityEngine;
using UnityEngine.Events;

namespace Jointure.Samples
{
    [AddComponentMenu("Jointure/Health")]
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField]
        private float _startingHealth;

        [SerializeField]
        UnityEvent _deathEvent;

        private float _currentHealth;

        private void Start()
        {
            _currentHealth = _startingHealth;
        }

        public void TakeDamage(float damageAmount)
        {
            _currentHealth -= damageAmount;

            if (_currentHealth > 0)
                return;

            _currentHealth = 0;
            _deathEvent?.Invoke();
        }
    }
}
