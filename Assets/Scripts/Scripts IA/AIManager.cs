using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance;

    [Header("Refs")]
    public PlacementManager placementManager;
    public InfluenceMap influenceMap;
    public Tilemap tilemap;

    public bool isAITurn = false;
    public Vector3Int enemyBaseCell = Vector3Int.zero;

    // Prefabs y ajuste de spawn
    [Header("Spawn IA")]
    public GameObject soldierPrefab;
    public GameObject archerPrefab;
    public int maxSpawnSoldiers = 3;
    public int maxSpawnArchers = 2;
    [Tooltip("Capas que marcan ocupación de unidades/estructuras")]
    public LayerMask unitLayer;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (placementManager == null) placementManager = FindObjectOfType<PlacementManager>();
        if (influenceMap == null) influenceMap = FindObjectOfType<InfluenceMap>();
        if (tilemap == null && placementManager != null) tilemap = placementManager.tilemap;
    }

    public void StartAITurn()
    {
        StartCoroutine(RunAITurn());
    }

    IEnumerator RunAITurn()
    {
        isAITurn = true;
        Debug.Log("[AIManager] Inicio turno IA");

        // 1) Recolectar estado del mundo
        var aiUnits = GameObject.FindGameObjectsWithTag("Enemy").ToList();
        var playerUnits = GameObject.FindGameObjectsWithTag("Aliado").ToList();

        // 2) Calcular Influence Map
        if (influenceMap != null)
        {
            influenceMap.tilemap = tilemap;
            influenceMap.Compute(aiUnits, playerUnits);
            // opcional: visualizar para debug
            // influenceMap.DebugDrawInfluence(1f);
        }

        // 3) Decidir estrategia usando AIGlobal
        var aiGlobal = AIGlobal.Instance ?? FindObjectOfType<AIGlobal>();
        if (aiGlobal != null)
        {
            aiGlobal.DecideStrategy(aiUnits.Count, playerUnits.Count);
            Debug.Log($"[AIManager] Estrategia decidida: {aiGlobal.currentStrategy}");
        }
        else
        {
            Debug.LogWarning("[AIManager] AIGlobal no encontrado. Usando estrategia Equilibrada por defecto.");
        }

        // 4) Fase Colocación (spawns IA si agresiva)
        yield return StartCoroutine(PlacementPhase(playerUnits, aiGlobal));

        // IMPORTANTE: actualizar lista de unidades tras spawns y habilitar movimiento
        aiUnits = GameObject.FindGameObjectsWithTag("Enemy").ToList();
        if (placementManager != null)
        {
            placementManager.ResetAllEnemyUnitsMovementFlags();
        }
        else
        {
            // Fallback: intentar habilitar movimiento en cada unidad
            foreach (var u in aiUnits)
            {
                var ctrl = u != null ? u.GetComponent<ControladorTropa>() : null;
                if (ctrl == null) continue;
                // Si tu ControladorTropa tiene un método específico para resetear movimiento, llámalo aquí.
                // Por ejemplo: ctrl.ResetMovementForNewTurn();
            }
        }

        // 5) Fase Movimiento
        yield return StartCoroutine(MovementPhase(aiUnits, playerUnits));

        // 6) Fase Ataque
        yield return StartCoroutine(AttackPhase(aiUnits));

        isAITurn= false;
        // 7) Fin de turno IA: restaurar cosas y devolver turno al jugador
        OnEndTurn();

        yield break;
    }

    // Colocación con spawn de nuevas tropas si estrategia Agresiva
    IEnumerator PlacementPhase(List<GameObject> playerUnits, AIGlobal aiGlobal)
    {
        // Si no hay estrategia global o no es agresiva, no spawnear
        if (aiGlobal == null || aiGlobal.currentStrategy != AIStrategy.Agresiva)
        {
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        // Validar prefabs
        if (soldierPrefab == null || archerPrefab == null || tilemap == null)
        {
            Debug.LogWarning("[AIManager] Prefabs o tilemap no configurados para spawn IA.");
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        // Calcular banda izquierda (6 columnas)
        var bounds = tilemap.cellBounds;
        int leftMinX = bounds.xMin;
        int leftMaxX = Mathf.Min(bounds.xMin + 5, bounds.xMax - 1); // 6 columnas: xMin..xMin+5

        // Centroid de tropas del jugador para orientar filas
        float playerCentroidY = 0f;
        int pCount = 0;
        foreach (var p in playerUnits)
        {
            if (p == null) continue;
            playerCentroidY += tilemap.WorldToCell(p.transform.position).y;
            pCount++;
        }
        playerCentroidY = (pCount > 0) ? playerCentroidY / pCount : (bounds.yMin + bounds.yMax) * 0.5f;

        // Construir lista de celdas candidatas en banda izquierda
        List<Vector3Int> candidateFront = new List<Vector3Int>(); // columnas más a la derecha de la banda (más “delante” hacia el jugador)
        List<Vector3Int> candidateBack = new List<Vector3Int>();  // columnas más a la izquierda de la banda (más “detrás”)
        for (int y = bounds.yMin; y <= bounds.yMax; y++)
        {
            // Ordenar por cercanía al centroid Y del jugador
            candidateFront.Add(new Vector3Int(Mathf.Min(leftMinX + 5, leftMaxX), y, 0));
            candidateFront.Add(new Vector3Int(Mathf.Min(leftMinX + 4, leftMaxX), y, 0));
            candidateBack.Add(new Vector3Int(leftMinX + 2, y, 0));
            candidateBack.Add(new Vector3Int(leftMinX + 1, y, 0));
        }
        // Ordenar listas por |y - playerCentroidY|
        candidateFront = candidateFront.OrderBy(c => Mathf.Abs(c.y - playerCentroidY)).ToList();
        candidateBack = candidateBack.OrderBy(c => Mathf.Abs(c.y - playerCentroidY)).ToList();

        // Funciones locales
        bool IsCellFree(Vector3Int cell)
        {
            if (tilemap == null) return false;
            if (!tilemap.HasTile(cell)) return false;

            // No colocar sobre la base enemiga
            if (cell == enemyBaseCell) return false;

            // Preferir la verdad del PlacementManager (ocupación lógica)
            if (placementManager != null && placementManager.IsCellOccupied(cell)) return false;

            // Como respaldo, comprobar colliders (por si hay objetos no registrados en PlacementManager)
            var world = tilemap.GetCellCenterWorld(cell);
            var hit = Physics2D.OverlapPoint(world, unitLayer);
            return hit == null;
        }

        GameObject SpawnUnit(GameObject prefab, Vector3Int cell)
        {
            var world = tilemap.GetCellCenterWorld(cell);
            var go = Instantiate(prefab, world, Quaternion.identity);
            if (!go.CompareTag("Enemy")) go.tag = "Enemy";

            // Marcar celda ocupada en PlacementManager
            if (placementManager != null) placementManager.SetCellOccupied(cell, true);

            return go;
        }

        int spawnedSoldiers = 0;
        int spawnedArchers = 0;

        // Spawn soldados delante (prioriza celdas front libres)
        foreach (var cell in candidateFront)
        {
            if (spawnedSoldiers >= maxSpawnSoldiers) break;
            // Solo dentro de banda izquierda y celda libre
            if (cell.x < leftMinX || cell.x > leftMaxX) continue;
            if (!IsCellFree(cell)) continue;

            SpawnUnit(soldierPrefab, cell);
            spawnedSoldiers++;
            yield return new WaitForSeconds(0.05f);
        }

        // Spawn arqueros detrás (prioriza celdas back libres)
        foreach (var cell in candidateBack)
        {
            if (spawnedArchers >= maxSpawnArchers) break;
            if (cell.x < leftMinX || cell.x > leftMaxX) continue;
            if (!IsCellFree(cell)) continue;

            SpawnUnit(archerPrefab, cell);
            spawnedArchers++;
            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log($"[AIManager] Spawn IA: Soldados={spawnedSoldiers}/{maxSpawnSoldiers}, Arqueros={spawnedArchers}/{maxSpawnArchers}");
        yield break;
    }

    // Devuelve la siguiente celda (un paso) desde 'fromCell' hacia 'toCell'
    private Vector3Int GetNextStepTowards(Vector3Int fromCell, Vector3Int toCell)
    {
        Vector3Int step = fromCell;

        int dx = toCell.x - fromCell.x;
        int dy = toCell.y - fromCell.y;

        // Prioriza avanzar en el eje con mayor distancia, pero sólo 1 celda
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            step.x += Mathf.Clamp(dx, -1, 1);
        }
        else
        {
            step.y += Mathf.Clamp(dy, -1, 1);
        }

        // Mantener z = 0
        step.z = 0;

        // Validación: si no hay tile o está ocupada, intenta el otro eje
        if (tilemap != null && (!tilemap.HasTile(step) ||
            (placementManager != null && placementManager.IsCellOccupied(step))))
        {
            // Intentar el eje alternativo
            Vector3Int alt = fromCell;
            if (Mathf.Abs(dx) < Mathf.Abs(dy))
                alt.x += Mathf.Clamp(dx, -1, 1);
            else
                alt.y += Mathf.Clamp(dy, -1, 1);

            alt.z = 0;

            if (tilemap != null && tilemap.HasTile(alt) &&
                (placementManager == null || !placementManager.IsCellOccupied(alt)))
            {
                step = alt;
            }
            else
            {
                // Si ambos fallan, no moverse (se enviará Mantener)
                step = fromCell;
            }
        }

        return step;
    }

    // Devuelve la celda de la unidad del jugador más cercana a 'fromCell' (ignora estructuras)
    private Vector3Int? GetNearestPlayerUnitCell(Vector3Int fromCell)
    {
        if (tilemap == null) return null;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Aliado");
        Vector3Int? best = null;
        float bestDist = float.MaxValue;

        foreach (var p in players)
        {
            if (p == null) continue;
            var pCtrl = p.GetComponent<ControladorTropa>();
            if (pCtrl != null && pCtrl.datosBase != null && pCtrl.datosBase.esEstructura) continue;

            Vector3Int c = tilemap.WorldToCell(p.transform.position);
            float d = Vector3Int.Distance(fromCell, c);
            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }
        return best;
    }

    // Celdas alrededor (anillo) de una celda objetivo para cualquier radio > 0 (incluye diagonales)
    private IEnumerable<Vector3Int> GetRingCells(Vector3Int center, int radius)
    {
        if (tilemap == null || radius <= 0) yield break;
        // Genera perimetro cuadrado de lado 2*radius alrededor del centro
        for (int dx = -radius; dx <= radius; dx++)
        {
            int x1 = center.x + dx;
            int y1 = center.y + radius;
            int y2 = center.y - radius;

            var c1 = new Vector3Int(x1, y1, 0);
            var c2 = new Vector3Int(x1, y2, 0);
            if (tilemap.HasTile(c1)) yield return c1;
            if (radius != 0 && tilemap.HasTile(c2)) yield return c2;
        }
        for (int dy = -radius + 1; dy <= radius - 1; dy++)
        {
            int y = center.y + dy;
            int x1 = center.x + radius;
            int x2 = center.x - radius;

            var c1 = new Vector3Int(x1, y, 0);
            var c2 = new Vector3Int(x2, y, 0);
            if (tilemap.HasTile(c1)) yield return c1;
            if (tilemap.HasTile(c2)) yield return c2;
        }
    }

    // Evalúa una celda candidata: mayor score = mejor para IA
    private float ScoreCellForAI(Vector3Int candidate, Vector3Int aiFrom, Vector3Int playerCenter)
    {
        if (influenceMap == null) return -Mathf.Infinity;

        // Preferir alta influencia para IA, menor distancia al jugador, y que acerque a la unidad actual
        float net = influenceMap.GetNetInfluence(candidate);
        float distToPlayer = Vector3Int.Distance(candidate, playerCenter);
        float distFromAI = Vector3Int.Distance(aiFrom, candidate);

        // Ajuste de pesos: influencia fuerte, proximidad al jugador, y evitar pasos demasiado largos innecesarios
        float score = net * 2.0f - distToPlayer * 1.0f - distFromAI * 0.25f;
        return score;
    }

    // Selecciona mejor celda alrededor del objetivo (jugador o base) expandiendo radio hasta encontrar libre con buen score
    private Vector3Int? PickBestSurroundCell(Vector3Int aiFrom, Vector3Int targetCell)
    {
        if (tilemap == null) return null;

        Vector3Int? best = null;
        float bestScore = -Mathf.Infinity;

        // Expandir radios 1..4 (ajusta si tu mapa necesita más)
        for (int radius = 1; radius <= 4; radius++)
        {
            bool foundAnyInThisRing = false;

            foreach (var c in GetRingCells(targetCell, radius))
            {
                // evita colocar sobre la propia base de la IA o celdas ocupadas
                if (c == enemyBaseCell) continue;
                if (placementManager != null && placementManager.IsCellOccupied(c)) continue;

                var world = tilemap.GetCellCenterWorld(c);
                if (Physics2D.OverlapPoint(world, unitLayer)) continue;

                float s = ScoreCellForAI(c, aiFrom, targetCell);
                if (s > bestScore)
                {
                    bestScore = s;
                    best = c;
                    foundAnyInThisRing = true;
                }
            }

            // si ya hemos encontrado alguna celda buena en este radio, podemos romper
            if (foundAnyInThisRing) break;
        }

        return best;
    }

    IEnumerator MovementPhase(List<GameObject> aiUnits, List<GameObject> playerUnits)
    {
        // Condición: agresiva y IA >= 2x jugador
        bool rushBase = (AIGlobal.Instance?.currentStrategy ?? AIStrategy.Equilibrada) == AIStrategy.Agresiva
                        && aiUnits.Count >= playerUnits.Count * 2;

        // Si no hay tropas del jugador, todos a por la base
        bool noPlayerTroops = playerUnits == null || playerUnits.Count == 0;
        if (noPlayerTroops) rushBase = true;

        // Celda de la base del jugador (enemiga para la IA)
        Vector3Int playerBaseCell = Vector3Int.zero;
        if (placementManager != null)
            playerBaseCell = placementManager.GetBaseCell(buscarAliada: true);

        // Precomputar distancia de cada unidad IA al jugador más cercano (si hay)
        var distToNearestPlayer = new Dictionary<GameObject, float>();
        foreach (var u in aiUnits)
        {
            if (u == null) continue;
            Vector3Int myCell = tilemap.WorldToCell(u.transform.position);
            var nearestCell = GetNearestPlayerUnitCell(myCell);
            float d = nearestCell.HasValue ? Vector3Int.Distance(myCell, nearestCell.Value) : Mathf.Infinity;
            distToNearestPlayer[u] = d;
        }

        float avgDist = distToNearestPlayer.Count > 0 ? distToNearestPlayer.Values.Average() : 0f;

        foreach (var u in aiUnits)
        {
            if (u == null) continue;
            var ctrl = u.GetComponent<ControladorTropa>();
            if (ctrl == null) continue;

            // No mover si es estructura
            if (ctrl.datosBase != null && ctrl.datosBase.esEstructura) continue;
            if (!ctrl.CanMoveThisTurn()) continue;

            Vector3Int myCell = tilemap.WorldToCell(u.transform.position);
            int rangoMovimiento = (ctrl.datosBase != null && ctrl.datosBase.rangoMovimiento > 0)
                                  ? ctrl.datosBase.rangoMovimiento
                                  : 1;

            TacticalOrder order;

            // Decidir objetivo principal: base si rushBase o no hay tropas del jugador
            bool isFar = rushBase && distToNearestPlayer.TryGetValue(u, out float dval) && dval > avgDist;
            if ((rushBase && isFar) || noPlayerTroops)
            {
                if (tilemap.HasTile(playerBaseCell))
                {
                    var surroundBaseTarget = PickBestSurroundCell(myCell, playerBaseCell) ?? playerBaseCell;
                    Vector3Int targetStep = GetStepTowardsWithRange(myCell, surroundBaseTarget, rangoMovimiento);

                    order = (targetStep == myCell)
                            ? new TacticalOrder(TacticalOrderType.Mantener)
                            : new TacticalOrder(TacticalOrderType.MoverHaciaZona, targetStep);
                }
                else
                {
                    order = new TacticalOrder(TacticalOrderType.Mantener);
                }
            }
            else
            {
                // Rodear a la unidad del jugador más cercana
                var nearestCell = GetNearestPlayerUnitCell(myCell);
                if (nearestCell.HasValue)
                {
                    var surroundTarget = PickBestSurroundCell(myCell, nearestCell.Value) ?? nearestCell.Value;
                    Vector3Int targetStep = GetStepTowardsWithRange(myCell, surroundTarget, rangoMovimiento);

                    order = (targetStep == myCell)
                            ? new TacticalOrder(TacticalOrderType.Mantener)
                            : new TacticalOrder(TacticalOrderType.MoverHaciaZona, targetStep);
                }
                else
                {
                    // Fallback: avanzar hacia la derecha rangoMovimiento pasos
                    Vector3Int desired = myCell + new Vector3Int(rangoMovimiento, 0, 0);
                    Vector3Int targetStep = GetStepTowardsWithRange(myCell, desired, rangoMovimiento);

                    order = (tilemap != null && tilemap.HasTile(targetStep) &&
                             (placementManager == null || !placementManager.IsCellOccupied(targetStep)))
                            ? new TacticalOrder(TacticalOrderType.MoverHaciaZona, targetStep)
                            : new TacticalOrder(TacticalOrderType.Mantener);
                }
            }

            ctrl.ReceiveTacticalOrder(order);
            yield return new WaitForSeconds(0.05f);
        }
    }

    // Helper: devuelve el GameObject de la base del jugador
    private GameObject GetPlayerBaseGO()
    {
        var bases = FindObjectsOfType<BaseMarker>();
        foreach (var b in bases)
        {
            var ctrl = b.GetComponent<ControladorTropa>();
            if (ctrl != null && ctrl.datosBase != null && !ctrl.datosBase.esEnemigo)
                return b.gameObject;
        }
        return null;
    }

    IEnumerator AttackPhase(List<GameObject> aiUnits)
    {
        // Usar FaseAtaque para ejecutar ataques: llamamos a wrapper público PerformAttackForAI
        var faseAtaque = FindObjectOfType<FaseAtaque>();

        foreach (var u in aiUnits)
        {
            if (u == null) continue;
            var ctrl = u.GetComponent<ControladorTropa>();
            if (ctrl == null) continue;

            // No atacar si es estructura
            if (ctrl.datosBase != null && ctrl.datosBase.esEstructura) continue;

            int rango = ctrl.datosBase != null ? ctrl.datosBase.rangoAtaque : 0;
            if (rango <= 0) continue;

            // Encontrar objetivo cercano en rango (simple búsqueda)
            Vector3Int center = tilemap.WorldToCell(u.transform.position);
            Vector3 centerWorld = tilemap.GetCellCenterWorld(center);
            Collider2D[] hits = Physics2D.OverlapCircleAll(centerWorld, rango + 0.1f);

            GameObject target = null;
            foreach (var h in hits)
            {
                if (h == null) continue;
                // Priorizar tropas del jugador
                if (h.gameObject.CompareTag("Aliado"))
                {
                    target = h.gameObject;
                    break;
                }
            }

            // Si no hay tropas del jugador en rango, intentar atacar la base del jugador
            if (target == null)
            {
                var baseGO = GetPlayerBaseGO();
                if (baseGO != null)
                {
                    // comprobar distancia al centro de la celda de la base
                    Vector3Int baseCell = tilemap.WorldToCell(baseGO.transform.position);
                    Vector3 baseWorld = tilemap.GetCellCenterWorld(baseCell);
                    float dist = Vector2.Distance(centerWorld, baseWorld);
                    if (dist <= rango + 0.1f)
                    {
                        target = baseGO;
                    }
                }
            }

            if (target != null)
            {
                if (faseAtaque != null)
                {
                    faseAtaque.PerformAttackForAI(u, target);
                    yield return new WaitForSeconds(0.25f);
                }
                else
                {
                    // fallback directo de daño:
                    var ctrlT = target.GetComponent<ControladorTropa>();
                    int damage = Mathf.Max(1, ctrl.datosBase.ataque - (ctrlT != null ? ctrlT.datosBase.defensa : 0));
                    if (ctrlT != null) ctrlT.TakeDamage(damage);
                    yield return new WaitForSeconds(0.25f);
                }
            }
        }
 
        yield break;
    }

    void OnEndTurn()
    {
        Debug.Log("[AIManager] Turno IA completado. Devolviendo control al jugador.");

        // Restaurar flags/opacidad de unidades (si corresponde)
        if (placementManager != null)
        {
            placementManager.RestoreOpacityOfAllUnits();
            placementManager.ResetAllUnitsMovementFlags();
            placementManager.ResetAllEnemyUnitsMovementFlags();
        }

        // Notificar al PlacementManager que el turno IA terminó (para reactivar fasePreparacion y UI)
        if (placementManager != null)
        {
            placementManager.OnAIEndTurn();
        }
    }

    public TacticalOrder GetOrderForUnit(GameObject unit)
    {
        var ctrl = unit.GetComponent<ControladorTropa>();
        if (ctrl == null) return new TacticalOrder(TacticalOrderType.Mantener);

        Vector3Int cell = tilemap.WorldToCell(unit.transform.position);

        // 1) leer influencia en la casilla
        float net = influenceMap.GetNetInfluence(cell);

        // 2) leer estrategia global (fallback a Equilibrada si no hay AIGlobal)
        var strat = (AIGlobal.Instance != null) ? AIGlobal.Instance.currentStrategy : AIStrategy.Equilibrada;

        switch (strat)
        {
            case AIStrategy.Agresiva:
                // si el mapa favorece IA -> avanzar
                if (net > 0) return new TacticalOrder(TacticalOrderType.Avanzar);
                else return new TacticalOrder(TacticalOrderType.MoverHaciaZona, GetNearestWeakPlayerZone());

            case AIStrategy.Defensiva:
                // si estamos en mala zona -> retroceder
                if (net < -5) return new TacticalOrder(TacticalOrderType.Retroceder);
                else return new TacticalOrder(TacticalOrderType.Mantener);

            case AIStrategy.Equilibrada:
            default:
                if (net > 3) return new TacticalOrder(TacticalOrderType.Avanzar);
                if (net < -3) return new TacticalOrder(TacticalOrderType.Retroceder);
                return new TacticalOrder(TacticalOrderType.Mantener);
        }
    }

    Vector3Int GetNearestWeakPlayerZone()
    {
        // placeholder: devuelve aleatoria cercana
        return tilemap.WorldToCell(transform.position) + new Vector3Int(1, 0, 0);
    }

    // Calcula la mejor celda alcanzable en 'maxSteps' hacia 'toCell' (avanza paso a paso validando cada celda)
    private Vector3Int GetStepTowardsWithRange(Vector3Int fromCell, Vector3Int toCell, int maxSteps)
    {
        if (maxSteps <= 0) return fromCell;

        Vector3Int current = fromCell;
        for (int i = 0; i < maxSteps; i++)
        {
            Vector3Int next = GetNextStepTowards(current, toCell);
            if (next == current) break; // no hay paso válido, detener
            current = next;
        }
        return current;
    }

}
//