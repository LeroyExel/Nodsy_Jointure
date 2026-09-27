using UnityEngine;
using UnityEngine.EventSystems;

namespace Jointure
{
    public class Pointer : MonoBehaviour
    {
        [SerializeField]
        private GameObject _point; //The point on the UI which will signify where the user is hovering

        [SerializeField]
        private Transform _pointerOffset; //The offset transform from which the pointer is from the controller

        [SerializeField]
        private LineRenderer _lineRenderer; //The line that will show the trajectory of the raycast

        [SerializeField]
        private VRInputModule _inputModule; //The input module within the EventSystem

        private void Update() //Procedure called each frame
        {
            if (_inputModule == null || _inputModule.CurrentControllerTransform == null || _inputModule.Data == null)
                return;

            //Move to the start of the line to the current controller
            if (_pointerOffset != null)
            {
                _pointerOffset.SetPositionAndRotation(
                    _inputModule.CurrentControllerTransform.position,
                    _inputModule.CurrentControllerTransform.rotation);
            }

            //Draw the line
            PointerEventData data = _inputModule.Data;

            if (_lineRenderer != null)
                _lineRenderer.enabled = data.pointerCurrentRaycast.gameObject != null;

            if (_point != null)
                _point.SetActive(data.pointerCurrentRaycast.gameObject != null);

            Vector3 endPosition = data.pointerCurrentRaycast.worldPosition; //Set the default end position

            if (_point != null)
                _point.transform.position = endPosition; //Put the point at the end of the line

            if (_lineRenderer != null)
            {
                _lineRenderer.SetPosition(0, Vector3.zero); //Set the start of the line to the controller
                _lineRenderer.SetPosition(1, transform.InverseTransformPoint(endPosition)); //Set the end of the line to the end position determined earlier
            }
        }
    }
}