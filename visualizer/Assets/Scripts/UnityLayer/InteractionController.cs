using UnityEngine;

[DisallowMultipleComponent]
public class InteractionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;

    [Header("Selection")]
    [SerializeField] private LayerMask pieceMask = ~0; // set this to your Pieces layer

    [Header("Options")]
    [SerializeField] private float maxRayDistance = 100f;
    [SerializeField] private bool commitVisualMove = false; // true = piece snaps to target square visually

    private MovablePiece _active;
    private int _fromSquare = -1;
    private Vector3 _lastWorldOnBoard; // last valid world point on the board plane

    private void Reset()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // --- Begin drag ---
        if (Input.GetMouseButtonDown(0) && _active == null)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, pieceMask))
            {
                var mp = hit.collider.GetComponentInParent<MovablePiece>();
                if (mp != null)
                {
                    _active = mp;
                    _fromSquare = mp.Square;

                    if (BoardCoord.TryRayToBoard(ray, out var world))
                    {
                        _lastWorldOnBoard = world;
                        _active.BeginDrag(world);
                    }
                }
            }
        }

        // --- Drag update ---
        if (_active != null)
        {
            if (BoardCoord.TryRayToBoard(ray, out var world))
            {
                _lastWorldOnBoard = world;
                _active.UpdateDrag(world);
            }

            // --- End drag ---
            if (Input.GetMouseButtonUp(0))
            {
                _active.EndDrag();

                // Decide drop square from last valid board-plane position
                if (BoardCoord.TryWorldToSquare(_lastWorldOnBoard, out int toSquare))
                {
                    if (commitVisualMove)
                    {
                        // Visual-only commit (engine not updated yet)
                        _active.SetSquare(toSquare);
                    }
                    // else: EndDrag already snapped back to previous square
                }

                _active = null;
                _fromSquare = -1;
            }
        }
    }
}
