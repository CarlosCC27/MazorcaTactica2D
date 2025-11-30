using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class InfluenceMapVisualizer : MonoBehaviour
{
    [Header("Refs (auto asigna si están vacíos)")]
    public InfluenceMap influenceMap;
    public Tilemap tilemap;

    [Header("Opciones de visualización")]
    public bool showGizmos = true;     // cubos coloreados en SceneView / GameView (si Gizmos activado)
    public bool showLabels = true;     // números en pantalla (OnGUI) en Play mode
    public float colorScale = 1f;      // escala para visualizar intensidad de color
    public float labelScale = 1f;      // escala del tamaño del label
    public float maxDisplayMagnitude = 30f; // valor que hace saturar el color (para normalizar)

    [Header("Ajustes de layout")]
    public Vector3 labelOffset = new Vector3(0f, 0f, 0f); // desplazamiento visual del label en unidades del mundo

    GUIStyle labelStyle;

    void OnEnable()
    {
        if (influenceMap == null)
            influenceMap = FindObjectOfType<InfluenceMap>();

        if (tilemap == null && influenceMap != null)
            tilemap = influenceMap.tilemap;

        // crear estilo simple
        labelStyle = new GUIStyle();
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontSize = Mathf.RoundToInt(12 * labelScale);
        labelStyle.richText = false;
    }

    void Update()
    {
        // actualizar referencias si las perdimos (útil en editor)
        if (influenceMap == null) influenceMap = FindObjectOfType<InfluenceMap>();
        if (tilemap == null && influenceMap != null) tilemap = influenceMap.tilemap;

        // mantener tamaño del label acorde a labelScale
        if (labelStyle == null) labelStyle = new GUIStyle();
        labelStyle.fontSize = Mathf.RoundToInt(12 * labelScale);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        if (influenceMap == null || tilemap == null) return;

        var bounds = tilemap.cellBounds;
        Camera cam = Camera.current;
        if (cam == null) cam = Camera.main;

        // recorrer celdas
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!tilemap.HasTile(cell)) continue;

                float net = influenceMap.GetNetInfluence(cell);
                float aiVal = influenceMap.GetAIInfluence(cell);
                float plVal = influenceMap.GetPlayerInfluence(cell);

                // color: si net > 0 -> azul (AI), si net < 0 -> rojo (Player)
                Color baseColor;
                float intensity = Mathf.Clamp01(Mathf.Abs(net) / Mathf.Max(0.0001f, maxDisplayMagnitude)) * colorScale;
                intensity = Mathf.Clamp01(intensity);

                if (net > 0.0001f)
                {
                    baseColor = new Color(0f, 0.4f, 1f, Mathf.Lerp(0.05f, 0.9f, intensity));
                }
                else if (net < -0.0001f)
                {
                    baseColor = new Color(1f, 0.2f, 0f, Mathf.Lerp(0.05f, 0.9f, intensity));
                }
                else
                {
                    baseColor = new Color(0.2f, 0.2f, 0.2f, 0.05f);
                }

                Gizmos.color = baseColor;
                Vector3 worldCenter = tilemap.GetCellCenterWorld(cell);

                // Dibujar un cubo fino para ver el heatmap
                Vector3 size = new Vector3(tilemap.cellSize.x * 0.9f, tilemap.cellSize.y * 0.9f, 0.001f);
                Gizmos.DrawCube(worldCenter, size);

                // Opcional: dibujar un contorno en la SceneView para facilitar lectura
#if UNITY_EDITOR
                UnityEditor.Handles.DrawWireDisc(worldCenter, Vector3.forward, Mathf.Max(tilemap.cellSize.x, tilemap.cellSize.y) * 0.35f);
#endif
            }
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return; // etiquetas en play para no ensuciar editor (opcional)
        if (!showLabels) return;
        if (influenceMap == null || tilemap == null) return;

        var bounds = tilemap.cellBounds;
        Camera cam = Camera.main;
        if (cam == null) return;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!tilemap.HasTile(cell)) continue;

                float net = influenceMap.GetNetInfluence(cell);
                float aiVal = influenceMap.GetAIInfluence(cell);
                float plVal = influenceMap.GetPlayerInfluence(cell);

                Vector3 worldCenter = tilemap.GetCellCenterWorld(cell) + labelOffset;
                Vector3 screenPos = cam.WorldToScreenPoint(worldCenter);

                // si está detrás de la cámara, ignorar
                if (screenPos.z < 0) continue;

                // Invertir Y para GUI (OnGUI usa coordenadas con origen en esquina superior)
                float sx = screenPos.x;
                float sy = Screen.height - screenPos.y;

                string text = $"{net:F1}\n(A:{aiVal:F1} P:{plVal:F1})";

                // fondo semitransparente para legibilidad
                var prevColor = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.45f);
                Vector2 size = labelStyle.CalcSize(new GUIContent(text));
                Rect bg = new Rect(sx - size.x * 0.5f - 4, sy - size.y * 0.5f - 2, size.x + 8, size.y + 4);
                GUI.Box(bg, GUIContent.none);
                GUI.color = prevColor;

                GUI.Label(new Rect(sx - size.x * 0.5f, sy - size.y * 0.5f, size.x, size.y), text, labelStyle);
            }
        }
    }
}
