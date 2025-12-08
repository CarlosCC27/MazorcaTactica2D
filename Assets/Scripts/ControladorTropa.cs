using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(SpriteRenderer))]
public class ControladorTropa : MonoBehaviour
{
    [Header("Datos de la Tropa")]
    public TropaData datosBase;

    [Header("Datos jugador")]
    public JugadorManager jugadorManager;

    [Header("Estado de turno")]
    public bool hasMoved = false;

    private int saludActual;
    public TacticalOrder currentOrder;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        jugadorManager = FindObjectOfType<JugadorManager>();

        if (datosBase == null)
        {
            Debug.LogError("❌ No se ha asignado TropaData a " + gameObject.name);
            return;
        }

        saludActual = datosBase.vida;
        AplicarAjustesDeBando();
        gameObject.tag = datosBase.esEnemigo ? "Enemy" : "Aliado";

        // ⚠️ Importante: no asignamos ninguna orden inicial
        currentOrder = null;
        hasMoved = false;
    }


    #region Movimiento / turno
    public void MarkMoved()
    {
        hasMoved = true;
        ApplyMovedVisual(true);
    }

    public void ResetMovedFlag()
    {
        hasMoved = false;
        ApplyMovedVisual(false);
    }

    public bool CanMoveThisTurn()
    {
        return !hasMoved;
    }

    public void ApplyMovedVisual(bool moved)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = moved ? new Color(1f, 1f, 1f, 0.6f) : new Color(1f, 1f, 1f, 1f);
    }
    #endregion

    #region Ajustes de bando
    void AplicarAjustesDeBando()
    {
        if (datosBase.esEnemigo) spriteRenderer.flipX = true;
        int layer = datosBase.esEnemigo ? LayerMask.NameToLayer("Enemy") : LayerMask.NameToLayer("Aliado");
        if (layer != -1) gameObject.layer = layer;
    }
    #endregion

    #region Salud / combate
    public void TakeDamage(int cantidad)
    {
        saludActual -= cantidad;
        if (saludActual <= 0) Die();
    }

    void Die()
    {
        PlacementManager pm = FindObjectOfType<PlacementManager>();
        if (pm != null && pm.tilemap != null)
        {
            Vector3Int cell = pm.tilemap.WorldToCell(transform.position);
            pm.SetCellOccupied(cell, false);
        }


        if (datosBase.esEnemigo)
        {
            Debug.Log($"Enemigo abatido. Recompensa otorgada.");
            jugadorManager.AñadirMadera(datosBase.maderaDada);
            jugadorManager.AñadirOro(datosBase.oroDado);
        }
        else
        {
            // Lógica si muere un aliado (opcional)
            Debug.Log("Ha muerto un aliado.");
        }

        //Poner aqui corrutina o el sistema de particulas directamente

        Destroy(gameObject);
    }

    public bool IsAlive() => saludActual > 0;
    public int GetSaludActual() => saludActual;
    public int GetRangoMovimiento() => datosBase.rangoMovimiento;
    #endregion

    #region Órdenes tácticas
    public void ReceiveTacticalOrder(TacticalOrder order)
    {

        if (!AIManager.Instance.isAITurn) return;
        // Solo ejecutar si puede moverse
        if (!CanMoveThisTurn()) return;

        currentOrder = order;
        Debug.Log($"[{name}] recibió orden: {order.tipo}");

        switch (order.tipo)
        {
            case TacticalOrderType.Avanzar:
                Vector3Int nextCell = GetNextCellForAdvance(
                    AIManager.Instance.tilemap,
                    AIManager.Instance.influenceMap
                );
                MoveToCell(nextCell);
                MarkMoved();
                break;

            case TacticalOrderType.Retroceder:
                // placeholder
                break;

            case TacticalOrderType.Mantener:
                break;

            case TacticalOrderType.MoverHaciaZona:
                MoveToCell(order.objetivoCell);
                MarkMoved();
                break;

            case TacticalOrderType.AtacarObjetivo:
                // placeholder
                break;
        }
    }

    public Vector3Int GetNextCellForAdvance(Tilemap tilemap, InfluenceMap influenceMap)
    {
        Vector3Int current = tilemap.WorldToCell(transform.position);
        Vector3Int baseAliadaCell = AIManager.Instance.placementManager.GetBaseCell(true); // true = buscar base del jugador

        Vector3Int bestCell = current;
        float bestScore = float.MaxValue;

        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        foreach (var dir in dirs)
        {
            Vector3Int n = current + dir;
            if (!tilemap.HasTile(n)) continue;
            if (AIManager.Instance.placementManager.IsCellOccupied(n)) continue;

            float influence = influenceMap.GetNetInfluence(n);
            float distanceToBase = Vector3Int.Distance(n, baseAliadaCell);

            float score = -influence + distanceToBase;

            if (score < bestScore)
            {
                bestScore = score;
                bestCell = n;
            }
        }

        return bestCell;
    }

    public void MoveToCell(Vector3Int cell)
    {
        PlacementManager pm = AIManager.Instance.placementManager;
        if (pm.IsCellOccupied(cell)) return;

        Vector3Int current = AIManager.Instance.tilemap.WorldToCell(transform.position);
        pm.SetCellOccupied(current, false);

        transform.position = AIManager.Instance.tilemap.GetCellCenterWorld(cell);
        pm.SetCellOccupied(cell, true);
    }
    #endregion
}
