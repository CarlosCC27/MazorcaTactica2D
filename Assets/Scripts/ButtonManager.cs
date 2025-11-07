using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    public Button botonA;
    public Button botonS;

    void Start()
    {
        // Asignar eventos a los botones
        botonA.onClick.AddListener(() => OnButtonPressed(botonA, botonS));
        botonS.onClick.AddListener(() => OnButtonPressed(botonS, botonA));
    }

    void OnButtonPressed(Button botonPulsado, Button botonOtro)
    {
        // Desactivar el botón pulsado
        botonPulsado.interactable = false;
        SetButtonColor(botonPulsado, Color.gray);
        //Debug.Log("Botón " + botonPulsado.name + " pulsado.");

        // Reactivar el otro botón
        botonOtro.interactable = true;
        SetButtonColor(botonOtro, Color.white);
    }

    void SetButtonColor(Button boton, Color color)
    {
        boton.image.color = color;
    }
}
