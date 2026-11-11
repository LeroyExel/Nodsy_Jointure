using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Jointure
{
    public class CharacterImporterWindow : EditorWindow
    {
        [SerializeField] private GameObject _modelAsset;
        [SerializeField] private Avatar _customAvatar;
        [SerializeField] private GameObject _templateRig;
        [SerializeField] private RuntimeAnimatorController _animatorController;
        [SerializeField] private HandPose _handPose;
        [SerializeField] private string _outputPrefabName;
        [SerializeField] private CharacterBoneMapping _mapping = new CharacterBoneMapping();
        [SerializeField] private GameObject _tuningRig;

        private Vector2 _scrollPos;
        private bool _showBoneDetails = false;
        private bool _showLeftFingers = false;
        private bool _showRightFingers = false;
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.Info;

        [MenuItem("Tools/Jointure/Character Importer", false, 10)]
        public static void OpenWindow()
        {
            var window = GetWindow<CharacterImporterWindow>("Character Importer");
            window.minSize = new Vector2(420, 560);
            window.Show();
        }

        [MenuItem("Assets/Jointure/Setup Character for Player Rig", false, 30)]
        private static void OpenFromAsset()
        {
            var selected = Selection.activeGameObject;
            if (selected == null) return;

            var window = GetWindow<CharacterImporterWindow>("Character Importer");
            window.minSize = new Vector2(420, 560);
            window.SetModel(selected);
            window.Show();
        }

        [MenuItem("Assets/Jointure/Setup Character for Player Rig", true)]
        private static bool ValidateOpenFromAsset()
        {
            var selected = Selection.activeGameObject;
            if (selected == null) return false;
            string path = AssetDatabase.GetAssetPath(selected);
            return !string.IsNullOrEmpty(path) && (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
                                                  path.EndsWith(".blend", StringComparison.OrdinalIgnoreCase) ||
                                                  path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase));
        }

        private void OnEnable()
        {
            LoadDefaults();
            if (_modelAsset != null)
            {
                AnalyzeModel();
            }
        }

        private void LoadDefaults()
        {
            if (_templateRig == null)
                _templateRig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/[Jointure] Player Rig.prefab");

            if (_animatorController == null)
                _animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Player.controller");

            if (_handPose == null)
                _handPose = AssetDatabase.LoadAssetAtPath<HandPose>("Assets/Animations/HandPoses/Default.asset");
        }

        public void SetModel(GameObject model)
        {
            _modelAsset = model;
            _customAvatar = null;
            AnalyzeModel();
        }

        private void AnalyzeModel()
        {
            if (_modelAsset == null)
            {
                _mapping = new CharacterBoneMapping();
                _statusMessage = "Select or drag a 3D humanoid character model asset to begin.";
                _statusType = MessageType.Info;
                return;
            }

            _outputPrefabName = $"[Jointure] Player Rig - {_modelAsset.name}";

            // Ensure model importer has Humanoid animation type enabled
            CharacterImporter.ConfigureHumanoidModel(_modelAsset, out _);

            if (_customAvatar == null)
            {
                _customAvatar = CharacterImporter.GetModelAvatar(_modelAsset);
            }

            // Instantiate temporary object to query bone mapping via Avatar
            GameObject tempInstance = Instantiate(_modelAsset);
            tempInstance.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                _mapping = CharacterImporter.DetectBones(tempInstance, _customAvatar);

                int leftCount = _mapping.CountMappedFingers(true);
                int rightCount = _mapping.CountMappedFingers(false);

                if (_mapping.IsValid())
                {
                    _statusMessage = $"Avatar bones detected successfully! (10/10 fingers mapped). Ready to assemble rig.";
                    _statusType = MessageType.None;
                }
                else
                {
                    _statusMessage = $"Avatar scan complete: Left fingers: {leftCount}/5, Right fingers: {rightCount}/5.";
                    _statusType = MessageType.Warning;
                }
            }
            finally
            {
                DestroyImmediate(tempInstance);
            }
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(8);
            DrawHeader();

            EditorGUILayout.Space(8);
            DrawModelSection();

            EditorGUILayout.Space(8);
            DrawConfigSection();

            EditorGUILayout.Space(8);
            DrawBoneAnalysisSection();

            EditorGUILayout.Space(12);
            DrawActionButtons();

            EditorGUILayout.Space(12);
            DrawCalibrationSection();

            EditorGUILayout.Space(10);
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Jointure Character Importer", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Extracts bones directly from the Humanoid Avatar and configures the model into Jointure Player Rig.", EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawModelSection()
        {
            EditorGUILayout.LabelField("1. Character Model & Humanoid Avatar", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var newModel = (GameObject)EditorGUILayout.ObjectField("Character Model", _modelAsset, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                _modelAsset = newModel;
                _customAvatar = null;
                AnalyzeModel();
            }

            EditorGUI.BeginChangeCheck();
            var newAvatar = (Avatar)EditorGUILayout.ObjectField("Humanoid Avatar", _customAvatar, typeof(Avatar), false);
            if (EditorGUI.EndChangeCheck())
            {
                _customAvatar = newAvatar;
                AnalyzeModel();
            }

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }

        private void DrawConfigSection()
        {
            EditorGUILayout.LabelField("2. Configuration & Rig Settings", EditorStyles.boldLabel);

            _templateRig = (GameObject)EditorGUILayout.ObjectField("Template Rig", _templateRig, typeof(GameObject), false);
            _animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Animator Controller", _animatorController, typeof(RuntimeAnimatorController), false);
            _handPose = (HandPose)EditorGUILayout.ObjectField("Default Hand Pose", _handPose, typeof(HandPose), false);
            _outputPrefabName = EditorGUILayout.TextField("Output Prefab Name", _outputPrefabName);
        }

        private void DrawBoneAnalysisSection()
        {
            EditorGUILayout.LabelField("3. Humanoid Avatar Bone Status", EditorStyles.boldLabel);

            if (_mapping == null || string.IsNullOrEmpty(_mapping.HipsName))
            {
                if (_modelAsset != null)
                {
                    if (GUILayout.Button("Scan Humanoid Avatar Bones", GUILayout.Height(26)))
                    {
                        AnalyzeModel();
                    }
                }
                EditorGUILayout.HelpBox("No avatar bones detected yet. Click 'Scan Humanoid Avatar Bones' above.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawStatusRow("Avatar Asset", _customAvatar != null ? _customAvatar.name : "Missing", _customAvatar != null);
                DrawStatusRow("Hips Bone", !string.IsNullOrEmpty(_mapping.HipsName) ? _mapping.HipsName : "Missing", !string.IsNullOrEmpty(_mapping.HipsName));
                DrawStatusRow("Left Hand Bone", !string.IsNullOrEmpty(_mapping.LeftHandName) ? _mapping.LeftHandName : "Missing", !string.IsNullOrEmpty(_mapping.LeftHandName));
                DrawStatusRow("Right Hand Bone", !string.IsNullOrEmpty(_mapping.RightHandName) ? _mapping.RightHandName : "Missing", !string.IsNullOrEmpty(_mapping.RightHandName));

                int leftFingers = _mapping.CountMappedFingers(true);
                DrawStatusRow("Left Hand Fingers", $"{leftFingers} / 5 fingers mapped (3 joints each)", leftFingers == 5);

                int rightFingers = _mapping.CountMappedFingers(false);
                DrawStatusRow("Right Hand Fingers", $"{rightFingers} / 5 fingers mapped (3 joints each)", rightFingers == 5);

                EditorGUILayout.Space(4);
                _showBoneDetails = EditorGUILayout.Foldout(_showBoneDetails, "Inspect / Edit Mapped Bone Names", true);
                if (_showBoneDetails)
                {
                    EditorGUI.indentLevel++;
                    _mapping.HipsName = EditorGUILayout.TextField("Hips Bone", _mapping.HipsName);
                    _mapping.LeftHandName = EditorGUILayout.TextField("Left Hand Bone", _mapping.LeftHandName);
                    _mapping.RightHandName = EditorGUILayout.TextField("Right Hand Bone", _mapping.RightHandName);

                    EditorGUILayout.Space(4);
                    _showLeftFingers = EditorGUILayout.Foldout(_showLeftFingers, "Left Hand Fingers", true);
                    if (_showLeftFingers)
                    {
                        EditorGUI.indentLevel++;
                        DrawFingerFields("Thumb", _mapping.LeftThumb);
                        DrawFingerFields("Index", _mapping.LeftIndex);
                        DrawFingerFields("Middle", _mapping.LeftMiddle);
                        DrawFingerFields("Ring", _mapping.LeftRing);
                        DrawFingerFields("Little", _mapping.LeftLittle);
                        EditorGUI.indentLevel--;
                    }

                    _showRightFingers = EditorGUILayout.Foldout(_showRightFingers, "Right Hand Fingers", true);
                    if (_showRightFingers)
                    {
                        EditorGUI.indentLevel++;
                        DrawFingerFields("Thumb", _mapping.RightThumb);
                        DrawFingerFields("Index", _mapping.RightIndex);
                        DrawFingerFields("Middle", _mapping.RightMiddle);
                        DrawFingerFields("Ring", _mapping.RightRing);
                        DrawFingerFields("Little", _mapping.RightLittle);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawStatusRow(string label, string value, bool isOk)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.color = isOk ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
                EditorGUILayout.LabelField(isOk ? "✓" : "✗", GUILayout.Width(18));
                GUI.color = Color.white;

                EditorGUILayout.LabelField(label, GUILayout.Width(130));
                EditorGUILayout.LabelField(value, EditorStyles.miniLabel);
            }
        }

        private void DrawFingerFields(string fingerName, string[] bones)
        {
            if (bones == null || bones.Length < 3) return;
            EditorGUILayout.LabelField(fingerName, EditorStyles.boldLabel);
            bones[0] = EditorGUILayout.TextField("  Root", bones[0]);
            bones[1] = EditorGUILayout.TextField("  Middle", bones[1]);
            bones[2] = EditorGUILayout.TextField("  Tip", bones[2]);
        }

        private void DrawActionButtons()
        {
            GUI.enabled = _modelAsset != null && _templateRig != null;

            if (GUILayout.Button("Create New Player Rig Prefab", GUILayout.Height(34)))
            {
                CreatePrefabAction();
            }

            if (GUILayout.Button("Spawn into Active Scene", GUILayout.Height(26)))
            {
                SpawnInSceneAction();
            }

            GUI.enabled = _modelAsset != null && Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Player>() != null;
            if (GUILayout.Button("Apply to Selected Rig in Scene", GUILayout.Height(26)))
            {
                ApplyToSelectedRigAction();
            }

            GUI.enabled = true;
        }

        private void CreatePrefabAction()
        {
            string targetFolder = "Assets/Prefabs";
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            string safeName = string.IsNullOrEmpty(_outputPrefabName) ? $"[Jointure] Player Rig - {_modelAsset.name}" : _outputPrefabName;
            string destPath = $"{targetFolder}/{safeName}.prefab";

            GameObject rig = CharacterImporter.CreatePlayerRig(
                _modelAsset,
                _templateRig,
                destPath,
                _animatorController,
                _handPose,
                instantiateInScene: false,
                userMapping: _mapping,
                customAvatar: _customAvatar);

            if (rig != null)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(destPath));
                EditorUtility.DisplayDialog("Jointure Importer", $"Successfully created Player Rig prefab at:\n{destPath}", "OK");
            }
        }

        private void SpawnInSceneAction()
        {
            GameObject rig = CharacterImporter.CreatePlayerRig(
                _modelAsset,
                _templateRig,
                null,
                _animatorController,
                _handPose,
                instantiateInScene: true,
                userMapping: _mapping,
                customAvatar: _customAvatar);

            if (rig != null)
            {
                Undo.RegisterCreatedObjectUndo(rig, "Spawn Player Rig with Imported Character");
                Selection.activeGameObject = rig;
                EditorUtility.DisplayDialog("Jointure Importer", $"Successfully spawned '{rig.name}' into the active scene!", "OK");
            }
        }

        private void ApplyToSelectedRigAction()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null || selected.GetComponent<Player>() == null)
            {
                EditorUtility.DisplayDialog("Jointure Importer", "Please select a GameObject with the Player component in the hierarchy.", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(selected, "Apply Character to Player Rig");

            bool success = CharacterImporter.ReplaceCharacterInRig(
                selected,
                _modelAsset,
                _templateRig,
                _animatorController,
                _handPose,
                _mapping,
                _customAvatar);

            if (success)
            {
                EditorUtility.SetDirty(selected);
                EditorUtility.DisplayDialog("Jointure Importer", $"Successfully applied '{_modelAsset.name}' to '{selected.name}'!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Jointure Importer", $"Failed to apply '{_modelAsset.name}' to '{selected.name}'. Check console for details.", "OK");
            }
        }

        private void DrawCalibrationSection()
        {
            EditorGUILayout.LabelField("4. Rig Tuning & Rotation Calibration", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Calibrate or fine-tune hand and foot rotations on any existing Player Rig.", EditorStyles.wordWrappedMiniLabel);

                _tuningRig = (GameObject)EditorGUILayout.ObjectField("Rig to Tune / Calibrate", _tuningRig, typeof(GameObject), true);

                if (_tuningRig == null && Selection.activeGameObject != null)
                {
                    if (Selection.activeGameObject.GetComponent<Player>() != null || Selection.activeGameObject.name.Contains("Rig"))
                    {
                        _tuningRig = Selection.activeGameObject;
                    }
                }

                if (_tuningRig != null)
                {
                    Transform animRigT = _tuningRig.transform.Find("AnimationRig");
                    RigCalibration rigCal = animRigT != null ? animRigT.GetComponent<RigCalibration>() : _tuningRig.GetComponentInChildren<RigCalibration>(true);

                    if (rigCal != null)
                    {
                        EditorGUILayout.HelpBox($"RigCalibration component detected on '{rigCal.gameObject.name}'. Status: {(rigCal.IsCalibrated ? "Auto-Calibrated ✓" : "Default Base Pose")}", MessageType.Info);
                    }

                    if (GUILayout.Button("⚡ Auto-Calibrate Hand & Foot Rotations", GUILayout.Height(28)))
                    {
                        Undo.RegisterFullObjectHierarchyUndo(_tuningRig, "Auto-Calibrate Rig Targets");
                        bool ok = CharacterImporter.CalibrateRigTargets(_tuningRig);
                        if (ok)
                        {
                            EditorUtility.SetDirty(_tuningRig);
                            EditorUtility.DisplayDialog("Jointure Importer", $"Successfully auto-calibrated targets on '{_tuningRig.name}'!", "OK");
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Jointure Importer", $"Could not calibrate '{_tuningRig.name}'. Ensure it contains AnimationRig/Character with an Animator.", "OK");
                        }
                    }

                    if (rigCal != null)
                    {
                        if (GUILayout.Button("Select Rig in Inspector for Live Sliders & Tweaking", GUILayout.Height(24)))
                        {
                            Selection.activeGameObject = rigCal.gameObject;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Select or drag a Player Rig (in scene or from Assets) to calibrate.", MessageType.None);
                }
            }
        }
    }
}
