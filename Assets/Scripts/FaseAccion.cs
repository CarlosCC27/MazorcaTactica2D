using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class FaseAccion : MonoBehaviour
{
    public PlacementManager placementManager;

    // Config de resaltado
    [Header("Resaltado de celdas")]
    public float highlightDuration = 1.2f;
    public Color emptyCellColor = new Color(1f, 1f, 0f, 0.65f);     // Amarillo
    public Color occupiedCellColor = new Color(1f, 0.25f, 0.25f, 0.85f); // Rojo

    private readonly List<Vector3Int> currentHighlights = new List<Vector3Int>();
    private Coroutine clearCoroutine;

    // Selección actual
    private GameObject selectedAlly;

    void Update()
    {
        if (placementManager.fasePreparacion) return;
        if (Input.GetMouseButtonDown(0))
        {
            CheckClickedObject();
        }
    }

    void CheckClickedObject()
    {
        Debug.Log("Click detected in FaseAccion");
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        // Raycast para detectar objetos en la posición del mouse
        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
        Vector3Int clickedCell = placementManager.tilemap.WorldToCell(mouseWorld);
        if (hit.collider != null)
        {
            GameObject clickedObject = hit.collider.gameObject;

            if (clickedObject.CompareTag("Aliado"))
            {
                Vector3Int cellPos = placementManager.tilemap.WorldToCell(clickedObject.transform.position);

                int rango = 1;
                var stats = clickedObject.GetComponent<Stats>();
                if (stats != null)
                    rango = Mathf.Max(0, stats.movimiento);

                selectedAlly = clickedObject;
                HighlightAdjacentCells(cellPos, rango);
            }
            else
            {
                // Si no clicamos un aliado, limpiamos cualquier resaltado previo
                ClearHighlights();
                selectedAlly = null;
            }
        }
        else
        {
            // Si hay una celda válida y tenemos un aliado seleccionado, intentamos mover
            if (placementManager.tilemap.HasTile(clickedCell) && selectedAlly != null)
            {
                // Debe estar resaltada (alcanzable) y libre
                bool inRange = currentHighlights.Contains(clickedCell);
                bool free = !placementManager.IsCellOccupied(clickedCell);
                if (inRange && free)
                {
                    MoveSelectedAllyTo(clickedCell);
                    // Tras mover, limpiamos resaltados
                    ClearHighlights();
                }
                else
                {
                    // Clic fuera de rango o no libre -> deseleccionar y limpiar
                    ClearHighlights();
                    selectedAlly = null;
                }
            }
            else
            {
                // Click vacío fuera de contexto
                ClearHighlights();
                selectedAlly = null;
            }
        }
    }

    void HighlightAdjacentCells(Vector3Int centerCell, int range)
    {
        // BFS por distancia Manhattan hasta "range"
        ClearHighlights();
        if (placementManager == null || placementManager.tilemap == null) return;

        if (range <= 0)
        {
            return;
        }

        var tilemap = placementManager.tilemap;
        var visited = new HashSet<Vector3Int>();
        var queue = new Queue<Vector3Int>();
        var distance = new Dictionary<Vector3Int, int>();

        visited.Add(centerCell);
        distance[centerCell] = 0;
        queue.Enqueue(centerCell);

        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0),   // derecha
            new Vector3Int(-1, 0, 0),  // izquierda
            new Vector3Int(0, 1, 0),   // arriba
            new Vector3Int(0, -1, 0)   // abajo
        };

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            int d = distance[cell];

            foreach (var dir in directions)
            {
                var neighborCell = cell + dir;
                if (visited.Contains(neighborCell)) continue;
                if (!tilemap.HasTile(neighborCell)) continue;

                int nd = d + 1;
                if (nd > range) continue;

                bool isOccupied = placementManager.IsCellOccupied(neighborCell);

                tilemap.SetTileFlags(neighborCell, TileFlags.None);
                tilemap.SetColor(neighborCell, isOccupied ? occupiedCellColor : emptyCellColor);
                currentHighlights.Add(neighborCell);

                visited.Add(neighborCell);
                distance[neighborCell] = nd;

                // Solo expandimos a través de celdas libres
                if (!isOccupied)
                {
                    queue.Enqueue(neighborCell);
                }
            }
        }

        // Persisten hasta siguiente clic; no limpiar automáticamente
    }

    IEnumerator ClearHighlightsAfterDelay()
    {
        yield return new WaitForSeconds(highlightDuration);
        ClearHighlights();
        clearCoroutine = null;
    }

    void ClearHighlights()
    {
        if (placementManager == null || placementManager.tilemap == null) return;
        foreach (var cell in currentHighlights)
        {
            placementManager.tilemap.SetTileFlags(cell, TileFlags.None);
            placementManager.tilemap.SetColor(cell, Color.white);
        }
        currentHighlights.Clear();
    }

    void OnDisable()
    {
        ClearHighlights();
        selectedAlly = null;
    }

    void MoveSelectedAllyTo(Vector3Int targetCell)
    {
        if (selectedAlly == null) return;
        var tm = placementManager.tilemap;
        Vector3Int fromCell = tm.WorldToCell(selectedAlly.transform.position);
        Vector3 fromCenter = tm.GetCellCenterWorld(fromCell);
        Vector3 toCenter = tm.GetCellCenterWorld(targetCell);

        // Mantener el mismo offset visual relativo al centro de celda
        Vector3 offset = selectedAlly.transform.position - fromCenter;
        selectedAlly.transform.position = toCenter + offset;

        // Actualizar ocupación
        if (placementManager != null)
        {
            placementManager.SetCellOccupied(fromCell, false);
            placementManager.SetCellOccupied(targetCell, true);
        }
    }
}
