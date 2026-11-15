using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Jointure
{
    public class VRInputModule : BaseInputModule
    {
        public Transform CurrentControllerTransform;
        public PointerEventData Data;

        [SerializeField]
        private Camera _eventCamera;
        
        [SerializeField] private Transform
            _leftControllerTransform,
            _rightControllerTransform,
            _pointerOffsetTransform;

        [SerializeField]
        private InputActionReference _leftTriggerAction, _rightTriggerAction;

        [SerializeField]
        private float _maxLength = 5f; //The maximum length of the ray that will look for UI elements

        private int _menuLayer;

        protected override void Awake()
        {
            base.Awake();
            Data = new PointerEventData(eventSystem);
            Data.position = new Vector2(_eventCamera.pixelWidth / 2, _eventCamera.pixelHeight / 2);

            _leftTriggerAction.action.Enable(); //Enables the input action
            _leftTriggerAction.action.performed += LeftControllerPress;
            _leftTriggerAction.action.canceled += Release; //Links the release of the trigger to the Release method

            _rightTriggerAction.action.Enable(); //Enables the input action
            _rightTriggerAction.action.performed += RightControllerPress;
            _rightTriggerAction.action.canceled += Release; //Links the release of the trigger to the Release method

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.InstanceID);
            foreach (Canvas canvas in canvases)
            {
                canvas.worldCamera = _eventCamera;
            }

            _menuLayer = LayerMask.NameToLayer("JointureMenu");
            CurrentControllerTransform = _rightControllerTransform;
        }

        public void LeftControllerPress(InputAction.CallbackContext context)
        {
            if (CurrentControllerTransform != _leftControllerTransform)
            {
                CurrentControllerTransform = _leftControllerTransform;
                if (_pointerOffsetTransform)
                    _pointerOffsetTransform.localScale = new Vector3(-1, 1, 1);
            }
            else
                Press();
        }

        public void RightControllerPress(InputAction.CallbackContext context)
        {
            if (CurrentControllerTransform != _rightControllerTransform)
            {
                CurrentControllerTransform = _rightControllerTransform;
                if (_pointerOffsetTransform)
                    _pointerOffsetTransform.localScale = new Vector3(1, 1, 1);
            }
            else
                Press();
        }

        public override void Process()
        {
            //UI raycast
            eventSystem.RaycastAll(Data, m_RaycastResultCache);
            Data.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);

            //Physics raycast
            RaycastHit hit; //Unassigned raycast hit
            Ray ray = new Ray(_eventCamera.transform.position, _eventCamera.transform.forward); //Create a new ray
            bool raycast = Physics.Raycast(ray, out hit, _maxLength); //Raycast with the ray to get out the hit

            if (!raycast)
                hit.distance = _maxLength;

            if (hit.distance < Data.pointerCurrentRaycast.distance && Data.pointerCurrentRaycast.gameObject.layer != _menuLayer)
            {
                Data.pointerCurrentRaycast = new RaycastResult();
                Release(new InputAction.CallbackContext());
                return;
            }

            //Hover
            HandlePointerExitAndEnter(Data, Data.pointerCurrentRaycast.gameObject);
            ExecuteEvents.Execute(Data.pointerDrag, Data, ExecuteEvents.dragHandler);
        }

        public void Press()
        {
            //Set raycast
            Data.pointerPressRaycast = Data.pointerCurrentRaycast;

            //Check for object hit, get the down handler, call
            Data.pointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(Data.pointerPressRaycast.gameObject);
            Data.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(Data.pointerPressRaycast.gameObject);

            //Set Data
            ExecuteEvents.Execute(Data.pointerPress, Data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(Data.pointerDrag, Data, ExecuteEvents.beginDragHandler);
        }

        public void Release(InputAction.CallbackContext context)
        {
            //Check for click handler
            GameObject pointerRelease = ExecuteEvents.GetEventHandler<IPointerClickHandler>(Data.pointerCurrentRaycast.gameObject);

            //Check if actual
            if (Data.pointerPress == pointerRelease)
                ExecuteEvents.Execute(Data.pointerPress, Data, ExecuteEvents.pointerClickHandler);

            //Execute pointer up
            ExecuteEvents.Execute(Data.pointerPress, Data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(Data.pointerDrag, Data, ExecuteEvents.endDragHandler);

            //Clear selected gameobject
            Data.pointerPress = null;
            Data.pointerDrag = null;

            Data.pointerCurrentRaycast.Clear();
        }
    }
}