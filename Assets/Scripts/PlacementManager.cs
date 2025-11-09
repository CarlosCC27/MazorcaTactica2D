using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class PlacementManager : MonoBehaviour
{
    [Header("Referencias de UI")]
    public Button botonA;
    public Button botonS;
    public Button botonE;

    [Header("Referencias del Tilemap")]
    public Tilemap tilemap;

    [Header("Prefabs o personajes")]
    public GameObject estructura;
    public GameObject humanoA;
    public GameObject humanoS;

    [Header("Costes de unidades")]
    private int coste = 0;
    public int costeSoldado = 5;
    public int costeArquero = 3;
    public int costeEstructura = 20;

    [Header("Referencias del Jugador")]
    public JugadorManager jugadorManager; // 👈 arrástralo desde el inspector

    private GameObject previewObject;   // el que sigue al cursor
    private GameObject selectedPrefab;  // el que se colocará
    private Camera mainCamera;
    private bool tropa = false;
    // Guardará qué celdas ya están ocupadas
    private System.Collections.Generic.HashSet<Vector3Int> occupiedCells = new System.Collections.Generic.HashSet<Vector3Int>();

    void Start()
    {
        mainCamera = Camera.main;

        // Asignar eventos a los botones
        botonA.onClick.AddListener(() => SelectCharacter(humanoA));
        botonS.onClick.AddListener(() => SelectCharacter(humanoS));
        botonE.onClick.AddListener(() => SelectCharacter(estructura));
    }

    void Update()
    {
        if (previewObject != null)
        {
            MovePreviewToMouse();

            if (Input.GetMouseButtonDown(0))
                PlaceCharacter();
        }
    }

    // ====================================================
    // 🔹 Selecciona el personaje y crea su preview
    // ====================================================
    void SelectCharacter(GameObject prefab)
    {
        // Destruir cualquier preview anterior
        if (previewObject != null)
            Destroy(previewObject);

        selectedPrefab = prefab;
        previewObject = Instantiate(prefab);
        SetPreviewTransparency(previewObject, 0.5f);
    }

    // ====================================================
    // 🔹 Mueve el personaje fantasma al ratón
    // ====================================================
    void MovePreviewToMouse()
    {
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        // Convertir posición a celda del grid
        Vector3Int cellPos = tilemap.WorldToCell(mouseWorld);
        Vector3 cellCenter = tilemap.GetCellCenterWorld(cellPos);
        if (tropa)
        {
            cellCenter -= new Vector3(0, tilemap.cellSize.y / 3f, 0);
        }
        else
        {
            cellCenter -= new Vector3(0, 0, 0);
        }
        // Obtener el centro de la celda

        // Mover el objeto preview
        previewObject.transform.position = cellCenter;

        // Cambiar color del preview si la celda no es válida
        if (!tilemap.HasTile(cellPos) || occupiedCells.Contains(cellPos))
        {
            // 🔴 Rojo semitransparente si no se puede colocar
            SetPreviewColor(previewObject, new Color(1f, 0.3f, 0.3f, 0.5f));
        }
        else
        {
            // ⚪ Blanco semitransparente si se puede colocar
            SetPreviewColor(previewObject, new Color(1f, 1f, 1f, 0.5f));
        }
    }

    // ====================================================
    // 🔹 Coloca el personaje en la celda actual
    // ====================================================
    void PlaceCharacter()
    {
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        Vector3Int cellPos = tilemap.WorldToCell(mouseWorld);

        // ❌ Si no hay tile o la celda está ocupada, salir
        if (!tilemap.HasTile(cellPos) || occupiedCells.Contains(cellPos))
            return;

        // Determinar el coste según el tipo de unidad
        if (selectedPrefab == humanoS)
        {
            tropa = true;
            coste = costeSoldado; // Soldado
        }
        else if (selectedPrefab == humanoA)
        {
            tropa = true;
            coste = costeArquero; // Arquero
        }
        else if (selectedPrefab == estructura)
        {
            tropa = false;
            coste = costeEstructura; // Estructura
        }
        // ❌ Si no hay suficiente oro, no colocar
        if (tropa && !jugadorManager.GastarOro(coste))
        {
            Debug.Log("❌ No tienes suficiente oro para colocar esta tropa");
            jugadorManager.ParpadearOro(); // 🔴 Muestra el parpadeo
            return;
        }
        else if (!tropa && !jugadorManager.GastarMadera(coste))
        {
            Debug.Log("❌ No tienes suficiente madera para colocar esta estructura");
            jugadorManager.ParpadearMadera(); // 🔴 Muestra el parpadeo
            return;
        }

        if (tropa)
        {
            Vector3 cellCenter = tilemap.GetCellCenterWorld(cellPos);
            cellCenter -= new Vector3(0, tilemap.cellSize.y / 3f, 0);

            // Instanciar el personaje en la celda válida
            GameObject placed = Instantiate(selectedPrefab, cellCenter, Quaternion.identity);

            // Marcar la celda como ocupada
            occupiedCells.Add(cellPos);

            // Restaurar opacidad total
            SetPreviewTransparency(placed, 1f);
        }
        else
        {
            Debug.Log("Estructura colocada");
            Vector3 cellCenter = tilemap.GetCellCenterWorld(cellPos);
            cellCenter -= new Vector3(0, 0, 0);

            // Instanciar el personaje en la celda válida
            GameObject placed = Instantiate(selectedPrefab, cellCenter, Quaternion.identity);

            // Marcar la celda como ocupada
            occupiedCells.Add(cellPos);

            // Restaurar opacidad total
            SetPreviewTransparency(placed, 1f);
        }
    }

    // ====================================================
    // 🔹 Cambia la transparencia de todos los renderers
    // ====================================================
    void SetPreviewTransparency(GameObject obj, float alpha)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            foreach (Material m in r.materials)
            {
                if (m.HasProperty("_Color"))
                {
                    Color c = m.color;
                    c.a = alpha;
                    m.color = c;
                }
            }
        }
    }

    // ====================================================
    // 🔹 Cambia el color (incluye transparencia)
    // ====================================================
    void SetPreviewColor(GameObject obj, Color color)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            foreach (Material m in r.materials)
            {
                if (m.HasProperty("_Color"))
                    m.color = color;
            }
        }
    }

    public bool IsCellOccupied(Vector3Int cell)
    {
        return occupiedCells.Contains(cell);
    }
}
