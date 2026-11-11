using UnityEngine;

namespace Jointure.Samples
{
    public class Magazine : MonoBehaviour
    {
        public int MaxRounds;
        public int RemainingRounds;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private GameObject[] _visualRounds;

        private void Awake()
        {
            UpdateAnimator();

            for (int i = _visualRounds.Length - 1; i > 0; i--)
                if (RemainingRounds <= i)
                    _visualRounds[i]?.SetActive(false);
        }

        public void RemoveRound()
        {
            RemainingRounds--;
            _visualRounds[RemainingRounds]?.SetActive(false);
            UpdateAnimator();
        }

        private void UpdateAnimator()
        {
            _animator.SetFloat("Full", (float) RemainingRounds / MaxRounds);
        }
    }
}