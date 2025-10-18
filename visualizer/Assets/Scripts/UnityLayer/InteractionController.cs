using UnityEngine;

[DisallowMultipleComponent]
public class InteractionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask pieceMask = ~0;
    [SerializeField] private BoardHighlights highlights;
    [SerializeField] private BoardPieceRegistry registry;

    [Header("Cursor")]
    [SerializeField] private Texture2D openHandCursor;
    [SerializeField] private Texture2D grabbingHandCursor;
    [SerializeField] private Vector2 openHandHotspot = new(8, 2);
    [SerializeField] private Vector2 grabbingHandHotspot = new(10, 6);

    [Header("Look & Feel")]
    [SerializeField] private float dragThresholdPixels = 6f;
    [SerializeField] private float clickSelectLift = 0.01f; // selection lift
    [SerializeField] private float dragLiftOffset = 0.02f;  // drag lift
    [SerializeField] private float animDuration = 0.12f;    // click-to-move animation duration

    private MovablePiece _active;     // dragging
    private MovablePiece _selected;   // click-to-move
    private MovablePiece _pressPiece; // press candidate
    private int _fromSquare = -1;
    private int _selectedFromSquare = -1;
    private Vector2 _mouseDownScreen;
    private Vector3 _lastWorldOnBoard;

    private enum CursorState { Arrow, Open, Grab }
    private CursorState _cursorState = CursorState.Arrow;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!highlights) highlights = FindObjectOfType<BoardHighlights>();
        if (!registry) registry = FindAnyObjectByType<BoardPieceRegistry>();
    }

    void Update()
    {
        if (!cam) return;
        var ray = cam.ScreenPointToRay(Input.mousePosition);

        // Cursor state logic
        bool isOverOccupiedSquare = false;
        if (BoardCoord.TryRayToBoard(ray, out Vector3 worldPointOnBoard))
        {
            if (BoardCoord.TryWorldToSquare(worldPointOnBoard, out int hoveredSquareIndex))
            {
                isOverOccupiedSquare = registry.GetAt(hoveredSquareIndex) != null;
            }
        }
        bool grabbing =
            (_active != null) ||
            (_pressPiece != null && Input.GetMouseButton(0));

        if (!isOverOccupiedSquare) { SetCursorState(CursorState.Arrow); }
        else if (grabbing) { SetCursorState(CursorState.Grab); }
        else { SetCursorState(CursorState.Open); }


        // Mouse down: record a press
        if (Input.GetMouseButtonDown(0))
        {
            _pressPiece = null;
            _mouseDownScreen = (Vector2)Input.mousePosition;

            if (Physics.Raycast(ray, out var hit, 100f, pieceMask))
            {
                var mp = hit.collider.GetComponentInParent<MovablePiece>();
                if (mp != null)
                {
                    _pressPiece = mp;
                    _fromSquare = mp.Square;
                }
            }

            // Fallback to arrow cursor
            if (_pressPiece == null)
            {
                if (BoardCoord.TryRayToBoard(ray, out Vector3 pressWorldPointOnBoard))
                {
                    if (BoardCoord.TryWorldToSquare(pressWorldPointOnBoard, out int pressedSquareIndex))
                    {
                        MovablePiece pieceAtPressedSquare = registry.GetAt(pressedSquareIndex);
                        if (pieceAtPressedSquare != null)
                        {
                            _pressPiece = pieceAtPressedSquare;
                            _fromSquare = pieceAtPressedSquare.Square;
                        }
                    }
                }
            }
        }

        // Promote to drag when threshold crossed
        if (_pressPiece != null && _active == null)
        {
            float moved = Vector2.Distance(_mouseDownScreen, (Vector2)Input.mousePosition);
            if (moved >= dragThresholdPixels && BoardCoord.TryRayToBoard(ray, out var world))
            {
                _lastWorldOnBoard = world;
                _active = _pressPiece;
                _active.BeginDrag(new Vector3(world.x, BoardCoord.Origin.y + dragLiftOffset, world.z));
                _pressPiece = null;
            }
        }

        // Drag update
        if (_active != null)
        {
            if (BoardCoord.TryRayToBoard(ray, out var world))
            {
                _lastWorldOnBoard = world;
                _active.UpdateDrag(new Vector3(world.x, BoardCoord.Origin.y + dragLiftOffset, world.z));

                if (BoardCoord.TryWorldToSquare(world, out int dsq))
                    highlights?.SetDestination(dsq);
            }

            // End drag
            if (Input.GetMouseButtonUp(0))
            {
                _active.EndDrag();

                if (BoardCoord.TryWorldToSquare(_lastWorldOnBoard, out int toSquare))
                {
                    CommitVisual(_active, toSquare);
                    highlights?.SetLastMove(_fromSquare, toSquare);
                }

                highlights?.SetDestination(null);
                _active = null;
                _fromSquare = -1;
                return;
            }
        }

        // Click-to-move path (no active drag)
        if (Input.GetMouseButtonUp(0) && _active == null)
        {
            if (!BoardCoord.TryRayToBoard(ray, out var worldUp) ||
                !BoardCoord.TryWorldToSquare(worldUp, out int squareUp))
            {
                _pressPiece = null;
                return;
            }

            if (_selected != null)
            {
                // Clicking same piece = deselect; else move to hovered square.
                if (_pressPiece == _selected)
                {
                    SetSelected(null);
                }
                else
                {
                    CommitVisual(_selected, squareUp);
                    if (_selectedFromSquare >= 0) highlights?.SetLastMove(_selectedFromSquare, squareUp);
                    SetSelected(null);
                }

                _pressPiece = null;
                return;
            }

            // No selection → click on a piece selects it (and snaps to cursor center)
            if (_pressPiece != null)
            {
                SetSelected(_pressPiece);
                // Snap-on-impact to cursor center, slight lift for feedback
                _pressPiece.transform.position = new Vector3(
                    worldUp.x, BoardCoord.Origin.y + clickSelectLift, worldUp.z);
            }

            _pressPiece = null;
        }

        // While selected and not dragging: show destination preview under cursor
        if (_selected != null &&
            BoardCoord.TryRayToBoard(ray, out var dw) &&
            BoardCoord.TryWorldToSquare(dw, out int dSq))
        {
            highlights?.SetDestination(dSq);
        }
    }

    // ---------- helpers ----------

    private void SetSelected(MovablePiece mp)
    {
        // Un-highlight previous
        if (_selected != null)
        {
            var p = _selected.transform.position;
            _selected.transform.position = new Vector3(p.x, BoardCoord.Origin.y, p.z);
        }

        _selected = mp;
        _selectedFromSquare = mp ? mp.Square : -1;
        highlights?.SetSelection(mp ? mp.Square : (int?)null);

        if (_selected != null)
        {
            var p = _selected.transform.position;
            _selected.transform.position = new Vector3(p.x, BoardCoord.Origin.y + clickSelectLift, p.z);
        }

        highlights?.ClearDots(); // reserve for future legal dots
    }

    private void CommitVisual(MovablePiece piece, int toSquare)
    {
        // Simple capture prevention
        var occupant = registry.GetAt(toSquare);
        if (occupant != null && occupant != piece)
        {
            registry.RemoveAt(toSquare);
            var go = occupant.gameObject;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        StopAllCoroutines();
        StartCoroutine(AnimateMove(piece, toSquare, animDuration));
    }

    private System.Collections.IEnumerator AnimateMove(MovablePiece piece, int toSquare, float dur)
    {
        Vector3 start = piece.transform.position;
        Vector3 end = BoardCoord.SquareToWorld(toSquare);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float s = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f); // cubic ease-out
            piece.transform.position = Vector3.LerpUnclamped(start, end, s);
            yield return null;
        }
        piece.SetSquare(toSquare); // lock to exact center
        registry.Move(_fromSquare, toSquare);
        highlights?.SetDestination(null);
        highlights?.SetSelection(null);
    }

    private void SetCursorState(CursorState s)
    {
        if (_cursorState == s) return;
        _cursorState = s;

        switch (s)
        {
            case CursorState.Open:
                if (openHandCursor) { Cursor.SetCursor(openHandCursor, openHandHotspot, CursorMode.Auto); break; }
                goto default;
            case CursorState.Grab:
                if (grabbingHandCursor) { Cursor.SetCursor(grabbingHandCursor, grabbingHandHotspot, CursorMode.Auto); break; }
                goto default;
            default:
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                break;
        }
    }
}
