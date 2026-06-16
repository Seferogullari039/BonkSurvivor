using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DragonBossPrefabBuilder
{
    private const string BossesFolder = "Assets/Prefabs/Bosses";
    private const string DragonPrefabPath = BossesFolder + "/DragonBoss.prefab";
    private const string EnemyPrefabPath = "Assets/Enemy.prefab";

    static DragonBossPrefabBuilder()
    {
        EditorApplication.delayCall += TryRebuildMissingPrefab;
    }

    [MenuItem("Tools/BonkSurvivor/Rebuild Dragon Boss Prefab")]
    public static void RebuildDragonBossPrefab()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Bosses");

        GameObject enemyTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        if (enemyTemplate == null)
        {
            Debug.LogError("[DragonBossPrefabBuilder] Enemy prefab not found at " + EnemyPrefabPath);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(DragonPrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(DragonPrefabPath);
        }

        GameObject root = PrefabUtility.InstantiatePrefab(enemyTemplate) as GameObject;
        if (root == null)
        {
            Debug.LogError("[DragonBossPrefabBuilder] Failed to instantiate enemy template.");
            return;
        }

        root.name = "DragonBoss";
        root.tag = "Enemy";
        root.transform.localScale = Vector3.one * 3.5f;
        root.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        EnemyVisualEnhancer visualEnhancer = root.GetComponent<EnemyVisualEnhancer>();
        if (visualEnhancer != null)
        {
            Object.DestroyImmediate(visualEnhancer);
        }

        BoxCollider collider = root.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
            collider.size = new Vector3(3f, 2.5f, 5f);
            collider.center = new Vector3(0f, 1.2f, 0f);
        }

        DragonBossVisual dragonVisual = root.GetComponent<DragonBossVisual>();
        if (dragonVisual == null)
        {
            dragonVisual = root.AddComponent<DragonBossVisual>();
        }

        dragonVisual.BuildVisual();

        DragonBossController controller = root.GetComponent<DragonBossController>();
        if (controller == null)
        {
            controller = root.AddComponent<DragonBossController>();
        }

        ConfigureDragonAudioSource(root, controller);

        PrefabUtility.SaveAsPrefabAsset(root, DragonPrefabPath);
        Object.DestroyImmediate(root);

        GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DragonPrefabPath);
        if (savedPrefab == null)
        {
            Debug.LogError("[DragonBossPrefabBuilder] Failed to save " + DragonPrefabPath);
            return;
        }

        WireSavedPrefabAudio(savedPrefab);

        AssignDragonPrefabToSpawner(savedPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[DragonBossPrefabBuilder] Rebuilt " + DragonPrefabPath);
    }

    private static void AssignDragonPrefabToSpawner(GameObject dragonPrefab)
    {
        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>(FindObjectsInactive.Include);
        if (spawner == null)
        {
            Debug.LogWarning("[DragonBossPrefabBuilder] EnemySpawner not found in open scenes. Assign DragonBoss prefab manually.");
            return;
        }

        SerializedObject serializedSpawner = new SerializedObject(spawner);
        SerializedProperty dragonPrefabProperty = serializedSpawner.FindProperty("dragonBossPrefab");
        if (dragonPrefabProperty == null)
        {
            Debug.LogWarning("[DragonBossPrefabBuilder] dragonBossPrefab field not found on EnemySpawner.");
            return;
        }

        dragonPrefabProperty.objectReferenceValue = dragonPrefab;
        serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spawner);

        if (spawner.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        }
    }

    private static void ConfigureDragonAudioSource(GameObject root, DragonBossController controller)
    {
        AudioSource audioSource = root.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = root.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 12f;
        audioSource.maxDistance = 120f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty audioSourceProperty = serializedController.FindProperty("dragonAudioSource");
        if (audioSourceProperty != null)
        {
            audioSourceProperty.objectReferenceValue = audioSource;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void WireSavedPrefabAudio(GameObject prefabRoot)
    {
        DragonBossController controller = prefabRoot.GetComponent<DragonBossController>();
        if (controller == null)
        {
            return;
        }

        AudioSource audioSource = prefabRoot.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = prefabRoot.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 12f;
        audioSource.maxDistance = 120f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty audioSourceProperty = serializedController.FindProperty("dragonAudioSource");
        if (audioSourceProperty != null)
        {
            audioSourceProperty.objectReferenceValue = audioSource;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(prefabRoot);
        PrefabUtility.SavePrefabAsset(prefabRoot);
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string combined = parent + "/" + folderName;
        if (!AssetDatabase.IsValidFolder(combined))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static void TryRebuildMissingPrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (!File.Exists(DragonPrefabPath))
        {
            Debug.LogWarning("[DragonBossPrefabBuilder] Missing DragonBoss prefab. Rebuilding.");
            RebuildDragonBossPrefab();
        }
    }
}
