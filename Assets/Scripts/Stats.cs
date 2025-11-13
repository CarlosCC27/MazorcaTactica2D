using UnityEngine;

public class Stats : MonoBehaviour
{
    [Header("Atributos de la unidad")]
    public int vida = 0;
    public int ataque = 0;
    public int defensa = 0;
    public int alcance = 0;
    public int movimiento = 0;

    [Header("Acciones de la unidad")]
    public bool puedeAtacar = false;
    public bool puedeMoverse = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
