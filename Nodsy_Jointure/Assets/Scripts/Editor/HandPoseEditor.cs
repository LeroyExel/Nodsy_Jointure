using System.Drawing.Printing;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Jointure
{
    // Monolithic class - I'm so sorry
    public class HandPoseEditor : EditorWindow
    {
        [SerializeField]
        private GameObject _dummyHandPrefab, _mirrorPrefab;

        private Vector2 _scrollPosition = Vector2.zero;
        private GameObject _dummyHand, _mirror;
        private Transform _armature, _currentSelection;
        private bool _isLeftHand;

        HandPose _currentHandPose;
        private enum subPoses
        {
            Idle,
            TriggerTouched,
            Closed,
            ThumbrestTouched,
            PrimaryTouched,
            PrimaryButton,
            SecondaryTouched,
            SecondaryButton,
            ThumbstickTouched
        };
        subPoses _subPose = subPoses.Idle;
        Object _currentAsset;

        [MenuItem("Window/Jointure/Hand Pose Editor")]
        public static void ShowEditor()
        {
            GetWindow<HandPoseEditor>("Hand Pose Editor");
        }

        private void Update()
        {
            if (_currentSelection && _dummyHand)
            {
                Transform offset = _armature.transform.Find("Offset");
                _armature.rotation = _currentSelection.rotation * Quaternion.Inverse(offset.rotation) * _armature.rotation;
                _armature.position += _currentSelection.position - offset.position;
            }
        }

        private void SpawnDummyHand(Vector3 spawnLocation, Quaternion spawnRotation)
        {
            DestroyImmediate(_dummyHand);
            _dummyHand = Instantiate(_dummyHandPrefab, spawnLocation, spawnRotation);
            _dummyHand.name = "DummyHand";
            _armature = _dummyHand.transform.Find("Armature");
            _subPose = subPoses.Idle;
            _currentHandPose = CreateInstance<HandPose>();
            _currentHandPose = Instantiate(_currentHandPose);

            _dummyHand.transform.parent = null;
            SnapGrab grab = _currentSelection?.GetComponent<SnapGrab>();
            if (grab)
            {
                _dummyHand.transform.parent = _currentSelection;

                _isLeftHand = grab.IsLeftHanded && !grab.IsRightHanded;

                _armature.transform.localScale = _isLeftHand ? new Vector3(-1f, 1f, 1f) : new Vector3(1f, 1f, 1f);
                if (grab.HandPose)
                {
                    _currentHandPose = grab.HandPose;
                    _currentAsset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(grab.HandPose), typeof(HandPose));
                    _subPose = subPoses.ThumbstickTouched;
                    HandPoseToDummy(_currentHandPose);
                    _subPose = subPoses.Closed;
                }
            }

            HandPoseToDummy(_currentHandPose);
        }

        private void OnGUI()
        {
            minSize = new Vector2(154f, 397f);
            maxSize = minSize + new Vector2(0.1f, 0.1f);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUIStyle.none);

            //All the controls for the editor
            if (GUILayout.Button("Select"))
            {
                _currentSelection = Selection.activeTransform;

                //Insert the dummy hand into the scene 0.25m in front of the camera
                Vector3 spawnLocation = SceneView.lastActiveSceneView.camera.transform.TransformPoint(Vector3.forward * 0.25f);
                Quaternion spawnRotation = Quaternion.LookRotation(Vector3.Cross(SceneView.lastActiveSceneView.camera.transform.right, Vector3.down));
                SpawnDummyHand(spawnLocation, spawnRotation);
            }
            GUILayout.Label("File Handling");
            if (GUILayout.Button("New"))
            {
                _currentAsset = null;

                //Reset the dummy hand's pose (by deleting and creating a new one in its position)
                if (_dummyHand != null)
                {
                    Vector3 position = _dummyHand.transform.position;
                    Quaternion rotation = _dummyHand.transform.rotation;
                    DestroyImmediate(_dummyHand); //Deletes dummy hand
                    SpawnDummyHand(position, rotation); //Respawns at old location
                }
            }
            if (GUILayout.Button("Open"))
            {
                //Opens file explorer for the user to navigate to their file
                string path = EditorUtility.OpenFilePanel("Open hand pose", "", "asset");
                path = path.Replace(Application.dataPath, "Assets"); //Converts absolute path to relative path

                if (path.Length > 0)
                { //Check that path isn't empty
                    _currentAsset = AssetDatabase.LoadAssetAtPath(path, typeof(HandPose));
                    HandPose handPose = (HandPose)_currentAsset;
                    _subPose = subPoses.Idle;
                    HandPoseToDummy(handPose);
                    _currentHandPose = handPose;
                }
            }
            if (GUILayout.Button("Save"))
            {
                //Updates the hand pose
                _currentHandPose = DummyToHandPose();
                if (!_currentAsset) //If there is no file currently loaded
                {
                    SaveAs();
                }
                else
                {
                    //Overwrite existing pose
                    HandPose handPose = (HandPose)_currentAsset;
                    handPose.Thumb = _currentHandPose.Thumb;
                    handPose.Index = _currentHandPose.Index;
                    handPose.Middle = _currentHandPose.Middle;
                    handPose.Ring = _currentHandPose.Ring;
                    handPose.Little = _currentHandPose.Little;
                    EditorUtility.SetDirty(_currentAsset);
                }
            }
            if (GUILayout.Button("Save as"))
            {
                //Updates the hand pose
                _currentHandPose = DummyToHandPose();

                SaveAs();
            }
            GUILayout.Label("Misc");
            if (_isLeftHand)
            {
                if (GUILayout.Button("Swap to right hand"))
                {
                    _isLeftHand = false;
                    _armature.transform.localScale = new Vector3(1f, 1f, 1f);
                }
            }
            else
            {
                if (GUILayout.Button("Swap to left hand"))
                {
                    _isLeftHand = true;
                    _armature.transform.localScale = new Vector3(-1f, 1f, 1f);
                }
            }
            if (!_mirror)
            {
                if (GUILayout.Button("Spawn mirror"))
                {
                    Transform parent = Selection.activeTransform.parent;

                    if (Selection.activeTransform.parent)
                        parent = Selection.activeTransform.parent;

                    _mirror = Instantiate(_mirrorPrefab, parent.position, parent.rotation, parent);
                    _mirror.name = "Mirror";
                }
            }
            else
            {
                if (GUILayout.Button("Mirror grabs"))
                {
                    foreach (GameObject grab in Selection.gameObjects)
                    {
                        if (!grab.GetComponent<Grab>())
                            return;

                        GameObject mirroredGrab = Instantiate(grab, grab.transform.parent);
                        mirroredGrab.name = grab.name
                            .Replace("Left", "R I G H T")
                            .Replace("Right", "L E F T")
                            .Replace("R I G H T", "Right")
                            .Replace("L E F T", "Left");

                        foreach (Transform child in mirroredGrab.transform)
                            DestroyImmediate(child.gameObject);

                        MirrorGrab(mirroredGrab.GetComponent<Grab>());
                    }
                }
            }
            GUILayout.Label("Sub-Poses");
            if (GUILayout.Button("Open"))
            {
                _currentHandPose = DummyToHandPose();
                _subPose = subPoses.Idle;
                HandPoseToDummy(_currentHandPose);
            }
            if (GUILayout.Button("Closed"))
            {
                _currentHandPose = DummyToHandPose();
                _subPose = subPoses.Closed;
                HandPoseToDummy(_currentHandPose);
            }
            if (GUILayout.Button("Trigger touched"))
            {
                _currentHandPose = DummyToHandPose();
                _subPose = subPoses.TriggerTouched;
                HandPoseToDummy(_currentHandPose);
            }
            if (GUILayout.Button("Thumbrest touched"))
            {
                _currentHandPose = DummyToHandPose();
                _subPose = subPoses.ThumbrestTouched;
                HandPoseToDummy(_currentHandPose);
            }
            if (GUILayout.Button("Thumbstick touched"))
            {
                _currentHandPose = DummyToHandPose();
                _subPose = subPoses.ThumbstickTouched;
                HandPoseToDummy(_currentHandPose);
            }
            if (GUILayout.Button("Primary touched"))
            {
                _currentHandPose = DummyToHandPose();
                _subPose = subPoses.PrimaryTouched;
                HandPoseToDummy(_currentHandPose);
            }
            if (GUILayout.Button("Primary button"))
            {
                _currentHandPose = DummyToHandPose();
                _subPose = subPoses.PrimaryButton;
                HandPoseToDummy(_currentHandPose);
            }
            if (GUILayout.Button("Secondary touched"))
            {
                _currentHandPose = DummyToHandPose();
                _subPose = subPoses.SecondaryTouched;
                HandPoseToDummy(_currentHandPose);
            }
            if (GUILayout.Button("Secondary button"))
            {
                _currentHandPose = DummyToHandPose();
                _subPose = subPoses.SecondaryButton;
                HandPoseToDummy(_currentHandPose);
            }
            GUILayout.EndScrollView();
        }

        private void SaveAs()
        {
            //Opens the file explorer for the user to navigate to/save their file
            string path = EditorUtility.SaveFilePanel("Save hand pose", "", "MyHandPose", "asset");
            path = path.Replace(Application.dataPath, "Assets"); //Converts absolute path to relative path
            if (path.Length > 0) //Check that path isn't empty
            {
                Object asset = AssetDatabase.LoadAssetAtPath(path, typeof(HandPose));
                HandPose handPose = (HandPose)asset;
                if (!handPose) //If the hand pose does not exist yet
                {
                    //Create new pose
                    AssetDatabase.CreateAsset(_currentHandPose, path);
                }
                else
                {
                    //Overwrite existing pose
                    handPose.Thumb = _currentHandPose.Thumb;
                    handPose.Index = _currentHandPose.Index;
                    handPose.Middle = _currentHandPose.Middle;
                    handPose.Ring = _currentHandPose.Ring;
                    handPose.Little = _currentHandPose.Little;
                    EditorUtility.SetDirty(asset);
                }
                _currentAsset = AssetDatabase.LoadAssetAtPath(path, typeof(HandPose));
            }
        }

        private void HandPoseToDummy(HandPose handPose)
        {
            switch (_subPose)
            {
                case subPoses.Idle:
                    FingerPoseToDummy(handPose.Thumb.Idle, _armature.Find("Thumb"));
                    FingerPoseToDummy(handPose.Index.Open, _armature.Find("Index"));
                    FingerPoseToDummy(handPose.Middle.Open, _armature.Find("Middle"));
                    FingerPoseToDummy(handPose.Ring.Open, _armature.Find("Ring"));
                    FingerPoseToDummy(handPose.Little.Open, _armature.Find("Little"));
                    break;
                case subPoses.Closed:
                    FingerPoseToDummy(handPose.Index.Closed, _armature.Find("Index"));
                    FingerPoseToDummy(handPose.Middle.Closed, _armature.Find("Middle"));
                    FingerPoseToDummy(handPose.Ring.Closed, _armature.Find("Ring"));
                    FingerPoseToDummy(handPose.Little.Closed, _armature.Find("Little"));
                    break;
                case subPoses.TriggerTouched:
                    FingerPoseToDummy(handPose.Index.TriggerTouched, _armature.Find("Index"));
                    break;
                case subPoses.ThumbrestTouched:
                    FingerPoseToDummy(handPose.Thumb.ThumbrestTouched, _armature.Find("Thumb"));
                    break;
                case subPoses.PrimaryTouched:
                    FingerPoseToDummy(handPose.Thumb.PrimaryTouched, _armature.Find("Thumb"));
                    break;
                case subPoses.PrimaryButton:
                    FingerPoseToDummy(handPose.Thumb.PrimaryButton, _armature.Find("Thumb"));
                    break;
                case subPoses.SecondaryTouched:
                    FingerPoseToDummy(handPose.Thumb.SecondaryTouched, _armature.Find("Thumb"));
                    break;
                case subPoses.SecondaryButton:
                    FingerPoseToDummy(handPose.Thumb.SecondaryButton, _armature.Find("Thumb"));
                    break;
                case subPoses.ThumbstickTouched:
                    FingerPoseToDummy(handPose.Thumb.ThumbstickTouched, _armature.Find("Thumb"));
                    break;
            }
        }

        private void FingerPoseToDummy(FingerPose fingerPose, Transform fingerTransform)
        {
            Transform root = fingerTransform.GetChild(0);
            Transform middle = root.GetChild(0);
            Transform tip = middle.GetChild(0);

            //Sets dummy finger bone rotations to those in the fingerPose
            root.localRotation = fingerPose.RootBone;
            middle.localRotation = fingerPose.MiddleBone;
            tip.localRotation = fingerPose.TipBone;
        }

        private HandPose DummyToHandPose()
        {
            HandPose handPose = _currentHandPose;
            handPose = Instantiate(handPose);

            switch (_subPose)
            {
                case subPoses.Idle:
                    handPose.Thumb.Idle = DummyToFingerPose(_armature.Find("Thumb"));
                    handPose.Index.Open = DummyToFingerPose(_armature.Find("Index"));
                    handPose.Middle.Open = DummyToFingerPose(_armature.Find("Middle"));
                    handPose.Ring.Open = DummyToFingerPose(_armature.Find("Ring"));
                    handPose.Little.Open = DummyToFingerPose(_armature.Find("Little"));
                    break;
                case subPoses.Closed:
                    handPose.Index.Closed = DummyToFingerPose(_armature.Find("Index"));
                    handPose.Middle.Closed = DummyToFingerPose(_armature.Find("Middle"));
                    handPose.Ring.Closed = DummyToFingerPose(_armature.Find("Ring"));
                    handPose.Little.Closed = DummyToFingerPose(_armature.Find("Little"));
                    break;
                case subPoses.TriggerTouched:
                    handPose.Index.TriggerTouched = DummyToFingerPose(_armature.Find("Index"));
                    break;
                case subPoses.ThumbrestTouched:
                    handPose.Thumb.ThumbrestTouched = DummyToFingerPose(_armature.Find("Thumb"));
                    break;
                case subPoses.PrimaryTouched:
                    handPose.Thumb.PrimaryTouched = DummyToFingerPose(_armature.Find("Thumb"));
                    break;
                case subPoses.PrimaryButton:
                    handPose.Thumb.PrimaryButton = DummyToFingerPose(_armature.Find("Thumb"));
                    break;
                case subPoses.SecondaryTouched:
                    handPose.Thumb.SecondaryTouched = DummyToFingerPose(_armature.Find("Thumb"));
                    break;
                case subPoses.SecondaryButton:
                    handPose.Thumb.SecondaryButton = DummyToFingerPose(_armature.Find("Thumb"));
                    break;
                case subPoses.ThumbstickTouched:
                    handPose.Thumb.ThumbstickTouched = DummyToFingerPose(_armature.Find("Thumb"));
                    break;
            }

            return handPose;
        }

        private FingerPose DummyToFingerPose(Transform fingerTransform)
        {
            FingerPose fingerPose = new FingerPose();

            Transform root = fingerTransform.GetChild(0);
            Transform middle = root.GetChild(0);
            Transform tip = middle.GetChild(0);

            fingerPose.RootBone = root.localRotation;
            fingerPose.MiddleBone = middle.localRotation;
            fingerPose.TipBone = tip.localRotation;

            return fingerPose;
        }

        private void OnDestroy()
        {
            //Remove the dummy hand
            DestroyImmediate(_dummyHand);
            DestroyImmediate(_mirror);
            _currentAsset = null;
        }

        private void MirrorGrab(Grab snapGrab)
        {
            //Position
            Plane plane = new Plane(-_mirror.transform.right, _mirror.transform.position);
            var mirrorPoint = plane.ClosestPointOnPlane(snapGrab.transform.position);
            snapGrab.transform.position = Vector3.LerpUnclamped(snapGrab.transform.position, mirrorPoint, 2f);

            //Rotation
            Vector3 mirroredForwardVector = Vector3.Reflect(snapGrab.transform.forward, plane.normal);
            Vector3 mirroredUpVector = Vector3.Reflect(snapGrab.transform.up, plane.normal);
            snapGrab.transform.rotation = Quaternion.LookRotation(mirroredForwardVector, mirroredUpVector);

            //Flipping grab
            if (snapGrab.IsLeftHanded)
            {
                snapGrab.IsLeftHanded = false;
                snapGrab.IsRightHanded = true;
            }
            else if (snapGrab.IsRightHanded)
            {
                snapGrab.IsRightHanded = false;
                snapGrab.IsLeftHanded = true;
            }
        }
    }
}