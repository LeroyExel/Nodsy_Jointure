using UnityEngine;
using Jointure;

namespace Jointure.Samples
{
    public class Pistol : MonoBehaviour
    {
        [SerializeField] private ArticulationBody _pistolBody;
        [SerializeField] private ArticulationBody _slideBody;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private Transform _barrelTransform;
        [SerializeField] private Animator _pistolAnimator;
        [SerializeField] private float RecoilMultiplier;
        [SerializeField] private float PowerMultiplier;
        [SerializeField] private Rigidbody _cartridgePrefab;
        [SerializeField] private Transform _cartridgeSpawnTransform;
        [SerializeField] private Vector3 _ejectForce;
        [SerializeField] private GameObject _bulletHolePrefab; // Make an object pooling system if you want
        private enum CartridgeStatus
        {
            None,
            ToChamber,
            Chambered,
            ToEject
        }
        private CartridgeStatus _cartridgeStatus;
        private bool _bullet;
        [SerializeField] private Socket _magazineSocket;
        [SerializeField] private ParticleSystem _muzzleFlash;
        [SerializeField] Renderer bulletRenderer;
        [SerializeField] Renderer casingRenderer;
        [SerializeField] GameObject magazinePrefab;
        private bool SlideLocked;

        [Header("Sounds")]
        [SerializeField] private AudioClip[] _gunshots;
        [SerializeField] private AudioClip[] _triggerPulls;
        [SerializeField] private AudioClip[] _slidePulls;
        [SerializeField] private AudioClip[] _slidePullReleases;
        [SerializeField] private AudioClip[] _slideReleases;
        [SerializeField] private AudioClip[] _impacts;

        private bool _chamber = false;
        private bool _isEnteringChamber = false;
        private bool _slideBack = false;

        // Really bad fix for a really annoying problem.
        // The chamber and slide back order would get messed up on prefab instances, but not dragged in prefabs.
        private void FixedUpdate()
        {
            if (_chamber)
            {
                _chamber = false;
                _cartridgeStatus = _isEnteringChamber ? CartridgeStatus.Chambered : CartridgeStatus.ToEject;
                _pistolAnimator.SetBool("Chambered", true);

                if (_isEnteringChamber)
                {
                    _isEnteringChamber = false;
                    _audioSource.PlayOneShot(Utilities.RandomAudioClip(_slidePullReleases));
                }
            }
            if (_slideBack)
            {
                _slideBack = false;
                _audioSource.PlayOneShot(Utilities.RandomAudioClip(_slidePulls));

                if (_cartridgeStatus == CartridgeStatus.ToEject)
                {
                    _cartridgeStatus = CartridgeStatus.None;

                    //Eject round
                    if (casingRenderer.enabled)
                        EjectCasing();

                    bulletRenderer.enabled = false;
                    casingRenderer.enabled = false;
                }

                Magazine magazine = _magazineSocket.Attacher?.Rigidbody.GetComponent<Magazine>();

                if (!magazine)
                {
                    SetSlideLock(false);
                    return;
                }

                if (magazine.RemainingRounds > 0 || _bullet)
                {
                    SetSlideLock(false);
                }
                else
                {
                    SetSlideLock(true);
                }
            }
        }

        public void OnGrab()
        {
            if (FindAnyObjectByType<AmmoPouch>())
                FindAnyObjectByType<AmmoPouch>().MagazinePrefab = magazinePrefab;
        }

        public void Fire()
        {
            _audioSource.PlayOneShot(Utilities.RandomAudioClip(_triggerPulls));

            if (_cartridgeStatus != CartridgeStatus.Chambered || !_bullet)
                return;

            _bullet = false;
            bulletRenderer.enabled = false;

            _audioSource.PlayOneShot(Utilities.RandomAudioClip(_gunshots));
            _slideBody.AddForceAtPosition(-_barrelTransform.forward * RecoilMultiplier, _barrelTransform.position, ForceMode.Impulse);
            _muzzleFlash.Play();

            RaycastHit hit;
            if (Physics.Raycast(_barrelTransform.position, _barrelTransform.forward, out hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform)
                    return;

                //Damage hit
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(10);
                }

                //Fling hit
                if (hit.rigidbody)
                    hit.rigidbody.AddForceAtPosition(_barrelTransform.forward * PowerMultiplier, hit.point, ForceMode.Impulse);

                if (hit.articulationBody)
                    hit.articulationBody.AddForceAtPosition(_barrelTransform.forward * PowerMultiplier, hit.point, ForceMode.Impulse);

                //Add bullet hole
                GameObject bulletHole = Instantiate(_bulletHolePrefab, hit.point - _barrelTransform.forward * 0.1f, Quaternion.LookRotation(_barrelTransform.forward), hit.transform);
                bulletHole.transform.Rotate(Vector3.forward, Random.Range(0, 360));
                bulletHole.transform.GetChild(0).transform.forward = hit.normal;
                bulletHole.GetComponent<AudioSource>().PlayOneShot(Utilities.RandomAudioClip(_impacts));
                Destroy(bulletHole, 5f);
            }
        }

        public void ReleaseSlide()
        {
            if (!SlideLocked)
                return;

            SetSlideLock(false);
            TransitRound();
            _audioSource.PlayOneShot(Utilities.RandomAudioClip(_slideReleases));
        }

        public void ReleaseMagazine()
        {
            if (!_magazineSocket.Attacher)
                return;

            _magazineSocket.Detach();
        }

        public void TransitRound()
        {
            if (_cartridgeStatus != CartridgeStatus.None)
                return;

            _pistolAnimator.SetBool("Chambered", false);

            Magazine magazine = _magazineSocket.Attacher?.Rigidbody.GetComponent<Magazine>();

            if (!magazine)
                return;

            if (magazine.RemainingRounds <= 0)
                return;

            magazine.RemoveRound();

            _cartridgeStatus = CartridgeStatus.ToChamber;
            _bullet = true;
            bulletRenderer.enabled = true;
            casingRenderer.enabled = true;
        }

        public void Chamber(bool isEnteringChamber)
        {
            _chamber = true;
            _isEnteringChamber = isEnteringChamber;
        }

        public void SlideBack()
        {
            _slideBack = true;
        }

        public void SlideForward()
        {
            if (SlideLocked)
                _audioSource.PlayOneShot(Utilities.RandomAudioClip(_slidePullReleases));
            TransitRound();
        }

        private void SetSlideLock(bool isLocked)
        {
            SlideLocked = isLocked;
            ArticulationDrive drive = _slideBody.zDrive;
            drive.upperLimit = isLocked ? -0.03f : 0;
            _slideBody.zDrive = drive;
        }

        private void EjectCasing()
        {
            Rigidbody cartridgeRigidbody = Instantiate(_cartridgePrefab, _cartridgeSpawnTransform.position, _cartridgeSpawnTransform.rotation);
            if (!_bullet)
                Destroy(cartridgeRigidbody.transform.Find("Bullet").gameObject);
            _bullet = false;
            cartridgeRigidbody.linearVelocity = _pistolBody.linearVelocity;
            cartridgeRigidbody.angularVelocity = _pistolBody.angularVelocity;
            cartridgeRigidbody.AddRelativeForce(_ejectForce);
            Destroy(cartridgeRigidbody.gameObject, 5);
        }
    }
}
