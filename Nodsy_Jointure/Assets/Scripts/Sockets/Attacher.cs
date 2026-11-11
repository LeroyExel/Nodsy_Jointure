using UnityEngine;

namespace Jointure
{
    [AddComponentMenu("Jointure/Attacher")]
    public class Attacher : MonoBehaviour
    {
        public string[] Tags;

        public Grab[] EnableGrabs, DisableGrabs;

        [HideInInspector]
        public Rigidbody Rigidbody;

        [HideInInspector]
        public Socket Socket;

        private void Awake()
        {
            Rigidbody = GetComponentInParent<Rigidbody>();
        }

        public bool IsGrabbed()
        {
            foreach (Grab grab in Rigidbody.GetComponentsInChildren<Grab>())
                if (grab.LeftHand || grab.RightHand)
                    return true;

            return false;
        }

        public void AttemptDetach()
        {
            if (!Socket)
                return;

            Socket.Detach();
        }
    }
}
