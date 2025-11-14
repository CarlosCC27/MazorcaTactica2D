using UnityEngine;

[CreateAssetMenu(fileName = "NuevaTropa", menuName = "Scriptable Objects/Tropas")]
public class TropaData : ScriptableObject
{
    
    [Header("Identificación")]
    public string nombreTropa = "Soldado Generico";
    public string descripcion = "Unidad básica.";


    [Header("Tipo")]
    [Tooltip("Marcar si es una estructura, si es un humano dejar desmarcado")]
    public bool esEstructura = false;
    

    [Header("Bando")]
    [Tooltip("Marcar si la unidad es controlada por la IA/Enemigo.")]
    public bool esEnemigo = false;

    
    [Header("Estadísticas Base")]
    public int vida = 10;
    public int ataque = 3;
    public int defensa = 2;

    
    [Header("Variables Tácticas / IA")]
    [Tooltip("Cuántas casillas puede mover la unidad por turno.")]
    public int rangoMovimiento = 3;
    [Tooltip("Distancia máxima de ataque (en casillas).")]
    public int rangoAtaque = 1;


    [Header("Coste")]
    public int costeOro = 5;
    public int costeMadera = 0;

    
    [Header("Recursos Visuales")]
    [Tooltip("El Sprite o Prefab que se usará para representar la unidad.")]
    public GameObject prefabUnidad;
}
