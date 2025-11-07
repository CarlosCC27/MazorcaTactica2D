using UnityEngine;
using TMPro;

public class JugadorManager : MonoBehaviour
{
    [Header("Recursos iniciales")]
    public int oro = 100;
    public int madera = 100;

    [Header("UI de recursos")]
    public TMP_Text textoOro;
    public TMP_Text textoMadera;

    void Start()
    {
        ActualizarUI();
    }

    // ====================================================
    // 🔹 Modifica los valores y actualiza el texto
    // ====================================================
    public void AñadirOro(int cantidad)
    {
        oro += cantidad;
        ActualizarUI();
    }

    public void AñadirMadera(int cantidad)
    {
        madera += cantidad;
        ActualizarUI();
    }

    public bool GastarOro(int cantidad)
    {
        if (oro >= cantidad)
        {
            oro -= cantidad;
            ActualizarUI();
            return true;
        }
        return false;
    }

    public bool GastarMadera(int cantidad)
    {
        if (madera >= cantidad)
        {
            madera -= cantidad;
            ActualizarUI();
            return true;
        }
        return false;
    }

    // ====================================================
    // 🔹 Actualiza los textos en pantalla
    // ====================================================
    void ActualizarUI()
    {
        if (textoOro != null)
            textoOro.text = "Oro: " + oro;

        if (textoMadera != null)
            textoMadera.text = "Madera: " + madera;
    }
}
