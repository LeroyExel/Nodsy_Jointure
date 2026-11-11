using System.Collections.Generic;
using UnityEngine;

namespace Jointure
{
    public class AmmoPouch : Grab
    {
        public GameObject MagazinePrefab;
        private List<GameObject> _spawnedMagazines = new List<GameObject>();

        public override void OnGrab(Hand hand) //Triggered when player grabs the grab
        {
            if (MagazinePrefab == null)
                return;

            OnRelease(hand, true);
            GameObject magazine = Instantiate(MagazinePrefab);
            magazine.transform.SetPositionAndRotation(hand.PhysicsHandTransform.position, hand.PhysicsHandTransform.rotation);

            foreach (Grab grab in magazine.GetComponentsInChildren<SnapGrab>())
                if (grab.IsLeftHanded && hand.IsLeftHand || grab.IsRightHanded && !hand.IsLeftHand)
                {
                    grab.OnGrab(hand);
                    break;
                }

            _spawnedMagazines.Add(magazine);
            if (_spawnedMagazines.Count > 5)
                foreach (GameObject spawnedMagazine in _spawnedMagazines)
                    if (!spawnedMagazine.GetComponentInChildren<Attacher>()?.Socket)
                    {
                        _spawnedMagazines.Remove(spawnedMagazine);
                        Destroy(spawnedMagazine);
                        break;
                    }
        }
    }
}
