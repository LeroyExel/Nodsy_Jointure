using UnityEngine;

namespace Jointure.Samples
{
    public class TimeScaleSetter : MonoBehaviour
    {
        public void SetTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = 1f / 144f * timeScale;
        }
    }
}
