using UnityEngine;


// Asegura que este script siempre tenga el componente SpriteRenderer
[RequireComponent(typeof(SpriteRenderer))]
public class ControladorTropa : MonoBehaviour
{
    // Asigna aquí el Scriptable Object (SO_SoldadoAliado, SO_ArqueroEnemigo, etc.)
    [Header("Datos de la Tropa")]
    public TropaData datosBase;

    // Variables de estado (pueden cambiar durante la partida)
    private int saludActual;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (datosBase == null)
        {
            Debug.LogError("❌ Error: No se ha asignado un TropaData al ControladorTropa de " + gameObject.name);
            return;
        }

        // Inicializar la salud
        saludActual = datosBase.vida;

        // Aplicar la lógica de bando
        AplicarAjustesDeBando();

        // Asegurar Tag correcto para que otros sistemas (FaseAccion/FaseAtaque) detecten
        gameObject.tag = datosBase.esEnemigo ? "Enemy" : "Aliado";

        Debug.Log($"Tropa desplegada: {datosBase.nombreTropa}. Salud: {saludActual}. Enemigo: {datosBase.esEnemigo}");
    }

    // =========================================
    // 🔹 Ajuste visual y lógico según el bando
    // =========================================
    void AplicarAjustesDeBando()
    {
        // 1. Ajuste visual (Modo Espejo)
        if (datosBase.esEnemigo)
        {
            // Voltear el sprite en el eje X para el modo espejo (flip X)
            spriteRenderer.flipX = true;
            // O si usas la escala (depende de tu setup):
            // transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        }

        // 2. Ajuste lógico (Etiquetas/Layer para diferenciar bandos)
        // Esto es útil para la IA y el sistema de combate.
        // Poner layer si existe (tener las layers creadas)
        int layer = datosBase.esEnemigo ? LayerMask.NameToLayer("Enemy") : LayerMask.NameToLayer("Aliado");
        if (layer != -1)
            gameObject.layer = layer;
        //gameObject.layer = datosBase.esEnemigo ? LayerMask.NameToLayer("Enemigo") : LayerMask.NameToLayer("Aliado");
    }
    // =========================
    // Salud / combate
    // =========================
    public void TakeDamage(int cantidad)
    {
        saludActual -= cantidad;
        Debug.Log($"{datosBase.nombreTropa} recibió {cantidad} daño. Salud restante: {saludActual}");

        if (saludActual <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Avisar al PlacementManager para liberar la celda si corresponde
        PlacementManager pm = FindObjectOfType<PlacementManager>();
        if (pm != null && pm.tilemap != null)
        {
            Vector3Int cell = pm.tilemap.WorldToCell(transform.position);
            pm.SetCellOccupied(cell, false);
        }

        // Aquí podrías reproducir animación de muerte, efectos, etc.
        Destroy(gameObject);
    }

    public bool IsAlive()
    {
        return saludActual > 0;
    }
    //getter
    public int GetSaludActual()
    {
        return saludActual;
    }

    // Método de ejemplo para ser usado por la IA o el Jugador
    public int GetRangoMovimiento()
    {
        return datosBase.rangoMovimiento;
    }

    // Puedes añadir más lógica como TomarDano(int cantidad), Atacar(Unit objetivo), etc.
}