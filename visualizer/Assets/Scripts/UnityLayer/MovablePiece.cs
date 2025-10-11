using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class MovablePiece : MonoBehaviour
{
    public int Square { get; private set; } = -1;

    [Header("View")]
    [SerializeField] Transform visual;

    [Header("Drag")]
    [SerializeField] float dragLift = 0.02f;
    [SerializeField] bool disableColliderWhileDragging = true;

    private Vector3 _restWorld;
    private bool _dragging;
    private Collider _col;

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (visual == null)
        {
            Transform t = transform.Find("Visual");
            if (t) { visual = t; }
        }
    }

    public void SetSquare(int square)
    {
        if (!BoardCoord.IsValidSquareIndex(square))
        {
            Debug.LogError($"MovablePiece.SetSquare: index out of range ({square}).");
            return;
        }

        Square = square;
        _restWorld = BoardCoord.SquareToWorld(square);
        transform.position = _restWorld;
    }

    public void BeginDrag(Vector3 worldPoint)
    {
        if (_dragging) { return; }
        _dragging = true;

        if (disableColliderWhileDragging && _col) { _col.enabled = false; }

        // Lift slightly above the board plane to avoid z-fighting while dragging.
        float liftedY = BoardCoord.Origin.y + dragLift;

        // Start following immediately on the first frame of the drag.
        transform.position = new Vector3(worldPoint.x, liftedY, worldPoint.z);
    }

    public void UpdateDrag(Vector3 worldPoint)
    {
        if (!_dragging) { return; }

        float liftedY = BoardCoord.Origin.y + dragLift;
        transform.position = new Vector3(worldPoint.x, liftedY, worldPoint.z);
    }

    public void EndDrag()
    {
        if (!_dragging) return;
        _dragging = false;

        if (disableColliderWhileDragging && _col) _col.enabled = true;

        // Snap back to the last committed square. If a move is accepted,
        // the controller will immediately call SetSquare(toSquare) afterward.
        if (Square >= 0)
        {
            transform.position = _restWorld;
        }
    }

}
