using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Referencias")]
    public Tilemap tilemap; // Asigna tu Tilemap en el inspector
    public PlacementManager placementManager; // referencia al PlacementManager

    private Vector3Int lastCellPosition;
    private bool hasHighlighted = false;

    void Update()
    {
        Vector3Int currentCell = GetMouseCellPosition();
        HandleHighlight(currentCell);
    }

    // =========================================
    // 🔹 Obtiene la celda actual del ratón
    // =========================================
    Vector3Int GetMouseCellPosition()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        return tilemap.WorldToCell(mouseWorldPos);
    }

    // =========================================
    // 🔹 Resalta la celda según si está libre u ocupada
    // =========================================
    void HandleHighlight(Vector3Int cellPosition)
    {
        if (cellPosition == lastCellPosition)
            return;

        // Restaurar color anterior
        if (hasHighlighted)
        {
            tilemap.SetTileFlags(lastCellPosition, TileFlags.None);
            tilemap.SetColor(lastCellPosition, Color.white);
        }

        // Aplicar nuevo resaltado si hay un tile
        if (tilemap.HasTile(cellPosition))
        {
            tilemap.SetTileFlags(cellPosition, TileFlags.None);

            bool cellOccupied = placementManager != null && placementManager.IsCellOccupied(cellPosition);

            // Blanco si libre, rojo si ocupada
            Color color = cellOccupied
                ? new Color(1f, 0.3f, 0.3f, 1f)
                : new Color(1f, 1f, 1f, 0.75f);

            tilemap.SetColor(cellPosition, color);
            hasHighlighted = true;
        }
        else
        {
            hasHighlighted = false;
        }

        lastCellPosition = cellPosition;
    }
}
