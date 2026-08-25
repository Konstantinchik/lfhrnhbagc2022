using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DarkTreeFPS.MCPBridge
{
    /// <summary>
    /// AI-powered система ретаргетинга анимаций между разными типами скелетов
    /// Работает в связке с Blender и Claude AI для интеллектуального маппинга костей
    /// </summary>
    public static class AnimationRetargeting
    {
        // Предустановленные маппинги скелетов
        private static readonly Dictionary<string, Dictionary<string, string>> SkeletonMappings = new Dictionary<string, Dictionary<string, string>>
        {
            // Mixamo -> Unity Mecanim
            {
                "Mixamo_to_Mecanim", new Dictionary<string, string>
                {
                    { "mixamorig:Hips", "Hips" },
                    { "mixamorig:Spine", "Spine" },
                    { "mixamorig:Spine1", "Chest" },
                    { "mixamorig:Spine2", "UpperChest" },
                    { "mixamorig:Neck", "Neck" },
                    { "mixamorig:Head", "Head" },
                    { "mixamorig:LeftShoulder", "LeftShoulder" },
                    { "mixamorig:LeftArm", "LeftUpperArm" },
                    { "mixamorig:LeftForeArm", "LeftLowerArm" },
                    { "mixamorig:LeftHand", "LeftHand" },
                    { "mixamorig:RightShoulder", "RightShoulder" },
                    { "mixamorig:RightArm", "RightUpperArm" },
                    { "mixamorig:RightForeArm", "RightLowerArm" },
                    { "mixamorig:RightHand", "RightHand" },
                    { "mixamorig:LeftUpLeg", "LeftUpperLeg" },
                    { "mixamorig:LeftLeg", "LeftLowerLeg" },
                    { "mixamorig:LeftFoot", "LeftFoot" },
                    { "mixamorig:LeftToeBase", "LeftToes" },
                    { "mixamorig:RightUpLeg", "RightUpperLeg" },
                    { "mixamorig:RightLeg", "RightLowerLeg" },
                    { "mixamorig:RightFoot", "RightFoot" },
                    { "mixamorig:RightToeBase", "RightToes" }
                }
            },
            // UE4 -> Unity Mecanim
            {
                "UE4_to_Mecanim", new Dictionary<string, string>
                {
                    { "pelvis", "Hips" },
                    { "spine_01", "Spine" },
                    { "spine_02", "Chest" },
                    { "spine_03", "UpperChest" },
                    { "neck_01", "Neck" },
                    { "head", "Head" },
                    { "clavicle_l", "LeftShoulder" },
                    { "upperarm_l", "LeftUpperArm" },
                    { "lowerarm_l", "LeftLowerArm" },
                    { "hand_l", "LeftHand" },
                    { "clavicle_r", "RightShoulder" },
                    { "upperarm_r", "RightUpperArm" },
                    { "lowerarm_r", "RightLowerArm" },
                    { "hand_r", "RightHand" },
                    { "thigh_l", "LeftUpperLeg" },
                    { "calf_l", "LeftLowerLeg" },
                    { "foot_l", "LeftFoot" },
                    { "ball_l", "LeftToes" },
                    { "thigh_r", "RightUpperLeg" },
                    { "calf_r", "RightLowerLeg" },
                    { "foot_r", "RightFoot" },
                    { "ball_r", "RightToes" }
                }
            }
        };

        public static string RetargetAnimation(Dictionary<string, object> args)
        {
            try
            {
                string sourceAnimPath = args["sourceAnimationPath"].ToString();
                string sourceSkeletonType = args["sourceSkeletonType"].ToString();
                string targetSkeletonType = args["targetSkeletonType"].ToString();
                string outputPath = args["outputPath"].ToString();

                // Загрузить исходную анимацию
                AnimationClip sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(sourceAnimPath);
                if (sourceClip == null)
                {
                    throw new Exception($"Animation clip not found: {sourceAnimPath}");
                }

                // Получить маппинг костей
                string mappingKey = $"{sourceSkeletonType}_to_{targetSkeletonType}";
                Dictionary<string, string> boneMapping = null;

                if (SkeletonMappings.ContainsKey(mappingKey))
                {
                    boneMapping = SkeletonMappings[mappingKey];
                }
                else
                {
                    // Если предустановленного маппинга нет, нужно использовать AI для создания маппинга
                    // Это требует вызова Claude API или Blender скриптов
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"No preset mapping found for {mappingKey}. AI-powered mapping not yet implemented.",
                        message = "Please implement AI bone mapping or add preset mapping to SkeletonMappings dictionary"
                    });
                }

                // Создать новый AnimationClip с ретаргетированными костями
                AnimationClip targetClip = new AnimationClip();
                targetClip.name = sourceClip.name + "_retargeted";
                targetClip.frameRate = sourceClip.frameRate;

                // Получить все кривые из исходной анимации
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceClip);

                int successCount = 0;
                int skippedCount = 0;

                foreach (EditorCurveBinding binding in bindings)
                {
                    // Попытаться найти соответствующую кость в маппинге
                    string targetBonePath = binding.path;

                    foreach (var mapping in boneMapping)
                    {
                        if (binding.path.Contains(mapping.Key))
                        {
                            targetBonePath = binding.path.Replace(mapping.Key, mapping.Value);
                            break;
                        }
                    }

                    // Копировать кривую с новым путем кости
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);

                    EditorCurveBinding targetBinding = new EditorCurveBinding
                    {
                        path = targetBonePath,
                        type = binding.type,
                        propertyName = binding.propertyName
                    };

                    AnimationUtility.SetEditorCurve(targetClip, targetBinding, curve);

                    if (targetBonePath != binding.path)
                        successCount++;
                    else
                        skippedCount++;
                }

                // Сохранить новую анимацию
                if (!outputPath.StartsWith("Assets/"))
                {
                    outputPath = "Assets/" + outputPath;
                }

                // Создать папку если не существует
                string directory = System.IO.Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
                {
                    CreateFolderRecursive(directory);
                }

                AssetDatabase.CreateAsset(targetClip, outputPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var result = new
                {
                    success = true,
                    sourceAnimation = sourceAnimPath,
                    outputAnimation = outputPath,
                    mapping = mappingKey,
                    bonesRetargeted = successCount,
                    bonesSkipped = skippedCount,
                    totalCurves = bindings.Length,
                    message = $"Animation retargeted successfully: {successCount} bones mapped, {skippedCount} kept original"
                };

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception e)
            {
                var result = new
                {
                    success = false,
                    error = e.Message,
                    stackTrace = e.StackTrace
                };

                return JsonConvert.SerializeObject(result);
            }
        }

        private static void CreateFolderRecursive(string path)
        {
            string[] folders = path.Split('/');
            string currentPath = "";

            foreach (string folder in folders)
            {
                if (string.IsNullOrEmpty(folder)) continue;

                string newPath = string.IsNullOrEmpty(currentPath) ? folder : currentPath + "/" + folder;

                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    string parentPath = string.IsNullOrEmpty(currentPath) ? folders[0] : currentPath;
                    AssetDatabase.CreateFolder(parentPath, folder);
                }

                currentPath = newPath;
            }
        }

        /// <summary>
        /// Анализирует структуру скелета и возвращает информацию о костях
        /// Полезно для AI-powered маппинга
        /// </summary>
        public static string AnalyzeSkeleton(GameObject skeletonRoot)
        {
            var boneInfo = new List<object>();
            Transform[] bones = skeletonRoot.GetComponentsInChildren<Transform>();

            foreach (Transform bone in bones)
            {
                boneInfo.Add(new
                {
                    name = bone.name,
                    path = GetBonePath(bone, skeletonRoot.transform),
                    parent = bone.parent != null ? bone.parent.name : null,
                    childCount = bone.childCount,
                    localPosition = new { x = bone.localPosition.x, y = bone.localPosition.y, z = bone.localPosition.z },
                    localRotation = new { x = bone.localRotation.x, y = bone.localRotation.y, z = bone.localRotation.z, w = bone.localRotation.w }
                });
            }

            return JsonConvert.SerializeObject(new
            {
                rootName = skeletonRoot.name,
                boneCount = bones.Length,
                bones = boneInfo
            }, Formatting.Indented);
        }

        private static string GetBonePath(Transform bone, Transform root)
        {
            string path = bone.name;
            Transform current = bone.parent;

            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        /// <summary>
        /// Добавляет пользовательский маппинг скелетов
        /// </summary>
        public static void AddCustomMapping(string mappingName, Dictionary<string, string> boneMapping)
        {
            if (!SkeletonMappings.ContainsKey(mappingName))
            {
                SkeletonMappings.Add(mappingName, boneMapping);
                Debug.Log($"[AnimationRetargeting] Added custom mapping: {mappingName}");
            }
            else
            {
                SkeletonMappings[mappingName] = boneMapping;
                Debug.Log($"[AnimationRetargeting] Updated mapping: {mappingName}");
            }
        }
    }
}
