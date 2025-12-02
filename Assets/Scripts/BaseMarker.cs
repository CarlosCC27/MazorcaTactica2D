using UnityEngine;

[RequireComponent(typeof(ControladorTropa))]
public class BaseMarker : MonoBehaviour
{
    [Tooltip("Si true, nadie podrá colocar tropas encima de la base (recomendado).")]
    public bool blockPlacementOnTop = true;

    private PlacementManager placement;
    private Vector3Int myCell;

    void Start()
    {
        placement = FindObjectOfType<PlacementManager>();
        if (placement == null)
        {
            Debug.LogError("BaseMarker: No se encontró PlacementManager en la escena.");
            return;
        }

        // Aseguramos tag para que AIManager/otros la reconozcan como Base.
        gameObject.tag = "Base";

        // Obtener la celda donde está colocada esta base
        myCell = placement.tilemap.WorldToCell(transform.position);

        // Marcar la celda como "ocupada" para evitar colocación
        placement.SetCellOccupied(myCell, true);

        // Opción extra: si quieres bloquear completamente (incluso el paso),
        // también puedes marcar como ocupada por unidad (solo si realmente quieres).
        if (blockPlacementOnTop)
        {
            // Este SetCellOccupied marca ocupación genérica (tu código lo interpreta como ocupada)
            placement.SetCellOccupied(myCell, true);
        }

        // Aviso si el SO no marca esBase = true (no obligatorio, pero útil)
        var ctrl = GetComponent<ControladorTropa>();
        if (ctrl != null && ctrl.datosBase != null && !ctrl.datosBase.esBase)
        {
            Debug.LogWarning($"BaseMarker: El TropaData asignado a {gameObject.name} no tiene esBase=true. " +
                             "Recomendado marcar esBase para influencia y lógica de IA.");
        }

        Debug.Log($"BaseMarker: Base registrada en celda {myCell}. Colocación bloqueada: {blockPlacementOnTop}");
    }

    void OnDestroy()
    {
        if (placement != null)
        {
            placement.SetCellOccupied(myCell, false);
        }
    }
}
