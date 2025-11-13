using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    public Button botonA;
    public Button botonS;
    public Button botonE;
    public Button botonNF;

    public PlacementManager pm;

    void Start()
    {
        // Asignar eventos a los botones
        botonA.onClick.AddListener(() => OnButtonPressed(botonA, new List<Button> { botonS, botonE }));
        botonS.onClick.AddListener(() => OnButtonPressed(botonS, new List<Button> { botonA, botonE }));
        botonE.onClick.AddListener(() => OnButtonPressed(botonE, new List<Button> { botonA, botonS }));
        botonNF.onClick.AddListener(() => OnButtonPressed(botonNF, new List<Button> { botonA, botonS, botonE }));
    }

    void OnButtonPressed(Button botonPulsado, List<Button> botonesOtros)
    {
        if (botonPulsado == botonNF)
        {
            if (botonesOtros[0].gameObject.activeSelf == false)
            {
                foreach (Button boton in botonesOtros)
                {
                    boton.gameObject.SetActive(true);
                    boton.interactable = true;
                    SetButtonColor(boton, Color.white);
                }
                pm.fasePreparacion = true;
            }
            else
            {
                foreach (Button boton in botonesOtros)
                {
                    boton.gameObject.SetActive(false);
                }

                if(pm != null)
                {
                    pm.DesactivarColocacion();
                }
            }
            return;
        }
        else
        {
            botonPulsado.interactable = false;
            SetButtonColor(botonPulsado, Color.gray);
            foreach (Button boton in botonesOtros)
            {
                boton.interactable = true;
                SetButtonColor(boton, Color.white);
            } 
        }
    }

    void SetButtonColor(Button boton, Color color)
    {
        boton.image.color = color;
    }
}
