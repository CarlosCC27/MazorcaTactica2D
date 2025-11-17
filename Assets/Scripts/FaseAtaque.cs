using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class FaseAtaque : MonoBehaviour
{
    public PlacementManager placementManager;

    [Header("Visual")]
    public Color attackRangeColor = new Color(1f, 0.5f, 0.2f, 0.65f); // color para rango de ataque
    public Color enemyHighlightColor = new Color(1f, 0f, 0f, 0.85f);

    // Estado
    private List<GameObject> playerUnitsToAct = new List<GameObject>(); // cola de unidades amigas que aún no han atacado
    private int currentIndex = -1; // unidad actual
    private readonly List<Vector3Int> rangeHighlights = new List<Vector3Int>();
    private readonly List<Vector3Int> enemyHighlights = new List<Vector3Int>();

    private GameObject currentUnit;

    void OnEnable()
    {
        StartPlayerAttackPhase();
    }

    // Llamar para iniciar la fase de ataque del jugador
    public void StartPlayerAttackPhase()
    {
        ClearAllHighlights();
        playerUnitsToAct.Clear();
        currentIndex = -1;
        currentUnit = null;

        // Recolectar todas las unidades amigas en escena (tag "Aliado")
        var allies = GameObject.FindGameObjectsWithTag("Aliado");
        foreach (var a in allies)
        {
            var ctrl = a.GetComponent<ControladorTropa>();
            if (ctrl != null && ctrl.IsAlive())
            {
                // Si tiene rangoAtaque > 0 añadimos a la lista; si quieres que unidades sin ataque también pasen, quítalo
                playerUnitsToAct.Add(a);
            }
        }

        // Si no hay unidades, completamos inmediatamente
        if (playerUnitsToAct.Count == 0)
        {
            Debug.Log("No hay unidades para atacar. Terminando fase de ataque jugador.");
            OnPlayerAttackPhaseComplete();
            return;
        }

        // Iniciar con la primera unidad
        currentIndex = 0;
        BeginTurnForUnit(playerUnitsToAct[currentIndex]);
    }

    void Update()
    {
        if (currentUnit == null) return;

        // Click izquierdo: intentar seleccionar un enemigo dentro de rango
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

            if (hit.collider != null)
            {
                GameObject clicked = hit.collider.gameObject;
                if (clicked.CompareTag("Enemy"))
                {
                    // Está dentro del rango? (si no, ignoramos)
                    Vector3Int enemyCell = placementManager.tilemap.WorldToCell(clicked.transform.position);
                    if (rangeHighlights.Contains(enemyCell))
                    {
                        // Realizar ataque
                        PerformAttack(currentUnit, clicked);
                        EndCurrentUnitTurn();
                    }
                    else
                    {
                        Debug.Log("Enemigo fuera de rango de la unidad actual.");
                    }
                }
            }
        }

        // Click derecho: saltar la acción de esta unidad
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("Acción saltada para unidad actual.");
            EndCurrentUnitTurn();
        }
    }

    void BeginTurnForUnit(GameObject unit)
    {
        ClearAllHighlights();
        currentUnit = unit;

        if (currentUnit == null)
        {
            EndCurrentUnitTurn();
            return;
        }

        var ctrl = currentUnit.GetComponent<ControladorTropa>();
        if (ctrl == null || !ctrl.IsAlive())
        {
            EndCurrentUnitTurn(); // si la unidad murió entre tanto
            return;
        }

        int rangoAtaque = Mathf.Max(0, ctrl.datosBase.rangoAtaque);
        Debug.Log($"Turno de ataque: {ctrl.datosBase.nombreTropa}. Rango de ataque: {rangoAtaque}");

        // Resaltar celdas en rango y detectar enemigos en ellas
        HighlightAttackRangeAndEnemies(currentUnit, rangoAtaque);
    }

    void HighlightAttackRangeAndEnemies(GameObject unit, int rango)
{
    ClearRangeHighlights();
    if (placementManager == null || placementManager.tilemap == null) return;
    if (rango <= 0) return;

    Vector3Int centerCell = placementManager.tilemap.WorldToCell(unit.transform.position);
    var tilemap = placementManager.tilemap;

    Debug.Log($"[FaseAtaque] Begin highlight for unit '{unit.name}' at cell {centerCell} with rango {rango}");

    // Si quieres que también se marque la celda donde está la unidad, descomenta:
    // tilemap.SetTileFlags(centerCell, TileFlags.None);
    // tilemap.SetColor(centerCell, attackRangeColor);
    // rangeHighlights.Add(centerCell);

    var visited = new HashSet<Vector3Int>();
    var queue = new Queue<Vector3Int>();
    var distance = new Dictionary<Vector3Int, int>();

    visited.Add(centerCell);
    distance[centerCell] = 0;
    queue.Enqueue(centerCell);

    Vector3Int[] directions = new Vector3Int[]
    {
        new Vector3Int(1,0,0),
        new Vector3Int(-1,0,0),
        new Vector3Int(0,1,0),
        new Vector3Int(0,-1,0)
    };

    while (queue.Count > 0)
    {
        var cell = queue.Dequeue();
        int d = distance[cell];

        foreach (var dir in directions)
        {
            var neighbor = cell + dir;
            if (visited.Contains(neighbor)) continue;

            // asegurarse de que existe tile en esa celda
            if (!tilemap.HasTile(neighbor))
            {
                visited.Add(neighbor);
                continue;
            }

            int nd = d + 1;
            if (nd > rango)
            {
                visited.Add(neighbor);
                continue;
            }

            // Resaltar la celda de alcance
            tilemap.SetTileFlags(neighbor, TileFlags.None);
            tilemap.SetColor(neighbor, attackRangeColor);
            rangeHighlights.Add(neighbor);
            Debug.Log($"[FaseAtaque] Resalto celda {neighbor}");

            // Detección: buscar todos los colliders cerca del centro y filtrar:
            Vector3 neighborCenter = tilemap.GetCellCenterWorld(neighbor);

            // Usamos OverlapCircleAll para no perdernos nada; filtramos luego
            Collider2D[] hits = Physics2D.OverlapCircleAll(neighborCenter, 0.18f);
            foreach (var h in hits)
            {
                if (h == null) continue;
                GameObject hitObj = h.gameObject;

                // IGNORAR a la propia unidad (evita que te detectes a ti mismo si el collider cubre el centro)
                if (hitObj == unit) continue;

                // Solo aceptar objetos con tag "Enemy"
                if (hitObj.CompareTag("Enemy"))
                {
                    tilemap.SetTileFlags(neighbor, TileFlags.None);
                    tilemap.SetColor(neighbor, enemyHighlightColor);
                    if (!enemyHighlights.Contains(neighbor))
                        enemyHighlights.Add(neighbor);

                    // Intentar obtener ControladorTropa para leer la vida actual
                    var ctrlEnem = hitObj.GetComponent<ControladorTropa>();
                    if (ctrlEnem != null)
                    {
                        Debug.Log($"[FaseAtaque] Enemigo detectado en {neighbor}: {hitObj.name} | Vida actual: {ctrlEnem.GetSaludActual()}");
                    }
                    else
                    {
                    Debug.Log($"[FaseAtaque] Enemigo detectado en {neighbor}: {hitObj.name} | No tiene ControladorTropa");
                    }
                }
                else
                {
                    Debug.Log($"[FaseAtaque] Collider en {neighbor} -> {hitObj.name} (tag={hitObj.tag})");
                }
            }

            visited.Add(neighbor);
            distance[neighbor] = nd;
            queue.Enqueue(neighbor);
        }
    }
}



    void ClearRangeHighlights()
    {
        if (placementManager == null || placementManager.tilemap == null) return;
        foreach (var cell in rangeHighlights.Concat(enemyHighlights))
        {
            if (placementManager.tilemap.HasTile(cell))
            {
                placementManager.tilemap.SetTileFlags(cell, TileFlags.None);
                placementManager.tilemap.SetColor(cell, Color.white);
            }
        }
        rangeHighlights.Clear();
        enemyHighlights.Clear();
    }

    void ClearAllHighlights()
    {
        ClearRangeHighlights();
    }

    void PerformAttack(GameObject attacker, GameObject target)
    {
        if (attacker == null || target == null) return;
         StartCoroutine(AttackMoveAnimation(attacker, target.transform.position));

        var ctrlAtt = attacker.GetComponent<ControladorTropa>();
        var ctrlTgt = target.GetComponent<ControladorTropa>();
        if (ctrlAtt == null || ctrlTgt == null)
        {
            Debug.LogWarning("ControladorTropa faltante en atacante o objetivo.");
            return;
        }

        // Calcular daño simple: ataque base del atacante - defensa del objetivo (mínimo 1)
        int damage = Mathf.Max(1, ctrlAtt.datosBase.ataque - ctrlTgt.datosBase.defensa);
        Debug.Log($"{ctrlAtt.datosBase.nombreTropa} ataca a {ctrlTgt.datosBase.nombreTropa} por {damage} daño.");

        ctrlTgt.TakeDamage(damage);

        // Aquí animaciones, efectos, sonido, etc.

        // --- debug: vida restante o muerte ---
        if (ctrlTgt != null)
        {
            if (ctrlTgt.IsAlive())
            {
                Debug.Log($"[FaseAtaque] Vida restante de {ctrlTgt.datosBase.nombreTropa} ({target.name}): {ctrlTgt.GetSaludActual()}");
            }
            else
            {
            Debug.Log($"[FaseAtaque] {ctrlTgt.datosBase.nombreTropa} ({target.name}) ha muerto.");
            }
        }
        else
        {
            Debug.Log($"[FaseAtaque] Objetivo {target.name} destruido tras el ataque.");
        }
    }

    void EndCurrentUnitTurn()
    {
        // Marcamos la unidad actual como hecha (en esta implementación simple no guardamos un flag en la unidad,
        // simplemente avanzamos en la lista)
        ClearAllHighlights();

        currentUnit = null;
        currentIndex++;

        // Si quedan unidades por actuar
        if (currentIndex >= 0 && currentIndex < playerUnitsToAct.Count)
        {
            BeginTurnForUnit(playerUnitsToAct[currentIndex]);
        }
        else
        {
            // Fase de ataque del jugador completada
            Debug.Log("Fase de ataque del jugador completada.");
            OnPlayerAttackPhaseComplete();
        }
    }

    // Llamado cuando el jugador acaba su fase de ataque
    void OnPlayerAttackPhaseComplete()
    {
        ClearAllHighlights();

        // Aquí lanzar la IA: por ejemplo
        Debug.Log("-> Aquí deberías invocar la IA para que haga sus movimientos y ataques.");

        // Si más adelante necesitas reactivar FaseAccion (movimiento del jugador) tras IA, hazlo desde el manager de fases.
    }

    void OnDisable()
    {
        ClearAllHighlights();
    }

    private IEnumerator AttackMoveAnimation(GameObject attacker, Vector3 targetPosition, float moveDistance = 0.3f, float speed = 8f)
{
    if (attacker == null) yield break;

    Vector3 startPos = attacker.transform.position;
    Vector3 direction = (targetPosition - startPos).normalized;

    // Posición a la que se moverá (un poco hacia el enemigo)
    Vector3 attackPos = startPos + direction * moveDistance;

    // Mover hacia el ataque
    float t = 0f;
    while (t < 1f)
    {
        t += Time.deltaTime * speed;
        attacker.transform.position = Vector3.Lerp(startPos, attackPos, t);
        yield return null;
    }

    // Breve pausa en la posición de ataque
    yield return new WaitForSeconds(0.05f);

    // Volver a la posición original
    t = 0f;
    while (t < 1f)
    {
        t += Time.deltaTime * speed;
        attacker.transform.position = Vector3.Lerp(attackPos, startPos, t);
        yield return null;
    }

    attacker.transform.position = startPos; // asegurar que queda exacta
}

}

