using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    public Tilemap tilemap; // Asigna tu Tilemap en el inspector
    private Vector3Int lastCellPosition;
    private bool hasHighlighted = false;

    void Update()
    {
        // Convertir la posición del ratón a coordenadas del mundo
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Convertir posición del mundo a celda del grid
        Vector3Int cellPosition = tilemap.WorldToCell(mouseWorldPos);

        // Si el ratón se ha movido a una nueva celda
        if (cellPosition != lastCellPosition)
        {
            // Restaurar el color anterior si había una celda destacada
            if (hasHighlighted)
                tilemap.SetTileFlags(lastCellPosition, TileFlags.None); // Permitir cambios
                tilemap.SetColor(lastCellPosition, Color.white);

            // Comprobar si la celda actual tiene un tile
            if (tilemap.HasTile(cellPosition))
            {
                tilemap.SetTileFlags(cellPosition, TileFlags.None);
                tilemap.SetColor(cellPosition, new Color(1f, 1f, 1f, 0.5f)); // Blanco semitransparente
                hasHighlighted = true;
            }
            else
            {
                hasHighlighted = false;
            }

            lastCellPosition = cellPosition;
        }
    }
}