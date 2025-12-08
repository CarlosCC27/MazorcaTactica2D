using System.Collections;
using System.Collections.Generic;
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

    [Header("Feedback visual al recibir daño")]
    public float hitShakeDuration = 0.18f;     // duración total del shake
    public float hitShakeMagnitude = 0.12f;    // magnitud (en unidades del world/local)
    public float hitFlashDuration = 0.12f;     // duración del flash rojo
    private bool isHitAnimating = false;


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

        // Si es unidad enemiga, no cambiar opacidad
        if (datosBase != null && datosBase.esEnemigo) return;

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

        // Lanzar la animación de hit (no bloqueante)
        if (!isHitAnimating)
            StartCoroutine(PlayHitAnimation());

        if (saludActual <= 0)
        {
            // Si muere, manejamos la muerte tras una pequeña espera para que se vea feedback
            StartCoroutine(HandleDeathCoroutine());
        }
    }

    IEnumerator HandleDeathCoroutine()
    {
        // Si ya estamos animando el hit, esperar a que termine (o un pequeño margen)
        float wait = Mathf.Max(hitShakeDuration, hitFlashDuration);
        yield return new WaitForSeconds(wait * 0.6f); // esperar una fracción para que el jugador vea el golpe

        // Proceder a la lógica de muerte (idem a tu Die original)
        PlacementManager pm = FindObjectOfType<PlacementManager>();
        if (pm != null && pm.tilemap != null)
        {
            Vector3Int cell = pm.tilemap.WorldToCell(transform.position);
            pm.SetCellOccupied(cell, false);
        }

        if (datosBase.esEnemigo)
        {
            Debug.Log($"Enemigo abatido. Recompensa otorgada.");

            if (datosBase.particulas != null)
                Instantiate(datosBase.particulas, transform.position, Quaternion.identity);

            if (jugadorManager != null)
            {
                jugadorManager.AñadirMadera(datosBase.maderaDada);
                jugadorManager.AñadirOro(datosBase.oroDado);
            }
        }
        else
        {
            Debug.Log("Ha muerto un aliado.");
        }

        Destroy(gameObject);
        yield break;
    }

    // -------------------------
    // Animación de hit (shake + flash)
    // -------------------------
    IEnumerator PlayHitAnimation()
    {
        if (isHitAnimating) yield break;
        isHitAnimating = true;

        Vector3 originalLocalPos = transform.localPosition;
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Color hitColor = new Color(1f, 0.45f, 0.45f, originalColor.a); // rojo suave manteniendo alpha

        float elapsed = 0f;

        // Durante la animación iremos haciendo shake y flash
        while (elapsed < hitShakeDuration)
        {
            float t = elapsed / hitShakeDuration;
            // Shake: decae con el tiempo para suavizar
            float magnitude = hitShakeMagnitude * (1f - t);

            Vector2 offset2D = Random.insideUnitCircle * magnitude;
            transform.localPosition = originalLocalPos + new Vector3(offset2D.x, offset2D.y, 0f);

            // Flash color (solo durante una fracción inicial)
            if (spriteRenderer != null)
            {
                if (elapsed < hitFlashDuration)
                {
                    // Interpola desde hitColor hacia original en esos primeros instantes
                    float ft = Mathf.Clamp01(elapsed / hitFlashDuration);
                    spriteRenderer.color = Color.Lerp(hitColor, originalColor, ft);
                }
                else
                {
                    // Aseguramos color original a partir de aquí (pero preservando alpha)
                    spriteRenderer.color = originalColor;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restaurar valores
        transform.localPosition = originalLocalPos;
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        isHitAnimating = false;
        yield break;
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

            Instantiate(datosBase.particulas, transform.position, Quaternion.identity);

            jugadorManager.AñadirMadera(datosBase.maderaDada);
            jugadorManager.AñadirOro(datosBase.oroDado);
        }
        else
        {
            
            Debug.Log("Ha muerto un aliado.");
        }

        

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
