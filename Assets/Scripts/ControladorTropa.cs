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
        gameObject.layer = datosBase.esEnemigo ? LayerMask.NameToLayer("Enemigo") : LayerMask.NameToLayer("Aliado");
    }

    // Método de ejemplo para ser usado por la IA o el Jugador
    public int GetRangoMovimiento()
    {
        return datosBase.rangoMovimiento;
    }

    // Puedes añadir más lógica como TomarDano(int cantidad), Atacar(Unit objetivo), etc.
}