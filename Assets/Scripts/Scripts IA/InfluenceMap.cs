using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class InfluenceMap : MonoBehaviour
{
    public Tilemap tilemap;

    // Mapas separados para evitar confusión
    private Dictionary<Vector3Int, float> aiInfluence = new Dictionary<Vector3Int, float>();
    private Dictionary<Vector3Int, float> playerInfluence = new Dictionary<Vector3Int, float>();

    [Header("Parámetros de propagación")]
    public int maxRadius = 4;
    [Range(0.1f, 0.99f)]
    public float decay = 0.6f; // factor por casilla de distancia

    // Limpia mapas
    public void Clear()
    {
        aiInfluence.Clear();
        playerInfluence.Clear();
    }

    // Calcula mapas a partir de listas de unidades (cada unidad aporta "semilla")
    public void Compute(List<GameObject> aiUnits, List<GameObject> playerUnits)
    {
        Clear();

        if (tilemap == null)
        {
            Debug.LogWarning("[InfluenceMap] tilemap no asignado");
            return;
        }

        // Para cada unidad enemiga (IA): propagar influencia positiva
        foreach (var u in aiUnits)
        {
            if (u == null) continue;
            var ctrl = u.GetComponent<ControladorTropa>();
            if (ctrl == null) continue;

            float baseValue = ComputeBaseValue(ctrl);
            Vector3Int origin = tilemap.WorldToCell(u.transform.position);
            PropagateInfluence(origin, baseValue, aiInfluence);
        }

        // Para cada unidad del jugador: propagar influencia positiva en playerInfluence
        foreach (var u in playerUnits)
        {
            if (u == null) continue;
            var ctrl = u.GetComponent<ControladorTropa>();
            if (ctrl == null) continue;

            float baseValue = ComputeBaseValue(ctrl);
            Vector3Int origin = tilemap.WorldToCell(u.transform.position);
            PropagateInfluence(origin, baseValue, playerInfluence);
        }
    }

    // Heurística simple para convertir stats en "potencia"
    float ComputeBaseValue(ControladorTropa ctrl)
    {
        var data = ctrl.datosBase;
        if (data == null) return 1f;
        // Ajusta pesos según lo que te interese: vida, ataque y defensa importan
        return data.vida * 0.6f + data.ataque * 1.0f + data.defensa * 0.8f + (data.esEstructura ? 10f : 0f);
    }

    void PropagateInfluence(Vector3Int origin, float baseValue, Dictionary<Vector3Int, float> map)
    {
        // BFS simple con decay por distancia
        var visited = new HashSet<Vector3Int>();
        var queue = new Queue<(Vector3Int cell, int dist)>();
        queue.Enqueue((origin, 0));
        visited.Add(origin);

        while (queue.Count > 0)
        {
            var (cell, dist) = queue.Dequeue();
            if (!tilemap.HasTile(cell)) continue;

            float contrib = baseValue * Mathf.Pow(decay, dist);
            if (map.ContainsKey(cell)) map[cell] += contrib; else map[cell] = contrib;

            if (dist >= maxRadius) continue;

            var dirs = new Vector3Int[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
            foreach (var d in dirs)
            {
                var n = cell + d;
                if (visited.Contains(n)) continue;
                visited.Add(n);
                queue.Enqueue((n, dist + 1));
            }
        }
    }

    // API para lectura
    public float GetAIInfluence(Vector3Int cell)
    {
        if (aiInfluence.ContainsKey(cell)) return aiInfluence[cell];
        return 0f;
    }

    public float GetPlayerInfluence(Vector3Int cell)
    {
        if (playerInfluence.ContainsKey(cell)) return playerInfluence[cell];
        return 0f;
    }

    // net = AI - Player, >0 favor IA, <0 favor player
    public float GetNetInfluence(Vector3Int cell)
    {
        return GetAIInfluence(cell) - GetPlayerInfluence(cell);
    }

    // UTILIDAD: debug visual opcional — pinta tilemap según net influence
    public void DebugDrawInfluence(float scale = 1f)
    {
        foreach (var kv in aiInfluence)
        {
            if (!tilemap.HasTile(kv.Key)) continue;
            // pintamos azul para AI
            Color c = new Color(0f, 0.3f, 1f, Mathf.Clamp(kv.Value / 20f * scale, 0f, 0.9f));
            tilemap.SetTileFlags(kv.Key, TileFlags.None);
            tilemap.SetColor(kv.Key, c);
        }
        foreach (var kv in playerInfluence)
        {
            if (!tilemap.HasTile(kv.Key)) continue;
            // pintamos rojo para Player (sobrescribe parcialmente)
            Color c = new Color(Mathf.Clamp(kv.Value / 20f * scale, 0f, 0.9f), 0f, 0f, 0.75f);
            tilemap.SetTileFlags(kv.Key, TileFlags.None);
            tilemap.SetColor(kv.Key, c);
        }
    }
}
