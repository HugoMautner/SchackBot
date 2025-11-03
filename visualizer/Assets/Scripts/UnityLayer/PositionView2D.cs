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
    [Tooltip("Rest height of pieces relative to the Board's transform Y.")]
    public float yLift = 0.02f;                     // slight lift above tiles
    [Range(0.5f, 1.1f)]
    public float fill = 0.92f;                      // fraction of a tile; 1 = edge-to-edge
    [Tooltip("World size of one tile. Should match GenGrid.")]
    public float tileSize = 1f;
    [Tooltip("If false, board mapping is mirrored for black-at-bottom viewing.")]
    public bool whiteAtBottom = true;

    private Transform piecesRoot; // all spawned pieces live here
    public Position CurrentPosition { get; private set; }

    void OnEnable()
    {
        EnsurePiecesRoot();
        ConfigureBoardMapping(); // << important: keeps BoardCoord aligned
        Rebuild();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            EnsurePiecesRoot();
            ConfigureBoardMapping();
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
            CurrentPosition = null;
            return;
        }

        CurrentPosition = pos;

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

            // Set engine/view square on the MovablePiece and register it
            var mp = go.GetComponent<MovablePiece>();
            if (mp != null)
            {
                mp.SetSquare(square);

                // Add the piece to the registry
                BoardPieceRegistry reg = GetComponentInParent<BoardPieceRegistry>();
                if (reg) { reg.Register(mp, square); }
            }
            else
            {
                // Fallback: place via BoardCoord directly (still centered & consistent)
                go.transform.position = BoardCoord.SquareToWorld(square);
            }
        }
    }

    public void SetCurrentPosition(Position pos)
    {
        CurrentPosition = pos;
    }

    public void TryRefreshCurrentPosition()
    {
        try
        {
            var text = string.IsNullOrWhiteSpace(fen) ? DefaultStartFen : fen.Trim();
            CurrentPosition = Position.FromFen(text);
        }
        catch
        {
            // ignore parse errors - caller should handle if null
        }
    }

    // --- helpers ---

    private void ConfigureBoardMapping()
    {
        // We treat this GameObject's transform as the board CENTER.
        // Bottom-left *corner* of the board in LOCAL space is (-4*tile, 0, -4*tile).
        Vector3 localBottomLeft = new Vector3(-4f * tileSize, 0f, -4f * tileSize);

        // Convert to WORLD space and set the rest height (Origin.y) to transform.y + yLift.
        Vector3 originWorld = transform.TransformPoint(localBottomLeft);
        originWorld.y = transform.position.y + yLift;

        BoardCoord.Configure(originWorld, tileSize, whiteAtBottom);
    }

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
