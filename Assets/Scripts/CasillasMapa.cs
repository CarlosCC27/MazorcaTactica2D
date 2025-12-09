using UnityEngine;

public class CasillasMapa : MonoBehaviour
{
    public enum TipoCasilla
    {
        Verde_Vida,
        Roja_Ataque,
        Morada_Rango
    }

    [Header("Configuración")]
    public TipoCasilla tipoDeCasilla;

    // Para que el efecto no se repita infinitamente si se queda parado encima
    private bool efectoConsumido = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Solo reacciona si no se ha usado y es un Aliado
        if (efectoConsumido) return;

        // Comprobamos si es la tropa aliada
        if (collision.CompareTag("Aliado"))
        {
            ControladorTropa tropa = collision.GetComponent<ControladorTropa>();

            if (tropa != null)
            {
                AplicarEfecto(tropa);

                efectoConsumido = true; 
                
            }
        }
    }

    void AplicarEfecto(ControladorTropa tropa)
    {
        switch (tipoDeCasilla)
        {
            case TipoCasilla.Verde_Vida:
                Debug.Log("Casilla Verde: Curando +2 vida");
                tropa.ModificarVida(2);
                break;

            case TipoCasilla.Roja_Ataque:
                Debug.Log("Casilla Roja: Ataque x2");
                tropa.ModificarAtaque(2); // Multiplica por 2
                break;

            case TipoCasilla.Morada_Rango:
                Debug.Log("Casilla Morada: Rango +2");
                tropa.ModificarRango(2); // Suma 2
                break;
        }

        efectoConsumido = false;
    }
}