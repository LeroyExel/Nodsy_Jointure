using UnityEngine;

namespace Jointure
{
    public class SpawnPointManager : MonoBehaviour
    {
        public SpawnPoint SpawnPoint;
        public GameObject PlayerInstance;

        [SerializeField]
        private Vector3 _startGravity = new Vector3(0, -9.81f, 0);

        [SerializeField]
        private float _startTimeScale = 1f;

        [Tooltip("Inserts this prefab in front of player on respawn")]
        public GameObject StarterProp;

        public Vector3 StarterPropOffset = new Vector3(0, 1f, 1f);

        private GameObject _starterPropInstance;

        private void Awake()
        {
            SetGravity(_startGravity);
            SetTimeScale(_startTimeScale);

            if (!SpawnPoint)
            {
                SpawnPoint = FindFirstObjectByType<SpawnPoint>();
                if (!SpawnPoint)
                {
                    Debug.LogError("You must have at least one spawn point!");
                    return;
                }
            }

            Respawn();
        }

        public void SetGravity(Vector3 gravity)
        {
            Physics.gravity = gravity;
        }

        public void SetGravityX(float x)
        {
            Vector3 gravity = Physics.gravity;
            gravity.x = x;
            Physics.gravity = gravity;
        }

        public void SetGravityY(float y)
        {
            Vector3 gravity = Physics.gravity;
            gravity.y = y;
            Physics.gravity = gravity;
        }

        public void SetGravityZ(float z)
        {
            Vector3 gravity = Physics.gravity;
            gravity.z = z;
            Physics.gravity = gravity;
        }

        public void SetStarterPropOffsetX(float x)
        {
            StarterPropOffset.x = x;
        }

        public void SetStarterPropOffsetY(float y)
        {
            StarterPropOffset.y = y;
        }

        public void SetStarterPropOffsetZ(float z)
        {
            StarterPropOffset.z = z;
        }

        public void SetTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = 1f / 144f * timeScale;
        }

        public void SetSpawnPoint(SpawnPoint spawnPoint)
        {
            SpawnPoint = spawnPoint;
        }

        public void SetStarterProp(GameObject starterProp)
        {
            StarterProp = starterProp;
        }

        public void Respawn()
        {
            if (PlayerInstance)
                Destroy(PlayerInstance);

            PlayerInstance = SpawnPoint.Spawn();

            Destroy(_starterPropInstance);

            if (StarterProp)
                _starterPropInstance = Instantiate(StarterProp, SpawnPoint.transform.TransformPoint(StarterPropOffset), SpawnPoint.transform.rotation);
        }
    }
}