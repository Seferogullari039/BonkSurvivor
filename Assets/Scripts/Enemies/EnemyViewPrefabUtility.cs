using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class EnemyViewPrefabUtility
{
    public const string BasicEnemyViewPath = "Assets/Prefabs/Enemies/BasicEnemy_View.prefab";
    public const string FastEnemyViewPath = "Assets/Prefabs/Enemies/FastEnemy_View.prefab";
    public const string TankEnemyViewPath = "Assets/Prefabs/Enemies/TankEnemy_View.prefab";
    public const string EliteEnemyViewPath = "Assets/Prefabs/Enemies/EliteEnemy_View.prefab";

    private const string BasicEnemyResourcePath = "Prefabs/Enemies/BasicEnemy_View";
    private const string FastEnemyResourcePath = "Prefabs/Enemies/FastEnemy_View";
    private const string TankEnemyResourcePath = "Prefabs/Enemies/TankEnemy_View";
    private const string EliteEnemyResourcePath = "Prefabs/Enemies/EliteEnemy_View";

    public static GameObject ResolveViewPrefab(Enemy.EnemyType enemyType)
    {
        string assetPath = GetAssetPathForType(enemyType);

        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

#if UNITY_EDITOR
        GameObject editorPrefab = LoadEditorPrefab(assetPath);

        if (editorPrefab != null)
        {
            return editorPrefab;
        }
#endif

        string resourcePath = GetResourcePathForType(enemyType);

        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        return Resources.Load<GameObject>(resourcePath);
    }

    public static string GetAssetPathForType(Enemy.EnemyType enemyType)
    {
        return enemyType switch
        {
            Enemy.EnemyType.Fast => FastEnemyViewPath,
            Enemy.EnemyType.Tank => TankEnemyViewPath,
            Enemy.EnemyType.Elite => EliteEnemyViewPath,
            Enemy.EnemyType.Normal => BasicEnemyViewPath,
            _ => null
        };
    }

#if UNITY_EDITOR
    public static GameObject LoadEditorPrefab(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    }
#endif

    private static string GetResourcePathForType(Enemy.EnemyType enemyType)
    {
        return enemyType switch
        {
            Enemy.EnemyType.Fast => FastEnemyResourcePath,
            Enemy.EnemyType.Tank => TankEnemyResourcePath,
            Enemy.EnemyType.Elite => EliteEnemyResourcePath,
            Enemy.EnemyType.Normal => BasicEnemyResourcePath,
            _ => null
        };
    }
}
