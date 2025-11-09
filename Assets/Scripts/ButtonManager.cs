using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    public Button botonA;
    public Button botonS;
    public Button botonE;

    void Start()
    {
        // Asignar eventos a los botones
        botonA.onClick.AddListener(() => OnButtonPressed(botonA, new List<Button> { botonS, botonE }));
        botonS.onClick.AddListener(() => OnButtonPressed(botonS, new List<Button> { botonA, botonE }));
        botonE.onClick.AddListener(() => OnButtonPressed(botonE, new List<Button> { botonA, botonS }));
    }

    void OnButtonPressed(Button botonPulsado, List<Button> botonesOtros)
    {
        // Desactivar el botón pulsado
        botonPulsado.interactable = false;
        SetButtonColor(botonPulsado, Color.gray);
        //Debug.Log("Botón " + botonPulsado.name + " pulsado.");

        // Reactivar el otro botón
        foreach (Button boton in botonesOtros)
        {
            boton.interactable = true;
            SetButtonColor(boton, Color.white);
        }
    }

    void SetButtonColor(Button boton, Color color)
    {
        boton.image.color = color;
    }
}
