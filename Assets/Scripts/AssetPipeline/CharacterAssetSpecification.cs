using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Collections.Generic;

namespace MOBA.Assets
{
    /// <summary>
    /// Texture compression formats for mobile optimization
    /// </summary>
    public enum TextureCompressionFormat
    {
        ASTC_4x4,
        ASTC_6x6,
        ASTC_8x8,
        ETC2,
        DXT5,
        Uncompressed
    }
    
    /// <summary>
    /// Animation compression levels for performance
    /// </summary>
    public enum AnimationCompressionLevel
    {
        None,
        Keyframe,
        Optimal,
        Maximum
    }
    
    /// <summary>
    /// Character asset specifications for Unity-optimized MOBA characters
    /// Reference: Game Engine Architecture Chapter 6, Unity In Action Chapters 8-9
    /// Defines performance requirements and asset pipeline standards
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterAssetSpec", menuName = "MOBA/Character Asset Specification")]
    public class CharacterAssetSpecification : ScriptableObject
    {
        [Header("Model Requirements")]
        [Tooltip("Maximum vertex count for mobile-friendly performance")]
        public int maxVertexCount = 8000;
        
        [Tooltip("Maximum texture resolution for performance optimization")]
        public int maxTextureSize = 1024;
        
        [Tooltip("Animation frame rate for smooth movement")]
        public int animationFrameRate = 30;
        
        [Tooltip("LOD (Level of Detail) distances")]
        public float[] lodDistances = { 25f, 50f, 100f };
        
        [Header("Animation Requirements")]
        [Tooltip("Required animation clips for gameplay")]
        public List<string> requiredAnimations = new List<string>
        {
            "Idle", "Walk", "Run", "Attack1", "Attack2", "Attack3",
            "AbilityCast", "Death", "Respawn", "Recall", "Emote"
        };
        
        [Header("Performance Targets")]
        [Tooltip("Target frame rate for animation system")]
        public int targetFrameRate = 60;
        
        [Tooltip("Maximum draw calls per character")]
        public int maxDrawCalls = 3;
        
        [Tooltip("Memory budget per character in MB")]
        public float memoryBudgetMB = 10f;
        
        [Header("Quality Settings")]
        [Tooltip("Texture compression format")]
        public TextureCompressionFormat textureFormat = TextureCompressionFormat.ASTC_6x6;
        
        [Tooltip("Animation compression settings")]
        public AnimationCompressionLevel animationCompression = AnimationCompressionLevel.Optimal;
        
        [Tooltip("Enable mesh compression")]
        public bool enableMeshCompression = true;
        
        /// <summary>
        /// Validate if a character asset meets the specifications
        /// </summary>
        /// <param name="characterModel">Character GameObject to validate</param>
        /// <returns>Validation results with details</returns>
        public AssetValidationResult ValidateCharacterAsset(GameObject characterModel)
        {
            var result = new AssetValidationResult();
            result.assetName = characterModel.name;
            result.isValid = true;
            result.validationMessages = new List<string>();
            
            // Validate mesh vertex count
            ValidateMeshRequirements(characterModel, result);
            
            // Validate animations
            ValidateAnimationRequirements(characterModel, result);
            
            // Validate textures
            ValidateTextureRequirements(characterModel, result);
            
            // Validate LOD setup
            ValidateLODRequirements(characterModel, result);
            
            return result;
        }
        
        private void ValidateMeshRequirements(GameObject characterModel, AssetValidationResult result)
        {
            var meshRenderers = characterModel.GetComponentsInChildren<MeshRenderer>();
            int totalVertexCount = 0;
            
            foreach (var renderer in meshRenderers)
            {
                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    totalVertexCount += meshFilter.sharedMesh.vertexCount;
                }
            }
            
            if (totalVertexCount > maxVertexCount)
            {
                result.isValid = false;
                result.validationMessages.Add($"Vertex count {totalVertexCount} exceeds maximum {maxVertexCount}");
            }
            else
            {
                result.validationMessages.Add($"Vertex count: {totalVertexCount}/{maxVertexCount} ✓");
            }
        }
        
        private void ValidateAnimationRequirements(GameObject characterModel, AssetValidationResult result)
        {
            var animator = characterModel.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                result.isValid = false;
                result.validationMessages.Add("Missing Animator or Animator Controller");
                return;
            }
            
            var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (controller != null)
            {
                var availableClips = new HashSet<string>();
                foreach (var clip in controller.animationClips)
                {
                    availableClips.Add(clip.name);
                }
                
                foreach (var requiredAnim in requiredAnimations)
                {
                    if (!availableClips.Contains(requiredAnim))
                    {
                        result.isValid = false;
                        result.validationMessages.Add($"Missing required animation: {requiredAnim}");
                    }
                }
                
                result.validationMessages.Add($"Animations: {availableClips.Count} clips found");
            }
        }
        
        private void ValidateTextureRequirements(GameObject characterModel, AssetValidationResult result)
        {
            var materials = new HashSet<Material>();
            var renderers = characterModel.GetComponentsInChildren<Renderer>();
            
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        materials.Add(material);
                    }
                }
            }
            
            foreach (var material in materials)
            {
                var mainTexture = material.mainTexture as Texture2D;
                if (mainTexture != null)
                {
                    if (mainTexture.width > maxTextureSize || mainTexture.height > maxTextureSize)
                    {
                        result.isValid = false;
                        result.validationMessages.Add($"Texture {mainTexture.name} size {mainTexture.width}x{mainTexture.height} exceeds maximum {maxTextureSize}x{maxTextureSize}");
                    }
                }
            }
            
            result.validationMessages.Add($"Materials: {materials.Count} materials validated");
        }
        
        private void ValidateLODRequirements(GameObject characterModel, AssetValidationResult result)
        {
            var lodGroup = characterModel.GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                result.validationMessages.Add("Warning: No LOD Group found - consider adding for performance");
                return;
            }
            
            var lods = lodGroup.GetLODs();
            result.validationMessages.Add($"LOD Group: {lods.Length} levels configured ✓");
        }
    }
    
    /// <summary>
    /// Asset validation result structure
    /// </summary>
    [System.Serializable]
    public class AssetValidationResult
    {
        public string assetName;
        public bool isValid;
        public List<string> validationMessages;
        public float validationTime;
    }
}