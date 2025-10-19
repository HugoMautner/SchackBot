using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private BoardHighlights highlights;
    [SerializeField] private BoardPieceRegistry registry;

    [Header("Cursor")]
    [SerializeField] private Texture2D openHandCursor;
    [SerializeField] private Texture2D grabbingHandCursor;
    [SerializeField] private Vector2 openHandHotspot = new Vector2(8, 2);
    [SerializeField] private Vector2 grabbingHandHotspot = new Vector2(10, 6);

    [Header("UX")]
    [SerializeField] private float dragThresholdPixels = 6f;
    [SerializeField] private float clickSelectLift = 0.01f; // selection lift
    [SerializeField] private float dragLiftOffset = 0.02f;  // drag lift
    [SerializeField] private float animDuration = 0.12f;    // click-to-move animation duration
    [SerializeField] private bool showDestinationHighlight = true; // drives the ring

    private MovablePiece _activePiece;         // piece currently being dragged
    private MovablePiece _selectedPiece;       // piece selected for click-to-move
    private MovablePiece _pressCandidatePiece; // piece under initial press (before drag threshold)
    private int _dragFromSquareIndex = -1;     // from-square for current drag
    private int _selectedFromSquareIndex = -1; // from-square when a piece is selected for click-to-move
    private Vector2 _mouseDownScreen;
    private Vector3 _lastWorldPointOnBoard;

    private enum CursorState { Arrow, Open, Grab }
    private CursorState _cursorState = CursorState.Arrow;

    private void Awake()
    {
        if (cam == null) { cam = Camera.main; }
        if (highlights == null) { highlights = FindObjectOfType<BoardHighlights>(); }
        if (registry == null) { registry = FindAnyObjectByType<BoardPieceRegistry>(); }
    }

    private void Update()
    {
        if (cam == null) { return; }

        Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);

        // --- Cursor state (square-driven) ---
        bool isOverOccupiedSquare = false;
        if (BoardCoord.TryRayToBoard(mouseRay, out Vector3 worldPointOnBoardForCursor))
        {
            if (BoardCoord.TryWorldToSquare(worldPointOnBoardForCursor, out int hoveredSquareIndex))
            {
                isOverOccupiedSquare = registry != null && registry.GetAt(hoveredSquareIndex) != null;
            }
        }

        bool isGrabbing =
            (_activePiece != null) ||
            (_pressCandidatePiece != null && Input.GetMouseButton(0));

        if (!isOverOccupiedSquare) { SetCursorState(CursorState.Arrow); }
        else if (isGrabbing) { SetCursorState(CursorState.Grab); }
        else { SetCursorState(CursorState.Open); }

        // --- Mouse down: select immediately (snap to cursor) or prepare for drag ---
        if (Input.GetMouseButtonDown(0))
        {
            _pressCandidatePiece = null;
            _mouseDownScreen = (Vector2)Input.mousePosition;

            if (BoardCoord.TryRayToBoard(mouseRay, out Vector3 pressWorldPointOnBoard) &&
                BoardCoord.TryWorldToSquare(pressWorldPointOnBoard, out int pressedSquareIndex))
            {
                MovablePiece pieceAtPressedSquare = registry != null ? registry.GetAt(pressedSquareIndex) : null;
                if (pieceAtPressedSquare != null)
                {
                    // Select this piece right away
                    SetSelected(pieceAtPressedSquare);
                    _pressCandidatePiece = pieceAtPressedSquare;
                    _dragFromSquareIndex = pieceAtPressedSquare.Square;

                    // Snap to cursor center on impact (lifted)
                    _pressCandidatePiece.transform.position = new Vector3(
                        pressWorldPointOnBoard.x,
                        BoardCoord.Origin.y + clickSelectLift,
                        pressWorldPointOnBoard.z
                    );

                    // Ensure start-square tint is visible even before drag
                    if (highlights != null)
                    {
                        highlights.SetSelection(_dragFromSquareIndex);
                    }
                }
            }
        }

        // --- Promote press -> drag once threshold crossed ---
        if (_pressCandidatePiece != null && _activePiece == null)
        {
            float movedPixels = Vector2.Distance(_mouseDownScreen, (Vector2)Input.mousePosition);
            if (movedPixels >= dragThresholdPixels &&
                BoardCoord.TryRayToBoard(mouseRay, out Vector3 worldPointOnBoardForDrag))
            {
                _lastWorldPointOnBoard = worldPointOnBoardForDrag;
                _activePiece = _pressCandidatePiece;

                // Begin drag from current cursor position (already snapped on mousedown)
                Vector3 lifted = new Vector3(
                    worldPointOnBoardForDrag.x,
                    BoardCoord.Origin.y + dragLiftOffset,
                    worldPointOnBoardForDrag.z
                );
                _activePiece.BeginDrag(lifted);

                _pressCandidatePiece = null;
            }
        }

        // --- Drag update ---
        if (_activePiece != null)
        {
            if (BoardCoord.TryRayToBoard(mouseRay, out Vector3 worldPointOnBoardDuringDrag))
            {
                _lastWorldPointOnBoard = worldPointOnBoardDuringDrag;

                Vector3 lifted = new Vector3(
                    worldPointOnBoardDuringDrag.x,
                    BoardCoord.Origin.y + dragLiftOffset,
                    worldPointOnBoardDuringDrag.z
                );
                _activePiece.UpdateDrag(lifted);

                if (BoardCoord.TryWorldToSquare(worldPointOnBoardDuringDrag, out int destinationSquareIndexDuringDrag))
                {
                    if (highlights != null)
                    {
                        highlights.SetDestination(showDestinationHighlight ? destinationSquareIndexDuringDrag : (int?)null);
                    }
                }
                else
                {
                    if (highlights != null)
                    {
                        highlights.SetDestination(null);
                    }
                }
            }

            // --- End drag ---
            if (Input.GetMouseButtonUp(0))
            {
                if (BoardCoord.TryWorldToSquare(_lastWorldPointOnBoard, out int toSquareIndex))
                {
                    _activePiece.EndDrag(snapBack: false);
                    CommitInstant(_activePiece, _dragFromSquareIndex, toSquareIndex);

                    if (highlights != null)
                    {
                        highlights.SetLastMove(_dragFromSquareIndex, toSquareIndex);
                    }
                }
                else
                {
                    _activePiece.EndDrag(snapBack: true);
                }

                // Clear transient visuals on drag end
                if (highlights != null)
                {
                    highlights.SetDestination(null);
                    highlights.SetSelection(null);
                }

                _activePiece = null;
                _dragFromSquareIndex = -1;
                return;
            }
        }

        // --- Mouse up without drag: click-to-move flow ---
        if (Input.GetMouseButtonUp(0) && _activePiece == null)
        {
            if (!BoardCoord.TryRayToBoard(mouseRay, out Vector3 releaseWorldPointOnBoard) ||
                !BoardCoord.TryWorldToSquare(releaseWorldPointOnBoard, out int releaseSquareIndex))
            {
                _pressCandidatePiece = null;
                return;
            }

            if (_selectedPiece != null)
            {
                // Released on the same square as selection: snap back to square center, stay selected
                if (releaseSquareIndex == _selectedFromSquareIndex)
                {
                    SnapPieceToSquareCenter(_selectedPiece, clickSelectLift);
                    _pressCandidatePiece = null;
                    return;
                }

                // Otherwise: animated move to the release square
                if (_selectedFromSquareIndex >= 0)
                {
                    CommitAnimated(_selectedPiece, _selectedFromSquareIndex, releaseSquareIndex);
                    if (highlights != null)
                    {
                        highlights.SetLastMove(_selectedFromSquareIndex, releaseSquareIndex);
                    }
                    SetSelected(null); // end selection after committing
                }

                _pressCandidatePiece = null;
                return;
            }

            _pressCandidatePiece = null;
        }

        // --- While selected and not dragging: optional ring preview under cursor ---
        if (_selectedPiece != null &&
            BoardCoord.TryRayToBoard(mouseRay, out Vector3 worldPointOnBoardForPreview) &&
            BoardCoord.TryWorldToSquare(worldPointOnBoardForPreview, out int previewSquareIndex) &&
            highlights != null)
        {
            highlights.SetDestination(showDestinationHighlight ? previewSquareIndex : (int?)null);
        }

        // If destination preview is disabled globally, scrub it.
        if (!showDestinationHighlight && highlights != null)
        {
            highlights.SetDestination(null);
        }
    }

    // ----------------- helpers -----------------

    private void SetSelected(MovablePiece piece)
    {
        if (_selectedPiece != null)
        {
            // Drop height on previous
            Vector3 p = _selectedPiece.transform.position;
            _selectedPiece.transform.position = new Vector3(p.x, BoardCoord.Origin.y, p.z);
        }

        _selectedPiece = piece;
        _selectedFromSquareIndex = piece != null ? piece.Square : -1;

        if (highlights != null)
        {
            highlights.SetSelection(piece != null ? (int?)piece.Square : null);
            highlights.ClearDots(); // reserved for future legal-move dots
        }

        if (_selectedPiece != null)
        {
            // Keep it lifted while selected (position may be overridden by caller)
            Vector3 p = _selectedPiece.transform.position;
            _selectedPiece.transform.position = new Vector3(p.x, BoardCoord.Origin.y + clickSelectLift, p.z);
        }
    }

    private static void SnapPieceToSquareCenter(MovablePiece piece, float liftY)
    {
        Vector3 center = BoardCoord.SquareToWorld(piece.Square);
        piece.transform.position = new Vector3(center.x, BoardCoord.Origin.y + liftY, center.z);
    }

    private void CommitInstant(MovablePiece piece, int fromSquareIndex, int toSquareIndex)
    {
        if (registry == null || piece == null) { return; }
        if (fromSquareIndex == toSquareIndex) { return; }

        MovablePiece occupant = registry.GetAt(toSquareIndex);
        if (occupant != null && occupant != piece)
        {
            registry.RemoveAt(toSquareIndex);
            GameObject go = occupant.gameObject;
            if (Application.isPlaying) { Destroy(go); } else { DestroyImmediate(go); }
        }

        piece.SetSquare(toSquareIndex);
        registry.Move(fromSquareIndex, toSquareIndex);
    }

    private void CommitAnimated(MovablePiece piece, int fromSquareIndex, int toSquareIndex)
    {
        if (registry == null || piece == null) { return; }
        if (fromSquareIndex == toSquareIndex) { return; }

        MovablePiece occupant = registry.GetAt(toSquareIndex);
        if (occupant != null && occupant != piece)
        {
            registry.RemoveAt(toSquareIndex);
            GameObject go = occupant.gameObject;
            if (Application.isPlaying) { Destroy(go); } else { DestroyImmediate(go); }
        }

        StopAllCoroutines();
        StartCoroutine(AnimateMove(piece, fromSquareIndex, toSquareIndex, animDuration));
    }

    private IEnumerator AnimateMove(MovablePiece piece, int fromSquareIndex, int toSquareIndex, float durationSeconds)
    {
        Vector3 start = piece.transform.position;
        Vector3 end = BoardCoord.SquareToWorld(toSquareIndex);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, durationSeconds);
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f); // cubic ease-out
            piece.transform.position = Vector3.LerpUnclamped(start, end, eased);
            yield return null;
        }

        piece.SetSquare(toSquareIndex);
        registry.Move(fromSquareIndex, toSquareIndex);

        if (highlights != null)
        {
            highlights.SetDestination(null);
            highlights.SetSelection(null);
        }
    }

    private void SetCursorState(CursorState newState)
    {
        if (_cursorState == newState) { return; }
        _cursorState = newState;

        switch (newState)
        {
            case CursorState.Open:
                {
                    if (openHandCursor != null)
                    {
                        Cursor.SetCursor(openHandCursor, openHandHotspot, CursorMode.Auto);
                        break;
                    }
                    goto default;
                }
            case CursorState.Grab:
                {
                    if (grabbingHandCursor != null)
                    {
                        Cursor.SetCursor(grabbingHandCursor, grabbingHandHotspot, CursorMode.Auto);
                        break;
                    }
                    goto default;
                }
            default:
                {
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    break;
                }
        }
    }
}
