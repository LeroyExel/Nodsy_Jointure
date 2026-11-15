using UnityEngine;

namespace Jointure.Samples
{
    public class GunAnimator : MonoBehaviour
    {
        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void UpdateTrigger(float trigger, bool primary, bool secondary)
        {
            trigger = Mathf.Clamp(trigger, 0f, 0.99f);
            _animator.SetFloat("Trigger", Mathf.Lerp(_animator.GetFloat("Trigger"), trigger, Time.deltaTime * 25f));
        }

        public void ResetTrigger()
        {
            _animator.SetFloat("Trigger", 0f);
        }

        public void UpdateSlide(float slide)
        {
            _animator.SetFloat("Slide", Mathf.Clamp(slide, 0f, 0.99f));
        }
    }
}
