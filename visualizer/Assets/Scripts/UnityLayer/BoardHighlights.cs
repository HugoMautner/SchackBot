using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BoardHighlights : MonoBehaviour
{
    [Header("Appearance")]
    [Tooltip("Height relative to the piece plane (Origin.y). Negative draws under pieces but over tiles.")]
    [SerializeField] private float yOffset = -0.0005f;

    [Header("Scales (fraction of a tile)")]
    [Range(0.1f, 1f)][SerializeField] private float selectionScale = 1.00f; // full-tile fill
    [Range(0.05f, 0.6f)][SerializeField] private float dotScale = 0.35f;

    [Header("Fill Nudge")]
    [Tooltip("Small extra scale to hide 1px seams (0 = exact). Typical 0.001–0.005 (0.1–0.5%).")]
    [SerializeField] private float fillNudge = 0.0025f;

    [Header("Colors")]
    [Tooltip("Single color used for selection and last-move fills. Alpha controls intensity.")]
    [SerializeField] private Color fillColor = new Color(0.78f, 0.93f, 0.73f, 0.40f);
    [Tooltip("Color used for the destination ring (inner border).")]
    [SerializeField] private Color ringColor = new Color(0.78f, 0.93f, 0.73f, 0.35f);
    [Tooltip("Color for legal-move dots (if you use them later).")]
    [SerializeField] private Color dotColor = new Color(0f, 0f, 0f, 0.18f);

    [Header("Per-use Alpha Multipliers")]
    [SerializeField] private float selectionAlphaMultiplier = 1.00f;
    [SerializeField] private float lastMoveAlphaMultiplier = 1.00f;

    [Header("Destination Ring (inner border)")]
    [Tooltip("Inset of the ring from the tile edge, as a fraction of tile size.")]
    [Range(0f, 0.45f)][SerializeField] private float ringInsetFraction = 0.08f;
    [Tooltip("Thickness of each ring stroke, as a fraction of tile size.")]
    [Range(0.005f, 0.2f)][SerializeField] private float ringThicknessFraction = 0.06f;

    [Header("Sprite Sorting")]
    [Tooltip("Copy sorting layer/order + render layer from any piece SpriteRenderer once it appears.")]
    [SerializeField] private bool autoMatchPieceSorting = true;
    [Tooltip("Highlights order = piece order + this value. Negative → under pieces.")]
    [SerializeField] private int relativeOrderToPieces = -10;
    [SerializeField] private string fallbackSortingLayerName = "Default";
    [SerializeField] private int fallbackSortingOrder = -10;

    // Runtime
    private static Sprite _unitWhiteSprite;

    // Fill markers
    private Transform _selection, _lastFrom, _lastTo;

    // Destination ring (4 segments)
    private Transform _ringParent, _ringTop, _ringBottom, _ringLeft, _ringRight;

    // Dots pool
    private readonly List<Transform> _dotPool = new List<Transform>();
    private int _activeDotCount;

    // Sorting & layer targets
    private int _targetSortingLayerId;
    private int _targetOrderInLayer;
    private int _targetUnityLayer;
    private bool _matchedPieceSortingOnce;

    // ---------- Public API ----------
    public void SetSelection(int? squareIndex)
    {
        EnsureReady();
        PlaceTintedFill(_selection, squareIndex, selectionScale, selectionAlphaMultiplier);
    }

    // REPURPOSED: Destination is drawn as a hollow ring, not a full-tile fill
    public void SetDestination(int? squareIndex)
    {
        EnsureReady();
        EnsureRingCreated();

        if (squareIndex == null)
        {
            Show(_ringParent, false);
            return;
        }

        int sq = squareIndex.Value;
        Vector3 center = BoardCoord.SquareToWorld(sq) + new Vector3(0f, yOffset, 0f);
        _ringParent.position = center;

        float tile = BoardCoord.TileSize;
        float inset = Mathf.Clamp01(ringInsetFraction) * tile;
        float thickness = Mathf.Clamp(ringThicknessFraction, 0.001f, 0.5f) * tile;
        float usable = Mathf.Max(0f, tile - 2f * inset);
        float halfTile = tile * 0.5f;

        // Each segment is a thin sprite; local X = width, local Y = height (due to +90°X rotation)
        Vector3 horizScale = new Vector3(usable, thickness, 1f);
        Vector3 vertScale = new Vector3(thickness, usable, 1f);

        float zOff = halfTile - inset - thickness * 0.5f;
        float xOff = halfTile - inset - thickness * 0.5f;

        _ringTop.localPosition = new Vector3(0f, zOff, 0f);
        _ringBottom.localPosition = new Vector3(0f, -zOff, 0f);
        _ringLeft.localPosition = new Vector3(-xOff, 0f, 0f);
        _ringRight.localPosition = new Vector3(xOff, 0f, 0f);

        _ringTop.localScale = horizScale;
        _ringBottom.localScale = horizScale;
        _ringLeft.localScale = vertScale;
        _ringRight.localScale = vertScale;

        // Apply ring color
        SetSpriteColor(_ringTop, ringColor);
        SetSpriteColor(_ringBottom, ringColor);
        SetSpriteColor(_ringLeft, ringColor);
        SetSpriteColor(_ringRight, ringColor);

        Show(_ringParent, true);
    }

    public void SetLastMove(int fromSquareIndex, int toSquareIndex)
    {
        EnsureReady();
        PlaceTintedFill(_lastFrom, fromSquareIndex, selectionScale, lastMoveAlphaMultiplier);
        PlaceTintedFill(_lastTo, toSquareIndex, selectionScale, lastMoveAlphaMultiplier);
    }

    public void ShowDots(IEnumerable<int> squares)
    {
        EnsureReady();
        ClearDots();
        foreach (int sq in squares)
        {
            Transform t = RentDot();
            t.position = BoardCoord.SquareToWorld(sq) + new Vector3(0f, yOffset, 0f);
            t.localScale = Vector3.one * BoardCoord.TileSize * dotScale;
            SetSpriteColor(t, dotColor);
            Show(t, true);
        }
    }

    public void ClearDots()
    {
        for (int i = 0; i < _dotPool.Count; i++) { Show(_dotPool[i], false); }
        _activeDotCount = 0;
    }

    // ---------- Internals ----------
    private void EnsureReady()
    {
        if (!_matchedPieceSortingOnce)
        {
            TryMatchPieceSortingAndLayer();
        }

        if (_selection == null)
        {
            EnsureUnitWhiteSprite();
            _selection = CreateFillMarker("Selection", selectionScale);
            _lastFrom = CreateFillMarker("LastFrom", selectionScale);
            _lastTo = CreateFillMarker("LastTo", selectionScale);

            Show(_selection, false);
            Show(_lastFrom, false);
            Show(_lastTo, false);
        }
    }

    private void TryMatchPieceSortingAndLayer()
    {
        if (!autoMatchPieceSorting)
        {
            _targetSortingLayerId = SortingLayer.NameToID(fallbackSortingLayerName);
            _targetOrderInLayer = fallbackSortingOrder;
            _targetUnityLayer = gameObject.layer;
            _matchedPieceSortingOnce = true;
            return;
        }

        SpriteRenderer pieceSr = GetComponentInParent<Transform>()?.GetComponentInChildren<SpriteRenderer>();
        if (pieceSr == null)
        {
            _targetSortingLayerId = SortingLayer.NameToID(fallbackSortingLayerName);
            _targetOrderInLayer = fallbackSortingOrder;
            _targetUnityLayer = gameObject.layer;
            return;
        }

        _targetSortingLayerId = pieceSr.sortingLayerID;
        _targetOrderInLayer = pieceSr.sortingOrder + relativeOrderToPieces;
        _targetUnityLayer = pieceSr.gameObject.layer;
        _matchedPieceSortingOnce = true;
    }

    private static void EnsureUnitWhiteSprite()
    {
        if (_unitWhiteSprite != null) { return; }
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);
        _unitWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
        _unitWhiteSprite.name = "HighlightWhite1x1_Sprite";
    }

    private Transform CreateFillMarker(string name, float scale)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = Vector3.one * BoardCoord.TileSize * scale * (1f + fillNudge);
        go.layer = _targetUnityLayer;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _unitWhiteSprite;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.sortingLayerID = _targetSortingLayerId;
        sr.sortingOrder = _targetOrderInLayer;
        sr.color = Color.white;

        return go.transform;
    }

    private void EnsureRingCreated()
    {
        if (_ringParent != null) { return; }

        _ringParent = new GameObject("DestinationRing").transform;
        _ringParent.SetParent(transform, false);
        _ringParent.rotation = Quaternion.Euler(90f, 0f, 0f);
        _ringParent.gameObject.layer = _targetUnityLayer;

        _ringTop = CreateRingSegment("Top");
        _ringBottom = CreateRingSegment("Bottom");
        _ringLeft = CreateRingSegment("Left");
        _ringRight = CreateRingSegment("Right");

        Show(_ringParent, false);
    }

    private Transform CreateRingSegment(string name)
    {
        GameObject go = new GameObject("Ring_" + name);
        go.transform.SetParent(_ringParent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.layer = _targetUnityLayer;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _unitWhiteSprite;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.sortingLayerID = _targetSortingLayerId;
        sr.sortingOrder = _targetOrderInLayer;
        sr.color = Color.white;

        return go.transform;
    }

    private void PlaceTintedFill(Transform t, int? squareIndex, float scale, float alphaMultiplier)
    {
        if (t == null) { return; }

        if (squareIndex == null)
        {
            Show(t, false);
            return;
        }

        int sq = squareIndex.Value;
        t.position = BoardCoord.SquareToWorld(sq) + new Vector3(0f, yOffset, 0f);
        t.localScale = Vector3.one * BoardCoord.TileSize * scale * (1f + fillNudge);

        Color c = fillColor;
        c.a *= Mathf.Clamp01(alphaMultiplier);

        SetSpriteColor(t, c);
        Show(t, true);
    }

    private void SetSpriteColor(Transform t, Color c)
    {
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = c;
            sr.sortingLayerID = _targetSortingLayerId;
            sr.sortingOrder = _targetOrderInLayer;
        }
        t.gameObject.layer = _targetUnityLayer;
    }

    private Transform RentDot()
    {
        if (_activeDotCount < _dotPool.Count) { return _dotPool[_activeDotCount++]; }
        Transform dot = CreateFillMarker("Dot", dotScale);
        _dotPool.Add(dot);
        _activeDotCount++;
        return dot;
    }

    private static void Show(Transform t, bool on)
    {
        if (t != null) { t.gameObject.SetActive(on); }
    }
}
