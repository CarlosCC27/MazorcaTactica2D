using UnityEngine;

[CreateAssetMenu(fileName = "NuevaTropa", menuName = "Scriptable Objects/Tropas")]
public class TropaData : ScriptableObject
{
    
    [Header("Identificaci�n")]
    public string nombreTropa = "Soldado Generico";
    public string descripcion = "Unidad b�sica.";


    [Header("Tipo")]
    [Tooltip("Marcar si es una estructura, si es un humano dejar desmarcado")]
    public bool esEstructura = false;

    [Header("EsBase")]
    [Tooltip("Marcar si es una base, si no dejar desmarcado")]
    public bool esBase = false;
    

    [Header("Bando")]
    [Tooltip("Marcar si la unidad es controlada por la IA/Enemigo.")]
    public bool esEnemigo = false;

    
    [Header("Estad�sticas Base")]
    public int vida = 10;
    public int ataque = 3;
    public int defensa = 2;

    
    [Header("Variables T�cticas / IA")]
    [Tooltip("Cu�ntas casillas puede mover la unidad por turno.")]
    public int rangoMovimiento = 3;
    [Tooltip("Distancia m�xima de ataque (en casillas).")]
    public int rangoAtaque = 1;


    [Header("Coste")]
    public int costeOro = 5;
    public int costeMadera = 0;

    [Header("Recursos que dan")]
    public int maderaDada = 0;
    public int oroDado = 0;

    
    [Header("Recursos Visuales")]
    [Tooltip("El Sprite o Prefab que se usar� para representar la unidad.")]
    public GameObject prefabUnidad;
}
