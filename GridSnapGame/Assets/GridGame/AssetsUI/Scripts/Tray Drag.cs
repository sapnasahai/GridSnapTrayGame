using System.Collections.Generic;
using UnityEngine;

public class TrayDrag : MonoBehaviour
{
    public float gridSize = 2f;
    public float fixedY = 1f;
    public Vector2Int gridSizeXY = new Vector2Int(7, 8);
    public Vector2 gridOrigin = Vector2.zero;
    public Vector2Int traySize = new Vector2Int(1, 1); // Set per tray

    public static HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    private List<Vector2Int> myCells = new List<Vector2Int>();

    private bool isDragging = false;
    private Camera mainCam;

    private Vector3 originalPosition;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        if (isDragging)
        {
            DragTray();
        }
    }

    void OnMouseDown()
    {
        isDragging = true;
        originalPosition = transform.position;
        RemoveMyCells(); // Free previously occupied cells
    }

    void OnMouseUp()
    {
        isDragging = false;
        SnapToGrid(); // Try to snap to new grid position
    }

    void DragTray()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 pos = hit.point;
            pos.y = fixedY;

            // Calculate tray's offset based on size (to center it properly)
            float xOffset = (traySize.x - 1) * gridSize * 0.5f;
            float zOffset = (traySize.y - 1) * gridSize * 0.5f;

            // Clamp tray movement within grid boundaries
            float minX = gridOrigin.x + xOffset;
            float maxX = gridOrigin.x + (gridSizeXY.x - traySize.x) * gridSize + xOffset;
            float minZ = gridOrigin.y + zOffset;
            float maxZ = gridOrigin.y + (gridSizeXY.y - traySize.y) * gridSize + zOffset;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

            transform.position = pos;
        }
    }

    void SnapToGrid()
    {
        Vector3 pos = transform.position;

        // Recalculate tray offsets for snapping
        float xOffset = (traySize.x - 1) * gridSize * 0.5f;
        float zOffset = (traySize.y - 1) * gridSize * 0.5f;

        float snappedX = Mathf.Round((pos.x - gridOrigin.x - xOffset) / gridSize) * gridSize + gridOrigin.x + xOffset;
        float snappedZ = Mathf.Round((pos.z - gridOrigin.y - zOffset) / gridSize) * gridSize + gridOrigin.y + zOffset;

        Vector2Int snappedCell = new Vector2Int(
            Mathf.RoundToInt((snappedX - gridOrigin.x) / gridSize),
            Mathf.RoundToInt((snappedZ - gridOrigin.y) / gridSize)
        );

        List<Vector2Int> newCells = new List<Vector2Int>();
        for (int x = 0; x < traySize.x; x++)
        {
            for (int y = 0; y < traySize.y; y++)
            {
                Vector2Int cell = new Vector2Int(snappedCell.x + x, snappedCell.y + y);

                //  check kr rahe hai yaha pe ki  agar cell grid ke andar hai )
                if (cell.x < 0 || cell.y < 0 || cell.x >= gridSizeXY.x || cell.y >= gridSizeXY.y)
                {
                    RestoreMyCells();
                    transform.position = originalPosition;
                    return;
                }

                //  (check kr rahe hai yaha pe ki  agar cell alredy occupide hai )
                if (occupiedCells.Contains(cell))
                {
                    RestoreMyCells();
                    transform.position = originalPosition;
                    return;
                }

                newCells.Add(cell);
            }
        }

        // Check kr rahe yaha pe ki physical overlap using updated bounding box logic
        if (IsOverlappingTray(new Vector3(snappedX, fixedY, snappedZ)))
        {
            RestoreMyCells();
            transform.position = originalPosition;
            return;
        }

        // age sb sahi se kaam kr raha hai to  OK — Snap tray and mark new occupied cells
        transform.position = new Vector3(snappedX, fixedY, snappedZ);
        myCells = newCells;
        foreach (var cell in myCells)
            occupiedCells.Add(cell);
    }

    // Updated overlap detection to allow tight fit with other trays
    bool IsOverlappingTray(Vector3 centerPosition)
    {
        float margin = 0.1f; // Slight margin allows trays to fit tightly

        Vector3 halfExtents = new Vector3(
            (traySize.x * gridSize * 0.5f) - margin,
            0.5f,
            (traySize.y * gridSize * 0.5f) - margin
        );

        Collider[] hits = Physics.OverlapBox(
            centerPosition,
            halfExtents,
            Quaternion.identity
        );

        foreach (var hit in hits)
        {
            if (hit.gameObject != this.gameObject && hit.CompareTag("Tray"))
            {
                return true;
            }
        }
        return false;
    }

    // Remove previously occupied grid cells
    void RemoveMyCells()
    {
        foreach (var cell in myCells)
            occupiedCells.Remove(cell);
        myCells.Clear();
    }

    // Restore the old cells in case of invalid placement
    void RestoreMyCells()
    {
        foreach (var cell in myCells)
            occupiedCells.Add(cell);
    }
}
