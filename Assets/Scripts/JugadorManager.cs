using UnityEngine;
using TMPro;

public class JugadorManager : MonoBehaviour
{
    [Header("Recursos iniciales")]
    public int oro;
    public int madera;

    [Header("UI de recursos")]
    public TMP_Text textoOro;
    public TMP_Text textoMadera;

    public GameObject panelJugador;

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

    // ====================================================
    // 🔹 Efecto visual: parpadear el texto de oro si no hay suficiente
    // ====================================================
    public void ParpadearOro()
    {
        if (textoOro != null)
            StartCoroutine(ParpadearTextoOroCoroutine());
    }

    public void ParpadearMadera()
    {
        if (textoMadera != null)
            StartCoroutine(ParpadearTextoMaderaCoroutine());
    }
    private System.Collections.IEnumerator ParpadearTextoOroCoroutine()
    {
        Color originalColor = textoOro.color;

        for (int i = 0; i < 2; i++)
        {
            textoOro.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            textoOro.color = originalColor;
            yield return new WaitForSeconds(0.2f);
        }
    }
    private System.Collections.IEnumerator ParpadearTextoMaderaCoroutine()
    {
        Color originalColor = textoMadera.color;

        for (int i = 0; i < 2; i++)
        {
            textoMadera.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            textoMadera.color = originalColor;
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void ActivarUIJugador()
    {
        Debug.Log("🔵 ACTIVANDO UI DEL JUGADOR");

        foreach (Transform child in panelJugador.transform)
            child.gameObject.SetActive(true);

        panelJugador.SetActive(true);
    }


}