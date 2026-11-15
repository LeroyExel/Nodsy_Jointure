using UnityEngine;

namespace Jointure
{
    public class Head : MonoBehaviour
    {
        [Header("Head Visibility")]
        [Tooltip("Hides the character head mesh in first-person VR so it doesn't clip into the camera.")]
        public bool HideHeadInFirstPerson = true;

        [SerializeField]
        private Transform _headBone;

        private Player _player;
        [SerializeField]
        private GameObject _thirdPersonCamera;

        public Transform HeadBone
        {
            get => _headBone;
            set => _headBone = value;
        }

        public GameObject ThirdPersonCamera
        {
            get => _thirdPersonCamera;
            set => _thirdPersonCamera = value;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            if (_headBone != null)
            {
                _headBone.localScale = Vector3.one;
            }
        }

        public void ResolveReferences()
        {
            if (_player == null)
            {
                _player = GetComponentInParent<Player>();
            }

            if (_headBone == null && _player != null)
            {
                // Try finding via Animator humanoid bone
                if (_player.AnimationRig != null)
                {
                    var animator = _player.AnimationRig.GetComponentInChildren<Animator>();
                    if (animator != null && animator.isHuman)
                    {
                        _headBone = animator.GetBoneTransform(HumanBodyBones.Head);
                    }
                }

                // Fallback to transform hierarchy search in Character
                if (_headBone == null && _player.AnimationRig != null && _player.AnimationRig.CharacterTransform != null)
                {
                    _headBone = _player.AnimationRig.CharacterTransform.Find("Armature/Hips/Spine/Chest/UpperChest/Neck/Head");
                }
            }

            if (_thirdPersonCamera == null && _player != null)
            {
                var tpCam = _player.transform.Find("ThirdPersonCamera");
                if (tpCam != null)
                {
                    _thirdPersonCamera = tpCam.gameObject;
                }
            }
        }

        private void Update()
        {
            if (_player == null || _player.AnimationRig == null || _player.ControllerRig == null) return;
            if (_player.AnimationRig.CharacterTransform == null || _player.ControllerRig.CameraTransform == null) return;

            _player.AnimationRig.CharacterTransform.position = _player.ControllerRig.CameraTransform.position - Vector3.up * 1.65f;
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.Cross(_player.ControllerRig.CameraTransform.right, Vector3.up));
            _player.AnimationRig.CharacterTransform.rotation = Quaternion.Lerp(_player.AnimationRig.CharacterTransform.rotation, targetRotation, Time.deltaTime * 5f);
        }

        private void LateUpdate()
        {
            UpdateHeadVisibility();
        }

        public void UpdateHeadVisibility()
        {
            if (_headBone == null || _thirdPersonCamera == null)
            {
                ResolveReferences();
            }
            if (_headBone == null) return;

            bool hide = HideHeadInFirstPerson;
            if (hide && _thirdPersonCamera != null && _thirdPersonCamera.activeInHierarchy)
            {
                hide = false;
            }

            _headBone.localScale = hide ? Vector3.zero : Vector3.one;
        }

        public void SetHeadVisible(bool visible)
        {
            HideHeadInFirstPerson = !visible;
            if (_headBone != null)
            {
                _headBone.localScale = visible ? Vector3.one : Vector3.zero;
            }
        }
    }
}

