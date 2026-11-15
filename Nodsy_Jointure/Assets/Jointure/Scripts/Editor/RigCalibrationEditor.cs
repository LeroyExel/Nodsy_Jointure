using UnityEditor;
using UnityEngine;

namespace Jointure
{
    [CustomEditor(typeof(RigCalibration))]
    public class RigCalibrationEditor : Editor
    {
        private bool _showReferences = false;
        private bool _showHands = true;
        private bool _showFeet = true;
        private bool _showBaseValues = false;

        public override void OnInspectorGUI()
        {
            var rigCal = (RigCalibration)target;
            serializedObject.Update();

            EditorGUILayout.Space(4);
            DrawHeader(rigCal);

            EditorGUILayout.Space(6);
            DrawActions(rigCal);

            EditorGUILayout.Space(6);
            DrawHandGroup(rigCal);

            EditorGUILayout.Space(6);
            DrawFootGroup(rigCal);

            EditorGUILayout.Space(6);
            DrawReferencesGroup(rigCal);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(RigCalibration rigCal)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Player Rig Rotation Tuning", EditorStyles.boldLabel);
                    GUI.color = rigCal.IsCalibrated ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.8f, 0.2f);
                    EditorGUILayout.LabelField(rigCal.IsCalibrated ? "✓ Auto-Calibrated" : "⚠ Base Pose", EditorStyles.miniBoldLabel, GUILayout.Width(110));
                    GUI.color = Color.white;
                }
                EditorGUILayout.LabelField("Live interactive tuning for hand and foot IK targets. Updates in real-time in Edit and Play modes.", EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawActions(RigCalibration rigCal)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Calibration Actions", EditorStyles.boldLabel);

                if (GUILayout.Button("⚡ Auto-Calibrate From Character Model", GUILayout.Height(30)))
                {
                    Undo.RecordObject(rigCal, "Auto-Calibrate Rig Targets");
                    CharacterImporter.CalibrateRigTargets(rigCal.gameObject);
                    EditorUtility.SetDirty(rigCal);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Bake Offsets to Base"))
                    {
                        Undo.RecordObject(rigCal, "Bake Offsets");
                        rigCal.BakeOffsets();
                        EditorUtility.SetDirty(rigCal);
                    }

                    if (GUILayout.Button("Reset Offsets to 0"))
                    {
                        Undo.RecordObject(rigCal, "Reset Offsets");
                        rigCal.ResetOffsets();
                        EditorUtility.SetDirty(rigCal);
                    }
                }

                // Check if prefab instance
                if (PrefabUtility.IsPartOfPrefabInstance(rigCal.gameObject))
                {
                    if (GUILayout.Button("💾 Save Changes to Prefab Asset", GUILayout.Height(24)))
                    {
                        PrefabUtility.ApplyPrefabInstance(rigCal.gameObject, InteractionMode.UserAction);
                        Debug.Log("[Jointure] Saved RigCalibration modifications to Prefab Asset.");
                    }
                }
            }
        }

        private void DrawHandGroup(RigCalibration rigCal)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showHands = EditorGUILayout.Foldout(_showHands, "🖐 Hand Rotations", true, EditorStyles.foldoutHeader);
                if (_showHands)
                {
                    EditorGUILayout.Space(2);

                    // Left Hand
                    EditorGUILayout.LabelField("Left Hand Offset", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    Vector3 newLeftOffset = EditorGUILayout.Vector3Field("Offset (Euler)", rigCal.LeftHandOffset);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(rigCal, "Change Left Hand Offset");
                        rigCal.LeftHandOffset = newLeftOffset;
                        rigCal.ApplyRotations();
                        EditorUtility.SetDirty(rigCal);
                    }

                    DrawNudgeButtons(rigCal, () => rigCal.LeftHandOffset, (val) => rigCal.LeftHandOffset = val);

                    EditorGUILayout.Space(4);

                    // Right Hand
                    EditorGUILayout.LabelField("Right Hand Offset", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    Vector3 newRightOffset = EditorGUILayout.Vector3Field("Offset (Euler)", rigCal.RightHandOffset);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(rigCal, "Change Right Hand Offset");
                        rigCal.RightHandOffset = newRightOffset;
                        rigCal.ApplyRotations();
                        EditorUtility.SetDirty(rigCal);
                    }

                    DrawNudgeButtons(rigCal, () => rigCal.RightHandOffset, (val) => rigCal.RightHandOffset = val);

                    EditorGUILayout.Space(4);
                    _showBaseValues = EditorGUILayout.Foldout(_showBaseValues, "Show Base Hand Rotations", true);
                    if (_showBaseValues)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginChangeCheck();
                        Vector3 bL = EditorGUILayout.Vector3Field("Base Left Hand", rigCal.BaseLeftHandEuler);
                        Vector3 bR = EditorGUILayout.Vector3Field("Base Right Hand", rigCal.BaseRightHandEuler);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(rigCal, "Change Base Hand Rotations");
                            rigCal.BaseLeftHandEuler = bL;
                            rigCal.BaseRightHandEuler = bR;
                            rigCal.ApplyRotations();
                            EditorUtility.SetDirty(rigCal);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        private void DrawFootGroup(RigCalibration rigCal)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showFeet = EditorGUILayout.Foldout(_showFeet, "🦶 Foot Rotations", true, EditorStyles.foldoutHeader);
                if (_showFeet)
                {
                    EditorGUILayout.Space(2);

                    // Left Foot
                    EditorGUILayout.LabelField("Left Foot Offset", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    Vector3 newLeftFootOffset = EditorGUILayout.Vector3Field("Offset (Euler)", rigCal.LeftFootOffset);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(rigCal, "Change Left Foot Offset");
                        rigCal.LeftFootOffset = newLeftFootOffset;
                        rigCal.ApplyRotations();
                        EditorUtility.SetDirty(rigCal);
                    }

                    DrawNudgeButtons(rigCal, () => rigCal.LeftFootOffset, (val) => rigCal.LeftFootOffset = val);

                    EditorGUILayout.Space(4);

                    // Right Foot
                    EditorGUILayout.LabelField("Right Foot Offset", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    Vector3 newRightFootOffset = EditorGUILayout.Vector3Field("Offset (Euler)", rigCal.RightFootOffset);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(rigCal, "Change Right Foot Offset");
                        rigCal.RightFootOffset = newRightFootOffset;
                        rigCal.ApplyRotations();
                        EditorUtility.SetDirty(rigCal);
                    }

                    DrawNudgeButtons(rigCal, () => rigCal.RightFootOffset, (val) => rigCal.RightFootOffset = val);

                    EditorGUILayout.Space(4);
                    if (_showBaseValues)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginChangeCheck();
                        Vector3 bLF = EditorGUILayout.Vector3Field("Base Left Foot", rigCal.BaseLeftFootEuler);
                        Vector3 bRF = EditorGUILayout.Vector3Field("Base Right Foot", rigCal.BaseRightFootEuler);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(rigCal, "Change Base Foot Rotations");
                            rigCal.BaseLeftFootEuler = bLF;
                            rigCal.BaseRightFootEuler = bRF;
                            rigCal.ApplyRotations();
                            EditorUtility.SetDirty(rigCal);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        private void DrawNudgeButtons(RigCalibration rigCal, System.Func<Vector3> getVal, System.Action<Vector3> setVal)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+90° Yaw", EditorStyles.miniButtonLeft))
                {
                    Undo.RecordObject(rigCal, "Nudge Offset");
                    Vector3 v = getVal();
                    v.y = (v.y + 90f) % 360f;
                    setVal(v);
                    rigCal.ApplyRotations();
                    EditorUtility.SetDirty(rigCal);
                }
                if (GUILayout.Button("-90° Yaw", EditorStyles.miniButtonMid))
                {
                    Undo.RecordObject(rigCal, "Nudge Offset");
                    Vector3 v = getVal();
                    v.y = (v.y - 90f + 360f) % 360f;
                    setVal(v);
                    rigCal.ApplyRotations();
                    EditorUtility.SetDirty(rigCal);
                }
                if (GUILayout.Button("+90° Roll", EditorStyles.miniButtonMid))
                {
                    Undo.RecordObject(rigCal, "Nudge Offset");
                    Vector3 v = getVal();
                    v.z = (v.z + 90f) % 360f;
                    setVal(v);
                    rigCal.ApplyRotations();
                    EditorUtility.SetDirty(rigCal);
                }
                if (GUILayout.Button("-90° Roll", EditorStyles.miniButtonMid))
                {
                    Undo.RecordObject(rigCal, "Nudge Offset");
                    Vector3 v = getVal();
                    v.z = (v.z - 90f + 360f) % 360f;
                    setVal(v);
                    rigCal.ApplyRotations();
                    EditorUtility.SetDirty(rigCal);
                }
                if (GUILayout.Button("+15° Pitch", EditorStyles.miniButtonMid))
                {
                    Undo.RecordObject(rigCal, "Nudge Offset");
                    Vector3 v = getVal();
                    v.x = (v.x + 15f) % 360f;
                    setVal(v);
                    rigCal.ApplyRotations();
                    EditorUtility.SetDirty(rigCal);
                }
                if (GUILayout.Button("-15° Pitch", EditorStyles.miniButtonRight))
                {
                    Undo.RecordObject(rigCal, "Nudge Offset");
                    Vector3 v = getVal();
                    v.x = (v.x - 15f + 360f) % 360f;
                    setVal(v);
                    rigCal.ApplyRotations();
                    EditorUtility.SetDirty(rigCal);
                }
            }
        }

        private void DrawReferencesGroup(RigCalibration rigCal)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showReferences = EditorGUILayout.Foldout(_showReferences, "Target References", true);
                if (_showReferences)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("LeftArmTarget"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("RightArmTarget"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("LeftLegTarget"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("RightLegTarget"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("CharacterAnimator"));

                    if (GUILayout.Button("Find Target References Automatically"))
                    {
                        Undo.RecordObject(rigCal, "Auto-Find References");
                        rigCal.FindReferences();
                        EditorUtility.SetDirty(rigCal);
                    }
                }
            }
        }
    }
}
