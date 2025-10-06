using UnityEngine;
using SchackBot.Engine.Core;
using SchackBot.Engine.Positioning;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PositionView2D : MonoBehaviour
{
    [TextArea] public string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    [Header("Piece Prefab")]
    public GameObject piece2DPrefab;

    [Header("Sprites (index: 1..6 = Pawn, Knight, Bishop, Rook, Queen, King)")]
    public Sprite[] whiteSprites = new Sprite[7];
    public Sprite[] blackSprites = new Sprite[7];

    [Header("Layout")]
    public float yLift = 0.02f;   // lift above tiles slightly
    [Range(0.5f, 1.1f)]
    public float fill = 0.92f;    // fraction of a tile to occupy; 1 = edge-to-edge
    public float tileSize = 1f;   // your tiles are 1 unit; keep unless you change GenGrid

    private Transform piecesRoot; // all spawned pieces live here

    void OnEnable()
    {
        EnsurePiecesRoot();
        Rebuild();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            EnsurePiecesRoot();
            Rebuild();
        };
    }
#endif

    [ContextMenu("Rebuild Now")]
    public void Rebuild()
    {
        EnsurePiecesRoot();
        SweepStragglers();
        if (!IsReady()) return;

        // Parse FEN
        Position pos;
        try
        {
            var text = string.IsNullOrWhiteSpace(fen) ? DefaultStartFen : fen.Trim();
            pos = Position.FromFen(text);
        }
        catch
        {
            ClearPieces();
            return;
        }

        // Fresh container
        ClearPieces();

        // Spawn
        foreach (var (square, piece) in pos.EnumeratePieces())
        {
            var type = Piece.TypeOf(piece);
            if (type == PieceType.None) continue;

            var color = Piece.ColorOf(piece);
            var go = (GameObject)PrefabInstantiate(piece2DPrefab, piecesRoot);
            go.name = $"{color}_{type}_{square}";

            // --- Position root at square center (root stays unscaled, rotation=identity) ---
            int file = square & 7;
            int rank = square >> 3;
            go.transform.localPosition = new Vector3((file - 3.5f) * tileSize,
                                                     yLift,
                                                     (rank - 3.5f) * tileSize);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            // --- SpriteRenderer lives on the child (Visual) ---
            var sr = go.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                var sprites = (color == SchackBot.Engine.Core.Color.White) ? whiteSprites : blackSprites;
                var sprite = sprites[(int)type];
                if (sprite == null)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"Missing sprite for {color} {type} at index {(int)type}. Assign sprites 1..6; leave index 0 empty.", this);
#endif
                    if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
                    continue;
                }

                sr.sprite = sprite;

                // Ensure visual lies on XZ and auto-fit to tile
                var visual = sr.transform;
                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.Euler(90f, 0f, 0f);
                AutoFitToTile(visual, sr, tileSize, fill);
            }

            // --- Keep collider on root sized to the tile (and thin in Y) ---
            var col = go.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.center = Vector3.zero;
                col.size = new Vector3(tileSize * 0.95f, 0.05f, tileSize * 0.95f);
            }
        }
    }

    // --- helpers ---

    private void AutoFitToTile(Transform visual, SpriteRenderer sr, float tileWorldSize, float fillFrac)
    {
        // Reset child scale so bounds reflect raw sprite size
        visual.localScale = Vector3.one;

        // sprite.bounds is in local units with scale=1 on the visual
        var size = sr.sprite.bounds.size; // x/y in local sprite space
        float spriteMax = Mathf.Max(size.x, size.y);
        if (spriteMax <= 0f) spriteMax = 1f;

        float target = tileWorldSize * Mathf.Clamp01(fillFrac);
        float s = target / spriteMax;

        visual.localScale = new Vector3(s, s, s);
    }

    private void EnsurePiecesRoot()
    {
        if (piecesRoot && piecesRoot.parent == transform) return;

        var t = transform.Find("Pieces");
        if (t == null)
        {
            var root = new GameObject("Pieces");
            root.transform.SetParent(transform, false);
            piecesRoot = root.transform;
        }
        else
        {
            piecesRoot = t;
        }
    }

    private void ClearPieces()
    {
        if (!piecesRoot) return;
        for (int i = piecesRoot.childCount - 1; i >= 0; i--)
        {
            var child = piecesRoot.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    private void SweepStragglers()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            var n = child.name;
            if (n == "Pieces" || n == "Tiles") continue;

            var sr = child.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }

    private bool IsReady()
    {
        if (!piece2DPrefab) return false;
        return whiteSprites != null && blackSprites != null;
    }

    private static readonly string DefaultStartFen =
        "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    private Object PrefabInstantiate(GameObject prefab, Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var obj = PrefabUtility.InstantiatePrefab(prefab, parent);
            if (obj is GameObject g && g.transform.parent != parent)
                g.transform.SetParent(parent, false);
            return obj;
        }
#endif
        return Instantiate(prefab, parent);
    }
}
