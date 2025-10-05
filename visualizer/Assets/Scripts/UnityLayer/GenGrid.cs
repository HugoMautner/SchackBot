using UnityEngine;

[ExecuteAlways]
public class GenGrid : MonoBehaviour
{
    [SerializeField] Material lightMat;
    [SerializeField] Material darkMat;

    const int N = 8;

    void OnEnable()
    {
        EnsureBuilt();
        ApplyMaterials();
    }

    void OnValidate()
    {
        ApplyMaterials();
    }

    void EnsureBuilt()
    {
        if (transform.childCount == N * N) return;

        // Clear any existing children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        for (int rank = 0; rank < N; rank++)
            for (int file = 0; file < N; file++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.name = $"Tile_{file}_{rank}";
                tile.transform.SetParent(transform, false);
                tile.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                tile.transform.localPosition = new Vector3(file - 3.5f, 0f, rank - 3.5f);
                tile.transform.localScale = Vector3.one;
            }
    }

    void ApplyMaterials()
    {
        if (transform.childCount != N * N) return;

        int i = 0;
        for (int rank = 0; rank < N; rank++)
            for (int file = 0; file < N; file++, i++)
            {
                var child = transform.GetChild(i);
                var r = child.GetComponent<MeshRenderer>();
                bool isLight = (file + rank) % 2 != 0;
                r.sharedMaterial = isLight ? lightMat : darkMat;
            }
    }

    [ContextMenu("Rebuild Now")]
    void RebuildNow() { EnsureBuilt(); ApplyMaterials(); }
}
