using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Jointure
{
    [System.Serializable]
    public class CharacterBoneMapping
    {
        public string HipsName;
        public string LeftHandName;
        public string RightHandName;

        public string[] LeftThumb = new string[3];
        public string[] LeftIndex = new string[3];
        public string[] LeftMiddle = new string[3];
        public string[] LeftRing = new string[3];
        public string[] LeftLittle = new string[3];

        public string[] RightThumb = new string[3];
        public string[] RightIndex = new string[3];
        public string[] RightMiddle = new string[3];
        public string[] RightRing = new string[3];
        public string[] RightLittle = new string[3];

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(HipsName) || string.IsNullOrEmpty(LeftHandName) || string.IsNullOrEmpty(RightHandName))
                return false;

            return AreFingersComplete(LeftThumb, LeftIndex, LeftMiddle, LeftRing, LeftLittle) &&
                   AreFingersComplete(RightThumb, RightIndex, RightMiddle, RightRing, RightLittle);
        }

        private static bool AreFingersComplete(params string[][] fingers)
        {
            foreach (var finger in fingers)
            {
                if (finger == null || finger.Length < 3) return false;
                if (string.IsNullOrEmpty(finger[0]) || string.IsNullOrEmpty(finger[1]) || string.IsNullOrEmpty(finger[2])) return false;
            }
            return true;
        }

        public int CountMappedFingers(bool isLeft)
        {
            int count = 0;
            var fingers = isLeft
                ? new string[][] { LeftThumb, LeftIndex, LeftMiddle, LeftRing, LeftLittle }
                : new string[][] { RightThumb, RightIndex, RightMiddle, RightRing, RightLittle };

            foreach (var finger in fingers)
            {
                if (finger != null && finger.Length >= 3 &&
                    !string.IsNullOrEmpty(finger[0]) &&
                    !string.IsNullOrEmpty(finger[1]) &&
                    !string.IsNullOrEmpty(finger[2]))
                {
                    count++;
                }
            }
            return count;
        }

        public Transform FindTransform(Transform root, string boneName)
        {
            if (string.IsNullOrEmpty(boneName) || root == null) return null;
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t.name == boneName) return t;
            }
            return null;
        }

        public Transform[] ResolveTransforms(Transform root, string[] names)
        {
            var result = new Transform[3];
            if (names == null || names.Length < 3 || root == null) return result;
            result[0] = FindTransform(root, names[0]);
            result[1] = FindTransform(root, names[1]);
            result[2] = FindTransform(root, names[2]);
            return result;
        }
    }

    public static class CharacterImporter
    {
        private const string DefaultTemplateRigPath = "Assets/Prefabs/[Jointure] Player Rig.prefab";
        private const string DefaultPlayerControllerPath = "Assets/Animations/Player.controller";
        private const string DefaultHandPosePath = "Assets/Animations/HandPoses/Default.asset";

        /// <summary>
        /// Retrieves the Humanoid Avatar associated with a model asset or GameObject.
        /// </summary>
        public static Avatar GetModelAvatar(GameObject modelAsset)
        {
            if (modelAsset == null) return null;

            // Check if Animator already has avatar
            var animator = modelAsset.GetComponent<Animator>();
            if (animator != null && animator.avatar != null && animator.avatar.isValid)
            {
                return animator.avatar;
            }

            string assetPath = AssetDatabase.GetAssetPath(modelAsset);
            if (!string.IsNullOrEmpty(assetPath))
            {
                var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var sub in subAssets)
                {
                    if (sub is Avatar avatar && avatar.isValid)
                    {
                        return avatar;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Ensures model importer is set to Humanoid and creates avatar if needed.
        /// </summary>
        public static bool ConfigureHumanoidModel(GameObject modelAsset, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (modelAsset == null)
            {
                errorMessage = "Model asset is null.";
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(modelAsset);
            if (string.IsNullOrEmpty(assetPath))
            {
                errorMessage = "Selected object is not a project asset.";
                return false;
            }

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                errorMessage = "Asset is not a 3D model importer.";
                return false;
            }

            bool needsReimport = false;

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                needsReimport = true;
            }

            if (needsReimport)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            return true;
        }

        /// <summary>
        /// Strips finger bones from model importer Avatar mapping so Animator doesn't overwrite VR fingers at runtime.
        /// </summary>
        public static void StripFingerBonesFromAvatar(GameObject modelAsset)
        {
            if (modelAsset == null) return;
            string assetPath = AssetDatabase.GetAssetPath(modelAsset);
            if (string.IsNullOrEmpty(assetPath)) return;

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null || importer.animationType != ModelImporterAnimationType.Human) return;

            var humanDesc = importer.humanDescription;
            if (humanDesc.human == null || humanDesc.human.Length == 0) return;

            var filteredBones = new List<HumanBone>();
            bool stripped = false;

            foreach (var bone in humanDesc.human)
            {
                string hName = bone.humanName;
                if (hName.Contains("Thumb") || hName.Contains("Index") ||
                    hName.Contains("Middle") || hName.Contains("Ring") ||
                    hName.Contains("Little"))
                {
                    stripped = true;
                    continue;
                }
                filteredBones.Add(bone);
            }

            if (stripped)
            {
                humanDesc.human = filteredBones.ToArray();
                importer.humanDescription = humanDesc;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// Detects all character bones directly from the Humanoid Avatar, with heuristic fallback for unmapped fingers.
        /// </summary>
        public static CharacterBoneMapping DetectBones(GameObject characterInstance, Avatar avatar = null)
        {
            var mapping = new CharacterBoneMapping();
            if (characterInstance == null) return mapping;

            var animator = characterInstance.GetComponent<Animator>();
            bool addedTempAnimator = false;
            if (animator == null)
            {
                animator = characterInstance.AddComponent<Animator>();
                addedTempAnimator = true;
            }

            if (avatar == null)
            {
                avatar = animator.avatar;
            }
            if (avatar != null && animator.avatar != avatar)
            {
                animator.avatar = avatar;
            }

            // --- 1. GET ALL BONES DIRECTLY FROM THE HUMANOID AVATAR ---
            if (animator.avatar != null && animator.avatar.isValid)
            {
                // Body bones
                mapping.HipsName = animator.GetBoneTransform(HumanBodyBones.Hips)?.name;
                mapping.LeftHandName = animator.GetBoneTransform(HumanBodyBones.LeftHand)?.name;
                mapping.RightHandName = animator.GetBoneTransform(HumanBodyBones.RightHand)?.name;

                // Left hand fingers from Humanoid Avatar
                mapping.LeftThumb[0] = animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal)?.name;
                mapping.LeftThumb[1] = animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate)?.name;
                mapping.LeftThumb[2] = animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal)?.name;

                mapping.LeftIndex[0] = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal)?.name;
                mapping.LeftIndex[1] = animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate)?.name;
                mapping.LeftIndex[2] = animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal)?.name;

                mapping.LeftMiddle[0] = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal)?.name;
                mapping.LeftMiddle[1] = animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate)?.name;
                mapping.LeftMiddle[2] = animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal)?.name;

                mapping.LeftRing[0] = animator.GetBoneTransform(HumanBodyBones.LeftRingProximal)?.name;
                mapping.LeftRing[1] = animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate)?.name;
                mapping.LeftRing[2] = animator.GetBoneTransform(HumanBodyBones.LeftRingDistal)?.name;

                mapping.LeftLittle[0] = animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal)?.name;
                mapping.LeftLittle[1] = animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate)?.name;
                mapping.LeftLittle[2] = animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal)?.name;

                // Right hand fingers from Humanoid Avatar
                mapping.RightThumb[0] = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal)?.name;
                mapping.RightThumb[1] = animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate)?.name;
                mapping.RightThumb[2] = animator.GetBoneTransform(HumanBodyBones.RightThumbDistal)?.name;

                mapping.RightIndex[0] = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal)?.name;
                mapping.RightIndex[1] = animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate)?.name;
                mapping.RightIndex[2] = animator.GetBoneTransform(HumanBodyBones.RightIndexDistal)?.name;

                mapping.RightMiddle[0] = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal)?.name;
                mapping.RightMiddle[1] = animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate)?.name;
                mapping.RightMiddle[2] = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal)?.name;

                mapping.RightRing[0] = animator.GetBoneTransform(HumanBodyBones.RightRingProximal)?.name;
                mapping.RightRing[1] = animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate)?.name;
                mapping.RightRing[2] = animator.GetBoneTransform(HumanBodyBones.RightRingDistal)?.name;

                mapping.RightLittle[0] = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal)?.name;
                mapping.RightLittle[1] = animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate)?.name;
                mapping.RightLittle[2] = animator.GetBoneTransform(HumanBodyBones.RightLittleDistal)?.name;
            }

            // Also check ModelImporter humanDescription for any mapped bones
            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(characterInstance);
            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = AssetDatabase.GetAssetPath(characterInstance);
            }
            if (!string.IsNullOrEmpty(assetPath))
            {
                var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer != null && importer.humanDescription.human != null)
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var hb in importer.humanDescription.human)
                    {
                        dict[hb.humanName] = hb.boneName;
                    }

                    if (string.IsNullOrEmpty(mapping.HipsName) && dict.ContainsKey("Hips")) mapping.HipsName = dict["Hips"];
                    if (string.IsNullOrEmpty(mapping.LeftHandName) && dict.ContainsKey("LeftHand")) mapping.LeftHandName = dict["LeftHand"];
                    if (string.IsNullOrEmpty(mapping.RightHandName) && dict.ContainsKey("RightHand")) mapping.RightHandName = dict["RightHand"];

                    FillFingerFromDict(dict, "LeftThumb", mapping.LeftThumb);
                    FillFingerFromDict(dict, "LeftIndex", mapping.LeftIndex);
                    FillFingerFromDict(dict, "LeftMiddle", mapping.LeftMiddle);
                    FillFingerFromDict(dict, "LeftRing", mapping.LeftRing);
                    FillFingerFromDict(dict, "LeftLittle", mapping.LeftLittle);

                    FillFingerFromDict(dict, "RightThumb", mapping.RightThumb);
                    FillFingerFromDict(dict, "RightIndex", mapping.RightIndex);
                    FillFingerFromDict(dict, "RightMiddle", mapping.RightMiddle);
                    FillFingerFromDict(dict, "RightRing", mapping.RightRing);
                    FillFingerFromDict(dict, "RightLittle", mapping.RightLittle);
                }
            }

            // --- 2. FALLBACK BY NAME FOR ANY BONE STILL MISSING ---
            Transform leftHandTransform = mapping.FindTransform(characterInstance.transform, mapping.LeftHandName);
            Transform rightHandTransform = mapping.FindTransform(characterInstance.transform, mapping.RightHandName);

            if (leftHandTransform == null)
            {
                leftHandTransform = FindTransformByName(characterInstance.transform, "lefthand", "hand_l", "hand.l", "l_hand", "lhand");
                if (leftHandTransform != null) mapping.LeftHandName = leftHandTransform.name;
            }
            if (rightHandTransform == null)
            {
                rightHandTransform = FindTransformByName(characterInstance.transform, "righthand", "hand_r", "hand.r", "r_hand", "rhand");
                if (rightHandTransform != null) mapping.RightHandName = rightHandTransform.name;
            }
            if (string.IsNullOrEmpty(mapping.HipsName))
            {
                Transform hips = FindTransformByName(characterInstance.transform, "hips", "pelvis");
                if (hips != null) mapping.HipsName = hips.name;
            }

            // Fallback for fingers if not mapped in Humanoid Avatar
            if (leftHandTransform != null)
            {
                if (IsFingerMissing(mapping.LeftThumb)) mapping.LeftThumb = FindFingerBoneNames(leftHandTransform, FingerType.Thumb);
                if (IsFingerMissing(mapping.LeftIndex)) mapping.LeftIndex = FindFingerBoneNames(leftHandTransform, FingerType.Index);
                if (IsFingerMissing(mapping.LeftMiddle)) mapping.LeftMiddle = FindFingerBoneNames(leftHandTransform, FingerType.Middle);
                if (IsFingerMissing(mapping.LeftRing)) mapping.LeftRing = FindFingerBoneNames(leftHandTransform, FingerType.Ring);
                if (IsFingerMissing(mapping.LeftLittle)) mapping.LeftLittle = FindFingerBoneNames(leftHandTransform, FingerType.Little);
            }

            if (rightHandTransform != null)
            {
                if (IsFingerMissing(mapping.RightThumb)) mapping.RightThumb = FindFingerBoneNames(rightHandTransform, FingerType.Thumb);
                if (IsFingerMissing(mapping.RightIndex)) mapping.RightIndex = FindFingerBoneNames(rightHandTransform, FingerType.Index);
                if (IsFingerMissing(mapping.RightMiddle)) mapping.RightMiddle = FindFingerBoneNames(rightHandTransform, FingerType.Middle);
                if (IsFingerMissing(mapping.RightRing)) mapping.RightRing = FindFingerBoneNames(rightHandTransform, FingerType.Ring);
                if (IsFingerMissing(mapping.RightLittle)) mapping.RightLittle = FindFingerBoneNames(rightHandTransform, FingerType.Little);
            }

            if (addedTempAnimator)
            {
                UnityEngine.Object.DestroyImmediate(animator);
            }

            return mapping;
        }

        private static void FillFingerFromDict(Dictionary<string, string> dict, string prefix, string[] finger)
        {
            if (string.IsNullOrEmpty(finger[0]) && dict.ContainsKey(prefix + "Proximal")) finger[0] = dict[prefix + "Proximal"];
            if (string.IsNullOrEmpty(finger[1]) && dict.ContainsKey(prefix + "Intermediate")) finger[1] = dict[prefix + "Intermediate"];
            if (string.IsNullOrEmpty(finger[2]) && dict.ContainsKey(prefix + "Distal")) finger[2] = dict[prefix + "Distal"];
        }

        private static bool IsFingerMissing(string[] finger)
        {
            return finger == null || finger.Length < 3 ||
                   string.IsNullOrEmpty(finger[0]) || string.IsNullOrEmpty(finger[1]) || string.IsNullOrEmpty(finger[2]);
        }

        private static Transform FindTransformByName(Transform root, params string[] keywords)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                string lower = t.name.ToLower();
                foreach (var kw in keywords)
                {
                    if (lower.Contains(kw))
                        return t;
                }
            }
            return null;
        }

        public enum FingerType { Thumb, Index, Middle, Ring, Little }

        private static string[] FindFingerBoneNames(Transform hand, FingerType fingerType)
        {
            var allDescendants = hand.GetComponentsInChildren<Transform>(true);
            var matching = new List<Transform>();

            foreach (var t in allDescendants)
            {
                if (t == hand) continue;
                string lower = t.name.ToLower();

                bool isFinger = false;
                switch (fingerType)
                {
                    case FingerType.Thumb:
                        isFinger = lower.Contains("thumb") || lower.Contains("pollux");
                        break;
                    case FingerType.Index:
                        isFinger = lower.Contains("index") || lower.Contains("pointer") || lower.Contains("forefinger");
                        break;
                    case FingerType.Ring:
                        isFinger = lower.Contains("ring") || lower.Contains("annularis");
                        break;
                    case FingerType.Little:
                        isFinger = lower.Contains("little") || lower.Contains("pinky") || lower.Contains("auricularis");
                        break;
                    case FingerType.Middle:
                        isFinger = (lower.Contains("middle") || lower.Contains("medius")) &&
                                   !lower.Contains("thumb") && !lower.Contains("index") &&
                                   !lower.Contains("ring") && !lower.Contains("little") &&
                                   !lower.Contains("pinky");
                        break;
                }

                if (isFinger) matching.Add(t);
            }

            if (matching.Count == 0) return new string[3];

            Transform chainRoot = null;
            foreach (var t in matching)
            {
                if (t.parent == hand || !matching.Contains(t.parent))
                {
                    chainRoot = t;
                    break;
                }
            }

            if (chainRoot == null) chainRoot = matching[0];

            var chain = new List<Transform>();
            Transform current = chainRoot;
            while (current != null)
            {
                chain.Add(current);
                Transform next = null;
                for (int i = 0; i < current.childCount; i++)
                {
                    var child = current.GetChild(i);
                    if (matching.Contains(child))
                    {
                        next = child;
                        break;
                    }
                }
                current = next;
            }

            int startIndex = 0;
            if (chain.Count >= 4)
            {
                string secondName = chain[1].name.ToLower();
                if (secondName.Contains("root") || secondName.Contains("proximal") || chain.Count >= 5)
                {
                    startIndex = 1;
                }
            }

            var result = new string[3];
            int fill = 0;
            for (int i = startIndex; i < chain.Count && fill < 3; i++)
            {
                result[fill++] = chain[i].name;
            }

            return result;
        }

        /// <summary>
        /// Replaces or configures the character inside an existing Player Rig hierarchy (in-scene or instantiated clone).
        /// Re-binds all Animation Rigging IK constraints, SkinnedMeshRenderers, Animator, and HandAnimators.
        /// </summary>
        public static bool ReplaceCharacterInRig(
            GameObject rigInstance,
            GameObject modelAsset,
            GameObject templateRigPrefab = null,
            RuntimeAnimatorController controller = null,
            HandPose defaultHandPose = null,
            CharacterBoneMapping userMapping = null,
            Avatar customAvatar = null)
        {
            if (rigInstance == null)
            {
                Debug.LogError("[Jointure] Cannot replace character: Rig instance is null.");
                return false;
            }
            if (modelAsset == null)
            {
                Debug.LogError("[Jointure] Cannot replace character: Model asset is null.");
                return false;
            }

            Transform animationRigTransform = rigInstance.transform.Find("AnimationRig");
            if (animationRigTransform == null)
            {
                Debug.LogError("[Jointure] Rig instance does not contain 'AnimationRig' child.");
                return false;
            }

            AnimationRig animationRig = animationRigTransform.GetComponent<AnimationRig>();
            if (animationRig == null)
            {
                Debug.LogError("[Jointure] AnimationRig component missing on AnimationRig child.");
                return false;
            }

            // Ensure Humanoid
            ConfigureHumanoidModel(modelAsset, out _);
            Avatar avatar = customAvatar ?? GetModelAvatar(modelAsset);

            // Load templates & defaults
            if (templateRigPrefab == null)
            {
                templateRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultTemplateRigPath);
            }
            if (templateRigPrefab == null)
            {
                Debug.LogError($"[Jointure] Could not find default template rig at {DefaultTemplateRigPath}.");
                return false;
            }

            if (controller == null)
            {
                controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DefaultPlayerControllerPath);
            }
            if (defaultHandPose == null)
            {
                defaultHandPose = AssetDatabase.LoadAssetAtPath<HandPose>(DefaultHandPosePath);
            }

            // Find any old character objects under AnimationRig
            var oldCharacters = new List<GameObject>();
            if (animationRig.CharacterTransform != null && animationRig.CharacterTransform.parent == animationRigTransform)
            {
                oldCharacters.Add(animationRig.CharacterTransform.gameObject);
            }
            foreach (Transform child in animationRigTransform)
            {
                if (child.name == "Character" || child.name == "CharacterNew" || 
                    child.GetComponent<Animator>() != null || child.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                {
                    if (!oldCharacters.Contains(child.gameObject))
                    {
                        oldCharacters.Add(child.gameObject);
                    }
                }
            }

            // Destroy old character(s)
            foreach (var oldChar in oldCharacters)
            {
                UnityEngine.Object.DestroyImmediate(oldChar);
            }

            // Instantiate new Character under AnimationRig
            GameObject newCharacter = UnityEngine.Object.Instantiate(modelAsset, animationRigTransform);
            newCharacter.name = "Character";
            newCharacter.transform.localPosition = Vector3.zero;
            newCharacter.transform.localRotation = Quaternion.identity;
            newCharacter.transform.localScale = Vector3.one;

            // Clone fresh RigSetup from template rig to guarantee all IK objects, hints, and components exist
            Transform templateRigSetup = templateRigPrefab.transform.Find("AnimationRig/Character/RigSetup");
            if (templateRigSetup == null)
            {
                Debug.LogError("[Jointure] Template rig is missing 'AnimationRig/Character/RigSetup'.");
                return false;
            }

            GameObject rigSetup = UnityEngine.Object.Instantiate(templateRigSetup.gameObject, newCharacter.transform);
            rigSetup.name = "RigSetup";
            rigSetup.transform.localPosition = Vector3.zero;
            rigSetup.transform.localRotation = Quaternion.identity;
            rigSetup.transform.localScale = Vector3.one;

            // Remove any leftover/orphaned MultiParentConstraint on IK
            Transform ikTransform = rigSetup.transform.Find("IK");
            if (ikTransform != null)
            {
                var ikMpc = ikTransform.GetComponent<MultiParentConstraint>();
                if (ikMpc != null)
                {
                    UnityEngine.Object.DestroyImmediate(ikMpc);
                }
            }

            // Ensure updateWhenOffscreen is enabled on all SkinnedMeshRenderers so meshes don't disappear in VR
            foreach (var smr in newCharacter.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
            }

            // Configure Animator
            Animator animator = newCharacter.GetComponent<Animator>();
            if (animator == null) animator = newCharacter.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Detect bones
            CharacterBoneMapping mapping = userMapping ?? DetectBones(newCharacter, avatar);

            // Re-target RigSetup IK constraints
            // 1. Left Arm IK
            var leftArmIK = rigSetup.transform.Find("IK/LeftArmIK")?.GetComponent<TwoBoneIKConstraint>();
            if (leftArmIK != null)
            {
                leftArmIK.data.root = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                leftArmIK.data.mid = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                leftArmIK.data.tip = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                leftArmIK.data.target = rigInstance.transform.Find("PhysicsRig/LeftHand/LeftArmTarget");
                leftArmIK.data.hint = leftArmIK.transform.Find("LeftArmHint");
                leftArmIK.data.targetPositionWeight = 1f;
                leftArmIK.data.targetRotationWeight = 1f;
            }

            // 2. Right Arm IK
            var rightArmIK = rigSetup.transform.Find("IK/RightArmIK")?.GetComponent<TwoBoneIKConstraint>();
            if (rightArmIK != null)
            {
                rightArmIK.data.root = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                rightArmIK.data.mid = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                rightArmIK.data.tip = animator.GetBoneTransform(HumanBodyBones.RightHand);
                rightArmIK.data.target = rigInstance.transform.Find("PhysicsRig/RightHand/RightArmTarget");
                rightArmIK.data.hint = rightArmIK.transform.Find("RightArmHint");
                rightArmIK.data.targetPositionWeight = 1f;
                rightArmIK.data.targetRotationWeight = 1f;
            }

            // 3. Left Leg IK
            var leftLegIK = rigSetup.transform.Find("IK/LeftLegIK")?.GetComponent<TwoBoneIKConstraint>();
            if (leftLegIK != null)
            {
                leftLegIK.data.root = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                leftLegIK.data.mid = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                leftLegIK.data.tip = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                leftLegIK.data.target = animationRigTransform.Find("LeftFootAnchor/LeftLegTarget");
                leftLegIK.data.hint = leftLegIK.transform.Find("LeftLegHint");
                leftLegIK.data.targetPositionWeight = 1f;
                leftLegIK.data.targetRotationWeight = 1f;
            }

            // 4. Right Leg IK
            var rightLegIK = rigSetup.transform.Find("IK/RightLegIK")?.GetComponent<TwoBoneIKConstraint>();
            if (rightLegIK != null)
            {
                rightLegIK.data.root = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                rightLegIK.data.mid = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                rightLegIK.data.tip = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                rightLegIK.data.target = animationRigTransform.Find("RightFootAnchor/RightLegTarget");
                rightLegIK.data.hint = rightLegIK.transform.Find("RightLegHint");
                rightLegIK.data.targetPositionWeight = 1f;
                rightLegIK.data.targetRotationWeight = 1f;
            }

            // 5. Head IK
            var headIK = rigSetup.transform.Find("IK/HeadIK")?.GetComponent<MultiParentConstraint>();
            if (headIK != null)
            {
                headIK.data.constrainedObject = animator.GetBoneTransform(HumanBodyBones.Head);
                var sourceArray = headIK.data.sourceObjects;
                sourceArray.SetTransform(0, rigInstance.transform.Find("ControllerRig/CameraOffset/FloorOffset/Camera"));
                headIK.data.sourceObjects = sourceArray;
            }

            // Ensure Head.cs is present on HeadIK
            Transform headIKTransform = rigSetup.transform.Find("IK/HeadIK");
            if (headIKTransform != null)
            {
                var headComp = headIKTransform.GetComponent<Head>();
                if (headComp == null)
                {
                    headComp = headIKTransform.gameObject.AddComponent<Head>();
                }
                headComp.HeadBone = animator.GetBoneTransform(HumanBodyBones.Head);
            }

            // 6. Chest IK
            var chestIK = rigSetup.transform.Find("ChestIK")?.GetComponent<ChainIKConstraint>();
            if (chestIK != null)
            {
                chestIK.data.root = animator.GetBoneTransform(HumanBodyBones.Hips);
                chestIK.data.tip = animator.GetBoneTransform(HumanBodyBones.Neck) ?? animator.GetBoneTransform(HumanBodyBones.Chest);
                chestIK.data.target = rigSetup.transform.Find("ChestIK");
            }

            // 7. Hip IK
            Transform hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips) ?? mapping.FindTransform(newCharacter.transform, mapping.HipsName);
            Transform armature = (hipsBone != null && hipsBone.parent != null && hipsBone.parent != newCharacter.transform) ? hipsBone.parent : hipsBone;
            var hipIK = rigSetup.transform.Find("HipIK")?.GetComponent<OverrideTransform>();
            if (hipIK != null)
            {
                hipIK.data.constrainedObject = armature;
            }

            var hipsScript = rigSetup.transform.Find("HipIK")?.GetComponent<Hips>();
            if (hipsScript != null)
            {
                SerializedObject soHips = new SerializedObject(hipsScript);
                soHips.FindProperty("_hipStandTransform").objectReferenceValue = rigSetup.transform.Find("HipStanding");
                soHips.FindProperty("_hipCrouchTransform").objectReferenceValue = rigSetup.transform.Find("HipCrouching");
                soHips.ApplyModifiedProperties();
            }

            // 8. Configure RigBuilder on newCharacter
            var rigBuilder = newCharacter.GetComponent<RigBuilder>() ?? newCharacter.AddComponent<RigBuilder>();
            rigBuilder.layers.Clear();
            var rigComp = rigSetup.GetComponent<Rig>();
            if (rigComp != null)
            {
                rigBuilder.layers.Add(new RigLayer(rigComp, true));
            }
            rigBuilder.Build();

            // 9. Wire AnimationRig fields (using SerializedObject for proper persistence)
            SerializedObject soAnimRig = new SerializedObject(animationRig);
            soAnimRig.FindProperty("CharacterTransform").objectReferenceValue = newCharacter.transform;
            soAnimRig.FindProperty("ArmatureTransform").objectReferenceValue = armature;
            soAnimRig.FindProperty("HipsTransform").objectReferenceValue = hipsBone;
            soAnimRig.FindProperty("HipIKTransform").objectReferenceValue = rigSetup.transform.Find("HipIK");
            soAnimRig.ApplyModifiedProperties();

            animationRig.CharacterTransform = newCharacter.transform;
            animationRig.ArmatureTransform = armature;
            animationRig.HipsTransform = hipsBone;
            animationRig.HipIKTransform = rigSetup.transform.Find("HipIK");
            EditorUtility.SetDirty(animationRig);

            // 10. Wire HandAnimators
            // Left Hand
            Transform leftHandObj = animationRigTransform.Find("LeftHand");
            if (leftHandObj != null)
            {
                HandAnimator leftHandAnimator = leftHandObj.GetComponent<HandAnimator>();
                if (leftHandAnimator != null)
                {
                    leftHandAnimator.IsLeftHand = true;
                    if (defaultHandPose != null)
                    {
                        leftHandAnimator.DefaultHandPose = defaultHandPose;
                        leftHandAnimator.HandPose = defaultHandPose;
                    }

                    SerializedObject so = new SerializedObject(leftHandAnimator);
                    Transform leftHandBone = animator.GetBoneTransform(HumanBodyBones.LeftHand) ?? mapping.FindTransform(newCharacter.transform, mapping.LeftHandName);
                    if (leftHandBone != null)
                    {
                        so.FindProperty("_hand").objectReferenceValue = leftHandBone;
                    }
                    so.ApplyModifiedProperties();

                    leftHandAnimator.Thumb = mapping.ResolveTransforms(newCharacter.transform, mapping.LeftThumb);
                    leftHandAnimator.Index = mapping.ResolveTransforms(newCharacter.transform, mapping.LeftIndex);
                    leftHandAnimator.Middle = mapping.ResolveTransforms(newCharacter.transform, mapping.LeftMiddle);
                    leftHandAnimator.Ring = mapping.ResolveTransforms(newCharacter.transform, mapping.LeftRing);
                    leftHandAnimator.Little = mapping.ResolveTransforms(newCharacter.transform, mapping.LeftLittle);

                    EditorUtility.SetDirty(leftHandAnimator);
                }
            }

            // Right Hand
            Transform rightHandObj = animationRigTransform.Find("RightHand");
            if (rightHandObj != null)
            {
                HandAnimator rightHandAnimator = rightHandObj.GetComponent<HandAnimator>();
                if (rightHandAnimator != null)
                {
                    rightHandAnimator.IsLeftHand = false;
                    if (defaultHandPose != null)
                    {
                        rightHandAnimator.DefaultHandPose = defaultHandPose;
                        rightHandAnimator.HandPose = defaultHandPose;
                    }

                    SerializedObject so = new SerializedObject(rightHandAnimator);
                    Transform rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand) ?? mapping.FindTransform(newCharacter.transform, mapping.RightHandName);
                    if (rightHandBone != null)
                    {
                        so.FindProperty("_hand").objectReferenceValue = rightHandBone;
                    }
                    so.ApplyModifiedProperties();

                    rightHandAnimator.Thumb = mapping.ResolveTransforms(newCharacter.transform, mapping.RightThumb);
                    rightHandAnimator.Index = mapping.ResolveTransforms(newCharacter.transform, mapping.RightIndex);
                    rightHandAnimator.Middle = mapping.ResolveTransforms(newCharacter.transform, mapping.RightMiddle);
                    rightHandAnimator.Ring = mapping.ResolveTransforms(newCharacter.transform, mapping.RightRing);
                    rightHandAnimator.Little = mapping.ResolveTransforms(newCharacter.transform, mapping.RightLittle);

                    EditorUtility.SetDirty(rightHandAnimator);
                }
            }

            // 11. Auto-calibrate Hand and Foot IK targets to match new character's rest pose
            CalibrateRigTargets(rigInstance, newCharacter, templateRigPrefab);

            // Strip finger bones from model avatar now that we have captured all their names
            // so runtime Animator evaluation won't override HandAnimator
            StripFingerBonesFromAvatar(modelAsset);

            return true;
        }

        /// <summary>
        /// Automatically calculates and applies optimal target rotations for LeftArmTarget, RightArmTarget,
        /// LeftLegTarget, and RightLegTarget by comparing rest-pose bone coordinate frames with the template rig.
        /// </summary>
        public static bool CalibrateRigTargets(GameObject rigInstance, GameObject characterInstance = null, GameObject templateRigPrefab = null)
        {
            if (rigInstance == null) return false;

            if (characterInstance == null)
            {
                var charT = rigInstance.transform.Find("AnimationRig/Character");
                if (charT != null) characterInstance = charT.gameObject;
            }
            if (characterInstance == null) return false;

            if (templateRigPrefab == null)
            {
                templateRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultTemplateRigPath);
            }

            Transform leftArmTarget = rigInstance.transform.Find("PhysicsRig/LeftHand/LeftArmTarget");
            Transform rightArmTarget = rigInstance.transform.Find("PhysicsRig/RightHand/RightArmTarget");
            Transform leftLegTarget = rigInstance.transform.Find("AnimationRig/LeftFootAnchor/LeftLegTarget");
            Transform rightLegTarget = rigInstance.transform.Find("AnimationRig/RightFootAnchor/RightLegTarget");

            if (leftArmTarget == null || rightArmTarget == null || leftLegTarget == null || rightLegTarget == null)
            {
                Debug.LogWarning("[Jointure] Could not find all IK targets for calibration in rig.");
                return false;
            }

            // Find new character bones
            Animator newAnim = characterInstance.GetComponent<Animator>();
            Transform newLHand = newAnim != null ? newAnim.GetBoneTransform(HumanBodyBones.LeftHand) : null;
            Transform newRHand = newAnim != null ? newAnim.GetBoneTransform(HumanBodyBones.RightHand) : null;
            Transform newLFoot = newAnim != null ? newAnim.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
            Transform newRFoot = newAnim != null ? newAnim.GetBoneTransform(HumanBodyBones.RightFoot) : null;

            // Fallback by name search if animator humanoid query fails
            if (newLHand == null) newLHand = FindBoneByName(characterInstance.transform, "mixamorig:LeftHand", "LeftHand", "hand_l", "hand.l");
            if (newRHand == null) newRHand = FindBoneByName(characterInstance.transform, "mixamorig:RightHand", "RightHand", "hand_r", "hand.r");
            if (newLFoot == null) newLFoot = FindBoneByName(characterInstance.transform, "mixamorig:LeftFoot", "LeftFoot", "foot_l", "foot.l");
            if (newRFoot == null) newRFoot = FindBoneByName(characterInstance.transform, "mixamorig:RightFoot", "RightFoot", "foot_r", "foot.r");

            if (newLHand == null || newRHand == null || newLFoot == null || newRFoot == null)
            {
                Debug.LogWarning("[Jointure] Could not find all limbs (hands/feet) on character for calibration.");
                return false;
            }

            // Template reference rest rotations (Human.fbx baseline in Player Rig)
            // LeftHand: (359.25, 355.77, 91.14), Target: (346.10, 0, 180) -> Delta: (341.89, 0.87, 88.64)
            // RightHand: (359.25, 4.23, 268.86), Target: (346.10, 0, 180) -> Delta: (341.89, 359.13, 271.36)
            Quaternion deltaLHand = Quaternion.Euler(341.89f, 0.87f, 88.64f);
            Quaternion deltaRHand = Quaternion.Euler(341.89f, 359.13f, 271.36f);

            // Compute calibrated rotations
            Quaternion calibratedLHand = deltaLHand * newLHand.rotation;
            Quaternion calibratedRHand = deltaRHand * newRHand.rotation;
            Quaternion calibratedLFoot = newLFoot.rotation;
            Quaternion calibratedRFoot = newRFoot.rotation;

            // Set local rotations
            leftArmTarget.localRotation = calibratedLHand;
            rightArmTarget.localRotation = calibratedRHand;
            leftLegTarget.localRotation = calibratedLFoot;
            rightLegTarget.localRotation = calibratedRFoot;

            EditorUtility.SetDirty(leftArmTarget);
            EditorUtility.SetDirty(rightArmTarget);
            EditorUtility.SetDirty(leftLegTarget);
            EditorUtility.SetDirty(rightLegTarget);

            // Update or add RigCalibration component
            Transform animRigT = rigInstance.transform.Find("AnimationRig");
            if (animRigT != null)
            {
                RigCalibration rigCal = animRigT.GetComponent<RigCalibration>();
                if (rigCal == null) rigCal = animRigT.gameObject.AddComponent<RigCalibration>();

                rigCal.LeftArmTarget = leftArmTarget;
                rigCal.RightArmTarget = rightArmTarget;
                rigCal.LeftLegTarget = leftLegTarget;
                rigCal.RightLegTarget = rightLegTarget;
                rigCal.CharacterAnimator = newAnim;
                rigCal.SetBaseRotations(calibratedLHand.eulerAngles, calibratedRHand.eulerAngles, calibratedLFoot.eulerAngles, calibratedRFoot.eulerAngles);
                EditorUtility.SetDirty(rigCal);
            }

            Debug.Log($"[Jointure] Auto-calibrated rig targets for '{characterInstance.name}':\n" +
                      $"  LeftArmTarget: {calibratedLHand.eulerAngles}\n" +
                      $"  RightArmTarget: {calibratedRHand.eulerAngles}\n" +
                      $"  LeftLegTarget: {calibratedLFoot.eulerAngles}\n" +
                      $"  RightLegTarget: {calibratedRFoot.eulerAngles}");

            return true;
        }

        private static Transform FindBoneByName(Transform root, params string[] names)
        {
            if (root == null) return null;
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                foreach (var n in names)
                {
                    if (string.Equals(t.name, n, StringComparison.OrdinalIgnoreCase))
                        return t;
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a ready-to-use Player Rig using the imported Humanoid mesh.
        /// </summary>
        public static GameObject CreatePlayerRig(
            GameObject modelAsset,
            GameObject templateRigPrefab = null,
            string destinationPrefabPath = null,
            RuntimeAnimatorController controller = null,
            HandPose defaultHandPose = null,
            bool instantiateInScene = true,
            CharacterBoneMapping userMapping = null,
            Avatar customAvatar = null)
        {
            if (modelAsset == null)
            {
                Debug.LogError("[Jointure] Cannot create Player Rig: Model asset is null.");
                return null;
            }

            if (templateRigPrefab == null)
            {
                templateRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultTemplateRigPath);
            }
            if (templateRigPrefab == null)
            {
                Debug.LogError($"[Jointure] Could not find default template rig at {DefaultTemplateRigPath}.");
                return null;
            }

            // Instantiate clean clone of the template rig so all 220+ GameObjects are intact
            GameObject rigInstance = UnityEngine.Object.Instantiate(templateRigPrefab);
            rigInstance.name = $"[Jointure] Player Rig - {modelAsset.name}";

            bool success = ReplaceCharacterInRig(
                rigInstance,
                modelAsset,
                templateRigPrefab,
                controller,
                defaultHandPose,
                userMapping,
                customAvatar);

            if (!success)
            {
                UnityEngine.Object.DestroyImmediate(rigInstance);
                return null;
            }

            // Save Prefab if requested
            GameObject resultObj = rigInstance;
            if (!string.IsNullOrEmpty(destinationPrefabPath))
            {
                string dir = Path.GetDirectoryName(destinationPrefabPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rigInstance, destinationPrefabPath);
                Debug.Log($"[Jointure] Saved Player Rig prefab at: {destinationPrefabPath}");

                if (!instantiateInScene)
                {
                    UnityEngine.Object.DestroyImmediate(rigInstance);
                    resultObj = savedPrefab;
                }
                else
                {
                    resultObj = rigInstance;
                }
            }

            return resultObj;
        }
    }
}

