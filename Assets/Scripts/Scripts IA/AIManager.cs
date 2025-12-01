using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public enum AIStrategy { Agresiva, Defensiva, Equilibrada }

public class AIManager : MonoBehaviour
{
    public static AIManager Instance;

    [Header("Refs")]
    public PlacementManager placementManager;
    public InfluenceMap influenceMap;
    public Tilemap tilemap;

    [Header("Tuning")]
    public float defendThreshold = 1.3f; // si playerUnits > aiUnits * defendThreshold -> defensiva
    public float attackThreshold = 0.8f; // si aiUnits > playerUnits * (1/attackThreshold) -> agresiva

    public AIStrategy currentStrategy = AIStrategy.Equilibrada;

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

        // 3) Decidir estrategia simple por tamaño de fuerzas
        DecideStrategy(aiUnits.Count, playerUnits.Count);

        Debug.Log($"[AIManager] Estrategia decidida: {currentStrategy}");

        // 4) Fase Colocación (opcional) - aquí puedes llamar placementManager.PlaceStructureAtCell(...) si implementas producción
        yield return StartCoroutine(PlacementPhase());

        // 5) Fase Movimiento (placeholder)
        yield return StartCoroutine(MovementPhase(aiUnits));

        // 6) Fase Ataque (placeholder)
        yield return StartCoroutine(AttackPhase(aiUnits));

        // 7) Fin de turno IA: restaurar cosas y devolver turno al jugador
        OnEndTurn();

        yield break;
    }

    void DecideStrategy(int aiCount, int playerCount)
    {
        if (playerCount > aiCount * defendThreshold)
            currentStrategy = AIStrategy.Defensiva;
        else if (aiCount > playerCount * (1f / attackThreshold))
            currentStrategy = AIStrategy.Agresiva;
        else
            currentStrategy = AIStrategy.Equilibrada;
    }

    IEnumerator PlacementPhase()
    {
        // Placeholder: la IA podría construir estructuras si tiene recursos.
        // Por ahora pausa corta para simular "pensado"
        yield return new WaitForSeconds(0.25f);
        yield break;
    }

    IEnumerator MovementPhase(List<GameObject> aiUnits)
    {
        foreach (var u in aiUnits)
        {
            if (u == null) continue;
            var ctrl = u.GetComponent<ControladorTropa>();
            if (ctrl == null || !ctrl.CanMoveThisTurn()) continue;

            TacticalOrder order = GetOrderForUnit(u);

            // Enviar orden a unidad
            ctrl.ReceiveTacticalOrder(order);

            yield return new WaitForSeconds(0.05f);
        }
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
            int rango = ctrl.datosBase != null ? ctrl.datosBase.rangoAtaque : 0;
            if (rango <= 0) continue;

            // Encontrar objetivo cercano en rango (simple búsqueda)
            Vector3Int center = tilemap.WorldToCell(u.transform.position);
            Collider2D[] hits = Physics2D.OverlapCircleAll(tilemap.GetCellCenterWorld(center), rango + 0.1f);
            GameObject target = null;
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (h.gameObject.CompareTag("Aliado"))
                {
                    target = h.gameObject;
                    break;
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

        switch (currentStrategy)
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


}
