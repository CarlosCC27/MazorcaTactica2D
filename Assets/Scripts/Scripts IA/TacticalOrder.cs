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
        tipo = t;
        objetivoCell = Vector3Int.zero;
        objetivoUnidad = null;
    }

    public TacticalOrder(TacticalOrderType t, Vector3Int cell)
    {
        tipo = t;
        objetivoCell = cell;
    }

    public TacticalOrder(TacticalOrderType t, GameObject target)
    {
        tipo = t;
        objetivoUnidad = target;
    }
}
