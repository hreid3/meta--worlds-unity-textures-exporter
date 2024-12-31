    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using UnityEngine.SceneManagement;
    using System.Linq;
    using System.Collections.Generic;

    /**
    */
    public class MaterialProcessor : MonoBehaviour
    {
        // Add a class to track issues
        private class ProcessingIssue
        {
            public string ObjectName { get; set; }
            public string MaterialName { get; set; }
            public string Message { get; set; }
            public bool IsError { get; set; }
            public GameObject GameObject { get; set; }

            public override string ToString()
            {
                return $"{(IsError ? "Error" : "Warning")} - {ObjectName} ({MaterialName}): {Message}";
            }

            public static List<ProcessingIssue> Errors(List<ProcessingIssue> issues)
            {
                return issues.Where(i => i.IsError && i.GameObject != null).ToList();
            }
        }

        [MenuItem("Tools/Process Materials")]
        public static void ProcessMaterials()
        {
            string outputDirectory = "Assets/textures/meta-horizon";
            var issues = new List<ProcessingIssue>();


            try
            {
                EditorUtility.DisplayProgressBar("Processing Materials", "Starting material processing...", 0f);
                Debug.Log("Starting material processing...");

                // Check if directory exists and has files
                if (Directory.Exists(outputDirectory) && Directory.GetFiles(outputDirectory).Length > 0)
                {
                    Debug.Log($"Found existing files in {outputDirectory}");
                    EditorUtility.ClearProgressBar(); // Clear before showing dialog
                    bool shouldProceed = EditorUtility.DisplayDialog(
                        "Clear Output Directory",
                        $"This will clear all files in {outputDirectory}. Are you sure you want to proceed?",
                        "Yes, Clear and Process",
                        "Cancel"
                    );

                    if (!shouldProceed)
                    {
                        Debug.Log("Material processing cancelled.");
                        return;
                    }

                    EditorUtility.DisplayProgressBar("Processing Materials", "Clearing output directory...", 0.1f);
                    Debug.Log("Clearing output directory...");
                    string[] assetPaths = Directory.GetFiles(outputDirectory)
                        .Where(f => f.StartsWith("Assets/"))
                        .ToArray();

                    Debug.Log($"Deleting {assetPaths.Length} existing files...");
                    foreach (string assetPath in assetPaths)
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }
                    AssetDatabase.Refresh();
                    Debug.Log("Directory cleared successfully.");
                }

                // Ensure output directory exists
                if (!Directory.Exists(outputDirectory))
                {
                    EditorUtility.DisplayProgressBar("Processing Materials", "Creating output directory...", 0.2f);
                    Debug.Log($"Creating output directory: {outputDirectory}");
                    Directory.CreateDirectory(outputDirectory);
                    AssetDatabase.Refresh();
                }

                // Get all materials in the scene
                EditorUtility.DisplayProgressBar("Processing Materials", "Finding renderers in scene...", 0.3f);
                Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                Debug.Log($"Found {renderers.Length} renderers in the scene");

                int processedRenderers = 0;
                int totalMaterials = 0;

                // Calculate total enabled renderers for progress
                int totalEnabledRenderers = renderers.Count(r => r.enabled && r.gameObject.activeInHierarchy);

                foreach (Renderer renderer in renderers)
                {
                    // Skip if renderer or any parent is disabled
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    {
                        Debug.Log($"Skipping disabled renderer on: {renderer.gameObject.name}");
                        continue;
                    }

                    processedRenderers++;
                    float progress = 0.3f + (0.7f * processedRenderers / totalEnabledRenderers);
                    EditorUtility.DisplayProgressBar("Processing Materials",
                        $"Processing renderer {processedRenderers}/{totalEnabledRenderers}: {renderer.gameObject.name}",
                        progress);

                    Debug.Log($"Processing renderer {processedRenderers}/{renderers.Length}: {renderer.gameObject.name}");

                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material == null)
                        {
                            issues.Add(new ProcessingIssue
                            {
                                ObjectName = renderer.gameObject.name,
                                MaterialName = "null",
                                Message = "Null material reference found",
                                IsError = false,
                                GameObject = renderer.gameObject
                            });
                            continue;
                        }

                        totalMaterials++;

                        // Check if material is visible (not transparent or with zero alpha)
                        if (material.renderQueue < 3000) // Non-transparent materials
                        {
                            // Verify base texture exists
                            if (!material.HasProperty("_BaseMap") || material.GetTexture("_BaseMap") == null)
                            {
                                issues.Add(new ProcessingIssue
                                {
                                    ObjectName = renderer.gameObject.name,
                                    MaterialName = material.name,
                                    Message = "Missing base texture on visible material",
                                    IsError = true,
                                    GameObject = renderer.gameObject
                                });
                                continue; // Skip this material but continue processing others
                            }
                        }

                        try
                        {
                            ProcessMaterialTextures(material, material.name, outputDirectory, issues, renderer.gameObject.name);
                        }
                        catch (System.Exception ex)
                        {
                            issues.Add(new ProcessingIssue
                            {
                                ObjectName = renderer.gameObject.name,
                                MaterialName = material.name,
                                Message = ex.Message,
                                IsError = true,
                                GameObject = renderer.gameObject
                            });
                        }
                    }
                }

                EditorUtility.DisplayProgressBar("Processing Materials", "Finalizing...", 0.95f);
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();

                // Show summary of issues
                if (issues.Any())
                {
                    var errors = issues.Where(i => i.IsError).ToList();
                    var warnings = issues.Where(i => !i.IsError).ToList();

                    string message = "";
                    if (errors.Any())
                    {
                        message += $"Errors ({errors.Count}):\n";
                        message += string.Join("\n", errors.Take(10).Select((e, i) => $"{i + 1}. {e}"));
                        if (errors.Count > 10) message += "\n...and more";
                        message += "\n\n";
                    }
                    if (warnings.Any())
                    {
                        message += $"Warnings ({warnings.Count}):\n";
                        message += string.Join("\n", warnings.Take(10).Select((w, i) => $"{i + 1}. {w}"));
                        if (warnings.Count > 10) message += "\n...and more";
                    }

                    message += "\n\nClick 'Select Error' to highlight the first error in hierarchy.";

                    bool shouldSelect = EditorUtility.DisplayDialog(
                        $"Material Processing Completed with Issues",
                        message,
                        "Select Error",
                        errors.Any() ? "Close" : "OK"
                    );

                    if (shouldSelect && errors.Any())
                    {
                        var firstError = errors.First();
                        if (firstError.GameObject != null)
                        {
                            // Select the object in hierarchy
                            Selection.activeGameObject = firstError.GameObject;
                            // Frame the object in scene view
                            SceneView.FrameLastActiveSceneView();
                            // Ping the object in hierarchy window
                            EditorGUIUtility.PingObject(firstError.GameObject);
                        }
                    }
                }

                Debug.Log($"Material processing completed. Processed {processedRenderers} active renderers with {totalMaterials} materials. Found {issues.Count} issues.");
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"Error processing materials: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog(
                    "Error Processing Materials",
                    $"A critical error occurred while processing materials:\n{ex.Message}",
                    "OK"
                );
            }
        }

        private static void ProcessMaterialTextures(Material material, string materialName, string outputDirectory, List<ProcessingIssue> issues, string objectName)
        {
            string baseName = materialName.Split('_')[0]; // Base name for the file
            string extension = ".png"; // Assuming all textures are PNG
            bool hasBaseMap = material.HasProperty("_BaseMap");
            bool hasMetallicGlossMap = material.HasProperty("_MetallicGlossMap");
            bool hasEmissionMap = material.HasProperty("_EmissionMap");

            // Process based on naming rules
            if (materialName.Contains("Metal"))
            {
                CopyTexture(material, "_BaseMap", baseName + "_BR" + extension, outputDirectory, hasBaseMap, issues, objectName);
                CopyTexture(material, "_MetallicGlossMap", baseName + "_MEO" + extension, outputDirectory, hasMetallicGlossMap, issues, objectName);
            }
            else if (materialName.Contains("Transparent"))
            {
                CopyTexture(material, "_BaseMap", baseName + "_BR" + extension, outputDirectory, hasBaseMap, issues, objectName);
                CopyTexture(material, "_EmissionMap", baseName + "_MESA" + extension, outputDirectory, hasEmissionMap, issues, objectName);
            }
            else if (materialName.Contains("Unlit"))
            {
                CopyTexture(material, "_BaseMap", baseName + "_B" + extension, outputDirectory, hasBaseMap, issues, objectName);
            }
            else if (materialName.Contains("Blend"))
            {
                CopyTexture(material, "_BaseMap", baseName + "_BA" + extension, outputDirectory, hasBaseMap, issues, objectName);
            }
            else if (materialName.Contains("Masked"))
            {
                CopyTexture(material, "_BaseMap", baseName + "_BA" + extension, outputDirectory, hasBaseMap, issues, objectName);
            }
            else if (materialName.Contains("UIO"))
            {
                CopyTexture(material, "_BaseMap", baseName + "_BA" + extension, outputDirectory, hasBaseMap, issues, objectName);
            }
            else
            {
                // Default case for Standard Image
                CopyTexture(material, "_BaseMap", baseName + "_BR" + extension, outputDirectory, hasBaseMap, issues, objectName);
            }
        }

        private static void CopyTexture(Material material, string propertyName, string outputFileName,
            string outputDirectory, bool propertyExists, List<ProcessingIssue> issues, string objectName)
        {
            if (propertyExists)
            {
                Texture texture = material.GetTexture(propertyName);
                if (texture != null)
                {
                    string texturePath = AssetDatabase.GetAssetPath(texture);

                    // Ensure the texture is a PNG
                    if (!texturePath.EndsWith(".png"))
                    {
                        issues.Add(new ProcessingIssue
                        {
                            ObjectName = objectName,
                            MaterialName = material.name,
                            Message = $"Texture {texturePath} is not a PNG. Only PNG textures are supported.",
                            IsError = true
                        });
                        return;
                    }

                    string outputPath = Path.Combine(outputDirectory, outputFileName);
                    outputPath = outputPath.Replace('\\', '/');

                    if (!AssetDatabase.CopyAsset(texturePath, outputPath))
                    {
                        issues.Add(new ProcessingIssue
                        {
                            ObjectName = objectName,
                            MaterialName = material.name,
                            Message = $"Failed to copy texture from {texturePath} to {outputPath}",
                            IsError = false
                        });
                    }
                }
                else
                {
                    issues.Add(new ProcessingIssue
                    {
                        ObjectName = objectName,
                        MaterialName = material.name,
                        Message = $"No texture found for property: {propertyName}",
                        IsError = false
                    });
                }
            }
        }
    }
