using UnityEngine;

public enum TacticalOrderType
{
    Mantener,
    Avanzar,
    Retroceder,
    AtacarObjetivo,
    MoverHaciaZona
}

public class TacticalOrder
{
    public TacticalOrderType tipo;
    public Vector3Int objetivoCell;   // opcional
    public GameObject objetivoUnidad; // opcional

    public TacticalOrder(TacticalOrderType t)
    {
        Debug.Log("Creando orden táctica de tipo: " + t);
        tipo = t;
        objetivoCell = Vector3Int.zero;
        objetivoUnidad = null;
    }

    public TacticalOrder(TacticalOrderType t, Vector3Int cell)
    {
        Debug.Log("Creando orden táctica de tipo: " + t + " con objetivo en celda: " + cell);
        tipo = t;
        objetivoCell = cell;
    }

    public TacticalOrder(TacticalOrderType t, GameObject target)
    {
        Debug.Log("Creando orden táctica de tipo: " + t + " con objetivo unidad: " + target.name);
        tipo = t;
        objetivoUnidad = target;
    }
}
