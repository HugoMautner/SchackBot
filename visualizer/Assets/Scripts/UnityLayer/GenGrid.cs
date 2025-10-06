using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class GenGrid : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GameObject lightTilePrefab;
    [SerializeField] GameObject darkTilePrefab;

    const int N = 8;
    Transform tilesRoot;

    void OnEnable()
    {
        EnsureTilesRoot();
        RebuildDeferred(); // safe in edit + play
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Defer to avoid destroying during OnValidate
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            EnsureTilesRoot();
            RebuildImmediate();
        };
    }
#endif

    [ContextMenu("Rebuild Now")]
    void RebuildNow()
    {
        StopAllCoroutines();
        EnsureTilesRoot();
        RebuildImmediate();
    }

    void EnsureTilesRoot()
    {
        if (tilesRoot != null) return;
        var t = transform.Find("Tiles");
        if (t == null)
        {
            var go = new GameObject("Tiles");
            go.transform.SetParent(transform, false);
            tilesRoot = go.transform;
        }
        else
        {
            tilesRoot = t;
        }
    }

    void RebuildDeferred()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(RebuildNextFrame());
        }
        else
        {
#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                if (this) RebuildImmediate();
            };
#else
            RebuildImmediate();
#endif
        }
    }

    IEnumerator RebuildNextFrame()
    {
        yield return null;
        RebuildImmediate();
    }

    void RebuildImmediate()
    {
        if (!lightTilePrefab || !darkTilePrefab) return;

        // Clear
        for (int i = tilesRoot.childCount - 1; i >= 0; i--)
        {
            var child = tilesRoot.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        // Build
        for (int rank = 0; rank < N; rank++)
            for (int file = 0; file < N; file++)
            {
                bool isLight = ((file + rank) & 1) != 0;
                var prefab = isLight ? lightTilePrefab : darkTilePrefab;

                var go = (GameObject)InstantiatePrefab(prefab, tilesRoot);
                go.name = $"Tile_{file}_{rank}";
                go.transform.localPosition = new Vector3(file - 3.5f, 0f, rank - 3.5f);
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                go.transform.localScale = Vector3.one;
            }
    }

    Object InstantiatePrefab(GameObject prefab, Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
        return Instantiate(prefab, parent);
    }
}
