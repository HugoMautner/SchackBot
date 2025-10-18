using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BoardHighlights : MonoBehaviour
{
    [Header("Appearance")]
    [Tooltip("Height above the board plane for all highlights.")]
    [SerializeField] private float yOffset = 0.001f;

    [Header("Scales (fraction of a tile)")]
    [Range(0.1f, 1f)][SerializeField] private float selectionScale = 0.98f;
    [Range(0.1f, 1f)][SerializeField] private float destinationScale = 0.98f;
    [Range(0.05f, 0.6f)][SerializeField] private float dotScale = 0.35f;

    [Header("Colors")]
    [SerializeField] private Color selectionColor = new(1f, 1f, 0.4f, 0.35f);
    [SerializeField] private Color destinationColor = new(0.5f, 0.8f, 1f, 0.28f);
    [SerializeField] private Color lastMoveColor = new(0.8f, 1f, 0.5f, 0.30f);
    [SerializeField] private Color dotColor = new(0f, 0f, 0f, 0.18f);

    Material _mat;
    Transform _selection, _destination, _lastFrom, _lastTo;
    readonly List<Transform> _dots = new();
    int _activeDots;

    void Awake()
    {
        var shader = Shader.Find("Unlit/Color");
        _mat = new Material(shader);

        _selection = CreateQuad("Selection", selectionScale, selectionColor);
        _destination = CreateQuad("Destination", destinationScale, destinationColor);
        _lastFrom = CreateQuad("LastFrom", selectionScale, lastMoveColor);
        _lastTo = CreateQuad("LastTo", selectionScale, lastMoveColor);

        Show(_selection, false); Show(_destination, false);
        Show(_lastFrom, false); Show(_lastTo, false);
    }

    Transform CreateQuad(string name, float scale, Color c)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * BoardCoord.TileSize * scale;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var mc = go.GetComponent<MeshCollider>(); if (mc) DestroyImmediate(mc);
        var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = _mat; mr.sharedMaterial.color = c;
        return go.transform;
    }

    // --- Public API ---------------------------------------------------------

    public void SetSelection(int? sq) => Place(_selection, sq, selectionScale, selectionColor);
    public void SetDestination(int? sq) => Place(_destination, sq, destinationScale, destinationColor);
    public void SetLastMove(int from, int to)
    {
        Place(_lastFrom, from, selectionScale, lastMoveColor);
        Place(_lastTo, to, selectionScale, lastMoveColor);
    }

    public void ShowDots(IEnumerable<int> squares)
    {
        ClearDots();
        foreach (var sq in squares)
        {
            var t = RentDot();
            t.position = BoardCoord.SquareToWorld(sq) + new Vector3(0, yOffset, 0);
            t.localScale = Vector3.one * BoardCoord.TileSize * dotScale;
            Show(t, true);
        }
    }
    public void ClearDots()
    {
        for (int i = 0; i < _dots.Count; i++) Show(_dots[i], false);
        _activeDots = 0;
    }

    // --- Internals ----------------------------------------------------------

    void Place(Transform t, int? sq, float scale, Color c)
    {
        if (sq == null) { Show(t, false); return; }
        t.position = BoardCoord.SquareToWorld(sq.Value) + new Vector3(0, yOffset, 0);
        t.localScale = Vector3.one * BoardCoord.TileSize * scale;
        var mr = t.GetComponent<MeshRenderer>(); if (mr) mr.sharedMaterial.color = c;
        Show(t, true);
    }

    Transform RentDot()
    {
        if (_activeDots < _dots.Count) return _dots[_activeDots++];
        var t = CreateQuad("Dot", dotScale, dotColor);
        _dots.Add(t);
        _activeDots++;
        return t;
    }

    static void Show(Transform t, bool on) { if (t) t.gameObject.SetActive(on); }
}
