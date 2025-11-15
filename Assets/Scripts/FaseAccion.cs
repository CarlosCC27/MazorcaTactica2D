using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FaseAccion : MonoBehaviour
{
    public PlacementManager placementManager;

    [Header("Resaltado de celdas")]
    public float highlightDuration = 1.2f;
    public Color emptyCellColor = new Color(1f, 1f, 0f, 0.65f);
    public Color occupiedCellColor = new Color(1f, 0.25f, 0.25f, 0.85f);

    [Header("Visualización de ruta y movimiento")]
    public Color pathColor = new Color(0f, 1f, 1f, 0.7f);  // color para la ruta A*
    public float moveSpeed = 3f; // unidades por segundo (ajustar)
    public float stepThreshold = 0.01f; // umbral para considerar alcanzada una posición

    private readonly List<Vector3Int> currentHighlights = new List<Vector3Int>();
    private readonly List<Vector3Int> currentPathTiles = new List<Vector3Int>();

    // Selección actual
    private GameObject selectedAlly;
    private Coroutine moveCoroutine;

    void Update()
    {
        if (placementManager == null) return;
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

        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
        Vector3Int clickedCell = placementManager.tilemap.WorldToCell(mouseWorld);

        if (hit.collider != null)
        {
            GameObject clickedObject = hit.collider.gameObject;

            if (clickedObject.CompareTag("Aliado"))
            {
                Vector3Int cellPos = placementManager.tilemap.WorldToCell(clickedObject.transform.position);

                int rango = 1;
                var controladorTropa = clickedObject.GetComponent<ControladorTropa>();
                if (controladorTropa != null && controladorTropa.datosBase != null)
                {
                    rango = Mathf.Max(0, controladorTropa.datosBase.rangoMovimiento);
                    Debug.Log($"Unidad seleccionada: {controladorTropa.datosBase.nombreTropa}. Rango de Movimiento: {rango}");
                }
                else
                {
                    Debug.LogWarning("⚠️ No se pudo obtener ControladorTropa o TropaData. Usando rango por defecto (1).");
                }

                selectedAlly = clickedObject;
                ClearPathVisualization();
                HighlightAdjacentCells(cellPos, rango);
            }
            else
            {
                ClearHighlights();
                ClearPathVisualization();
                selectedAlly = null;
            }
        }
        else
        {
            // Clic en espacio vacío
            if (placementManager.tilemap.HasTile(clickedCell) && selectedAlly != null)
            {
                bool inRange = currentHighlights.Contains(clickedCell);
                bool free = !placementManager.IsCellOccupied(clickedCell);
                if (inRange && free)
                {
                    // Encontrar ruta A* desde la celda de la unidad hasta clickedCell
                    Vector3Int fromCell = placementManager.tilemap.WorldToCell(selectedAlly.transform.position);
                    List<Vector3Int> path = FindPathAStar(fromCell, clickedCell);

                    if (path != null && path.Count > 0)
                    {
                        // Visualizar ruta
                        ShowPathVisualization(path);

                        // Si ya hay una rutina de movimiento, detenerla
                        if (moveCoroutine != null) StopCoroutine(moveCoroutine);

                        // Iniciar movimiento animado
                        moveCoroutine = StartCoroutine(MoveAlongPath(selectedAlly, path));
                    }
                    else
                    {
                        Debug.Log("No se encontró ruta válida hasta la celda objetivo.");
                        // limpiamos selección/resaltado si quieres
                        // ClearHighlights();
                        // selectedAlly = null;
                    }
                }
                else
                {
                    ClearHighlights();
                    ClearPathVisualization();
                    selectedAlly = null;
                }
            }
            else
            {
                ClearHighlights();
                ClearPathVisualization();
                selectedAlly = null;
            }
        }
    }

    // ==========================
    // A* Pathfinding (4-dir)
    // ==========================
    List<Vector3Int> FindPathAStar(Vector3Int start, Vector3Int goal)
    {
        var tilemap = placementManager.tilemap;
        if (tilemap == null) return null;

        // Si goal no es tile válido o está ocupado, no hay ruta
        if (!tilemap.HasTile(goal)) return null;
        if (placementManager.IsCellOccupied(goal)) return null;

        // Nodo auxiliar
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, int> gScore = new Dictionary<Vector3Int, int>();
        Dictionary<Vector3Int, int> fScore = new Dictionary<Vector3Int, int>();

        var openSet = new List<Vector3Int>();
        openSet.Add(start);
        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);

        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0)
        };

        // Tratamiento: consideramos la celda 'start' como libre (para que la unidad pueda salir)
        while (openSet.Count > 0)
        {
            // escoger nodo de openSet con menor fScore
            Vector3Int current = openSet[0];
            int bestIdx = 0;
            for (int i = 1; i < openSet.Count; i++)
            {
                int fcur = fScore.ContainsKey(openSet[i]) ? fScore[openSet[i]] : int.MaxValue;
                int fbest = fScore.ContainsKey(current) ? fScore[current] : int.MaxValue;
                if (fcur < fbest)
                {
                    current = openSet[i];
                    bestIdx = i;
                }
            }
            openSet.RemoveAt(bestIdx);

            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (var dir in directions)
            {
                Vector3Int neighbor = current + dir;

                // 1) Debe existir tile
                if (!tilemap.HasTile(neighbor)) continue;

                // 2) No puede estar ocupado (excepto si es la celda start -> permitimos)
                bool occupied = placementManager.IsCellOccupied(neighbor);
                if (occupied && neighbor != start) continue;

                // 3) También consideramos obstáculos físicos por colliders (opcional)
                Vector3 neighborCenter = tilemap.GetCellCenterWorld(neighbor);
                Collider2D hit = Physics2D.OverlapPoint(neighborCenter);
                if (hit != null)
                {
                    continue;  // tratar cualquier collider como obstáculo
                }

                int tentativeG = gScore.ContainsKey(current) ? gScore[current] + 1 : int.MaxValue;
                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // No se encontró camino
        return null;
    }

    int Heuristic(Vector3Int a, Vector3Int b)
    {
        // Manhattan
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        var totalPath = new List<Vector3Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Add(current);
        }
        totalPath.Reverse();
        return totalPath;
    }

    // ==========================
    // Visualización de ruta
    // ==========================
    void ShowPathVisualization(List<Vector3Int> path)
    {
        ClearPathVisualization(); // limpiar anterior
        foreach (var cell in path)
        {
            placementManager.tilemap.SetTileFlags(cell, TileFlags.None);
            placementManager.tilemap.SetColor(cell, pathColor);
            currentPathTiles.Add(cell);
        }

        // Opcional: dibujar líneas para depuración
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 c1 = placementManager.tilemap.GetCellCenterWorld(path[i]);
            Vector3 c2 = placementManager.tilemap.GetCellCenterWorld(path[i + 1]);
            Debug.DrawLine(c1 + Vector3.up * 0.01f, c2 + Vector3.up * 0.01f, Color.cyan, 2f);
        }
    }

    void ClearPathVisualization()
    {
        if (placementManager == null || placementManager.tilemap == null) return;
        foreach (var cell in currentPathTiles)
        {
            if (placementManager.tilemap.HasTile(cell))
            {
                placementManager.tilemap.SetTileFlags(cell, TileFlags.None);
                placementManager.tilemap.SetColor(cell, Color.white);
            }
        }
        currentPathTiles.Clear();
    }

    // ==========================
    // Movimiento animado
    // ==========================
    IEnumerator MoveAlongPath(GameObject unit, List<Vector3Int> path)
    {
        if (unit == null || path == null || path.Count == 0 || placementManager == null) yield break;

        Tilemap tm = placementManager.tilemap;
        Vector3Int fromCell = tm.WorldToCell(unit.transform.position);

        // Antes de moverse, marcamos la celda de inicio como libre (la unidad la deja)
        placementManager.SetCellOccupied(fromCell, false);

        // Pero marcaremos las celdas por las que vamos entrando para reservarlas
        // Empezamos en el primer nodo de la ruta. Normalmente path[0] == fromCell
        int startIndex = 0;
        if (path[0] == fromCell) startIndex = 1; // saltamos la primera porque ya estamos ahí

        for (int i = startIndex; i < path.Count; i++)
        {
            Vector3Int nextCell = path[i];
            Vector3 targetWorld = tm.GetCellCenterWorld(nextCell);

            // Reservar la celda destino para evitar que otra unidad la ponga como libre
            placementManager.SetCellOccupied(nextCell, true);

            // Interpolación simple (MoveTowards) hasta alcanzar target
            while (Vector3.Distance(unit.transform.position, targetWorld) > stepThreshold)
            {
                unit.transform.position = Vector3.MoveTowards(unit.transform.position, targetWorld, moveSpeed * Time.deltaTime);
                yield return null;
            }

            // Aseguramos posición exacta
            unit.transform.position = targetWorld;

            // Liberar la celda anterior (porque ya hemos entrado en nextCell)
            Vector3Int prevCell = tm.WorldToCell(unit.transform.position); // este será nextCell
            // Para liberar la anterior necesitamos calcularla: es path[i-1] si i>0
            if (i - 1 >= 0)
            {
                Vector3Int prev = path[i - 1];
                if (prev != nextCell) // seguridad
                    placementManager.SetCellOccupied(prev, false);
            }
        }

        // Movimiento completado: limpiar visualización de ruta y resaltar
        ClearPathVisualization();
        ClearHighlights();
        selectedAlly = null;
        moveCoroutine = null;
    }

    // ==========================
    // Resaltado (tu BFS original)
    // ==========================
    void HighlightAdjacentCells(Vector3Int centerCell, int range)
    {
        ClearHighlights();
        if (placementManager == null || placementManager.tilemap == null) return;
        if (range <= 0) return;

        var tilemap = placementManager.tilemap;
        var visited = new HashSet<Vector3Int>();
        var queue = new Queue<Vector3Int>();
        var distance = new Dictionary<Vector3Int, int>();

        visited.Add(centerCell);
        distance[centerCell] = 0;
        queue.Enqueue(centerCell);

        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0)
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

                if (!isOccupied)
                {
                    queue.Enqueue(neighborCell);
                }
            }
        }
    }

    public void ClearHighlights()
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
        ClearPathVisualization();
        selectedAlly = null;
    }
}
