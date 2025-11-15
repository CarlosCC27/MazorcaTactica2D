using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class PlacementManager : MonoBehaviour
{
    /*
    [Header("Referencias de UI")]
    public Button botonA;
    public Button botonS;
    public Button botonE;
    */
    public bool fasePreparacion = true;
    

    [Header("Referencias del Tilemap")]
    public Tilemap tilemap;

    /*
    [Header("Prefabs o personajes")]
    public GameObject estructura;
    public GameObject humanoA;
    public GameObject humanoS;
    public GameObject enemigoPrueba;
    */

    /*
    [Header("Costes de unidades")]
    public int costeSoldado = 5;
    public int costeArquero = 3;
    public int costeEstructura = 20
    */
    private int coste;

    [Header("Referencias del Jugador")]
    public JugadorManager jugadorManager;

    [Header("Referencias de Otros Managers")]
    public FaseAccion faseAccionManager;

    private GameObject previewObject;
    private GameObject selectedPrefab;
    private Camera mainCamera;

    private TropaData selectedTropaData;

    private bool tropa;
    // Guardará qué celdas ya están ocupadas
    private System.Collections.Generic.HashSet<Vector3Int> occupiedCells = 
            new System.Collections.Generic.HashSet<Vector3Int>();

    void Start()
    {
        mainCamera = Camera.main;

        // Asignar eventos a los botones
        //botonA.onClick.AddListener(() => SelectCharacter(humanoA));
        //botonS.onClick.AddListener(() => SelectCharacter(humanoS));
        //botonE.onClick.AddListener(() => SelectCharacter(estructura));

        //PosicionarEnemigoPrueba(enemigoPrueba);
    }

    void Update()
    {

        if (!fasePreparacion) return;
        if (previewObject != null)
        {
            MovePreviewToMouse();

            if (Input.GetMouseButtonDown(0))
                PlaceCharacter();
        }
        if (Input.GetMouseButtonDown(0))
        {
            GetClickedCellIndices();
        }
    }

    void GetClickedCellIndices()
    {
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        Vector3Int cellPos = tilemap.WorldToCell(mouseWorld);

        // Solo si hay tile en esa celda
        if (tilemap.HasTile(cellPos))
        {
            Debug.Log($"Celda clicada: X={cellPos.x}, Y={cellPos.y}, Z={cellPos.z}");
            // Aquí puedes pasar cellPos a otro método si quieres
        }
    }
    public void DesactivarColocacion()
    {
        fasePreparacion = false;

        // Destruir cualquier preview activo
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }

        selectedPrefab = null; // quitar referencia al prefab seleccionado
    }
    void PosicionarEnemigoPrueba(GameObject enemigo)
    {
        // Celda objetivo
        Vector3Int targetCell = new Vector3Int(0, 0, 0);

        // Verificar que existe un tile en esa celda
        if (!tilemap.HasTile(targetCell))
        {
            Debug.LogWarning("❌ La celda [0,0] no tiene un tile, no se puede colocar el enemigo.");
            return;
        }

        // Obtener el centro del tile
        Vector3 cellCenter = tilemap.GetCellCenterWorld(targetCell);

        // Instanciar el objeto (usa el prefab que quieras)
        GameObject enemigoInstanciado = Instantiate(enemigo, cellCenter, Quaternion.identity);

        // Marcar la celda como ocupada
        occupiedCells.Add(targetCell);

        // Asegurar opacidad total
        SetPreviewTransparency(enemigoInstanciado, 1f);
    }
    // ====================================================
    // 🔹 Selecciona el personaje y crea su preview
    // ====================================================
    public void SelectCharacter(GameObject prefab)
    {
        // Destruir cualquier preview anterior
        if (previewObject != null)
            Destroy(previewObject);

        selectedPrefab = prefab;

        ControladorTropa controlador = prefab.GetComponent<ControladorTropa>();

        selectedTropaData = controlador.datosBase;

        tropa = !selectedTropaData.esEstructura;
        coste = tropa ? selectedTropaData.costeOro : selectedTropaData.costeMadera;

        Debug.Log($"Unidad seleccionada: {selectedTropaData.nombreTropa}. Coste: {coste}. Es Tropa: {tropa}");

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
        /*
        if (tropa)
        {
            cellCenter -= new Vector3(0, tilemap.cellSize.y / 3f, 0);
        }
        else
        {
            cellCenter -= new Vector3(0, 0, 0);
        }
        */
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
        /*
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
        */
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
            //cellCenter -= new Vector3(0, tilemap.cellSize.y / 3f, 0);

            // Instanciar el personaje en la celda válida
            GameObject placed = Instantiate(selectedPrefab, cellCenter, Quaternion.identity);

            // Marcar la celda como ocupada
            occupiedCells.Add(cellPos);

            // Restaurar opacidad total
            SetPreviewTransparency(placed, 1f);


            //Correccion visual
            if (faseAccionManager != null)
            {
                faseAccionManager.ClearHighlights();
            }
        }
        else
        {
            Debug.Log("Estructura colocada");
            Vector3 cellCenter = tilemap.GetCellCenterWorld(cellPos);
            //cellCenter -= new Vector3(0, 0, 0);

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

    // Permite marcar o desmarcar una celda como ocupada
    public void SetCellOccupied(Vector3Int cell, bool occupied)
    {
        if (occupied)
            occupiedCells.Add(cell);
        else
            occupiedCells.Remove(cell);
    }

    // ====================================================
    // 🔹 FUNCIÓN PÚBLICA PARA EL BOTÓN DE CAMBIO DE FASE
    // ====================================================
    public void TogglePhase()
    {
        if (fasePreparacion)
        {
            // 1. TRANSICIÓN: PREPARACIÓN -> ACCIÓN

            // Llama a la lógica de limpieza de colocación (Desactiva la colocación y el preview)
            DesactivarColocacion(); // Establece fasePreparacion = false

            // Activa la lógica de la Fase de Acción
            if (faseAccionManager != null)
            {
                faseAccionManager.enabled = true; // Activa el Update de FaseAccion
                Debug.Log("🚀 Fase cambiada a ACCIÓN. Colocación desactivada.");
            }
        }
        
        else
        {
            // 2. TRANSICIÓN: ACCIÓN -> PREPARACIÓN

            // Desactiva la lógica de la Fase de Acción
            if (faseAccionManager != null)
            {
                faseAccionManager.enabled = false; // Desactiva el Update de FaseAccion
                // Llama a su función de limpieza si es necesario (limpia selecciones y resaltados)
                faseAccionManager.ClearHighlights();
            }

            // Rehabilita la fase de preparación
            fasePreparacion = true;

            Debug.Log("🏠 Fase cambiada a PREPARACIÓN. Colocación activa.");

            // Si necesitas que los botones de UI de selección de tropa se muestren,
            // la responsabilidad recae en el sistema que los gestiona, NO en PlacementManager.
            // Asegúrate de que el botón de cambio de fase oculte/muestre los botones A/S/E 
            // a través de su propia configuración 'OnClick' en el Inspector, además de llamar a TogglePhase().
        }
        
    }
}
