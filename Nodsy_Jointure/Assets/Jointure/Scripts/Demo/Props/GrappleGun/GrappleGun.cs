using UnityEngine;
using Jointure;
namespace Jointure.Samples
{
    public class GrappleGun : MonoBehaviour
    {
        [SerializeField]
        private Transform _barrelTransform, _hook, _visualHook, _visualTether, _hookIdleTransform;

        [SerializeField]
        private Rigidbody _target;

        [SerializeField]
        private GameObject _ropeGameObject;

        private Rigidbody _gunRigidbody;
        private AudioSource _audioSource;
        private ConfigurableJoint _grappleJoint;
        private Rope _rope;

        [Header("Sounds")]

        [SerializeField]
        private AudioClip[] _impacts;

        [SerializeField]
        private AudioClip[] _zippers;

        private void Awake()
        {
            _gunRigidbody = GetComponent<Rigidbody>();
            _audioSource = GetComponent<AudioSource>();
            _rope = _ropeGameObject.GetComponent<Rope>();
            _target.transform.parent = null;
        }

        public void Grapple()
        {
            if (_grappleJoint)
                return;

            RaycastHit hit;
            if (Physics.Raycast(_barrelTransform.position, _barrelTransform.forward, out hit, 100, ~0, QueryTriggerInteraction.Ignore))
            {
                _audioSource.PlayOneShot(Utilities.RandomAudioClip(_zippers));

                if (!hit.collider)
                    return;

                _grappleJoint = _gunRigidbody.gameObject.AddComponent<ConfigurableJoint>();
                _grappleJoint.autoConfigureConnectedAnchor = false;

                _hook.parent = hit.transform;
                _hook.position = hit.point;
                _hook.rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);

                //_grappleJoint.anchor = _gunRigidbody.transform.InverseTransformPoint(_barrelTransform.position);

                if (hit.rigidbody)
                {
                    _grappleJoint.connectedBody = hit.collider.attachedRigidbody;
                    _grappleJoint.connectedAnchor = hit.transform.InverseTransformPoint(_hook.TransformPoint(_visualHook.InverseTransformPoint(_visualTether.position)));
                }
                else
                {
                    _target.transform.position = _hook.TransformPoint(_visualHook.InverseTransformPoint(_visualTether.position));
                    _grappleJoint.connectedBody = _target;
                }

                SoftJointLimit softJointLimit = new SoftJointLimit();
                softJointLimit.limit = Vector3.Distance(transform.position, _hook.TransformPoint(_visualHook.InverseTransformPoint(_visualTether.position)));

                _grappleJoint.linearLimit = softJointLimit;
                _grappleJoint.xMotion = ConfigurableJointMotion.Limited;
                _grappleJoint.yMotion = ConfigurableJointMotion.Limited;
                _grappleJoint.zMotion = ConfigurableJointMotion.Limited;

                _audioSource.PlayOneShot(Utilities.RandomAudioClip(_impacts));
                _rope.SetRopeLength(Vector3.Distance(_barrelTransform.position, _hook.TransformPoint(_visualHook.InverseTransformPoint(_visualTether.position))));
                _ropeGameObject.SetActive(true);
            }
        }

        public void UnGrapple()
        {
            if (!_grappleJoint)
                return;

            Destroy(_grappleJoint);
            _audioSource.PlayOneShot(Utilities.RandomAudioClip(_zippers));
            _ropeGameObject.SetActive(false);

            _hook.parent = transform;
            _hook.position = _hookIdleTransform.position;
            _hook.rotation = _hookIdleTransform.rotation;
        }

        private void Update()
        {
            _visualHook.SetPositionAndRotation(_hook.position, _hook.rotation);
        }
    }
}