using UnityEngine;

public enum AIStrategy { Agresiva, Defensiva, Equilibrada }

public class AIGlobal : MonoBehaviour
{
    public static AIGlobal Instance;

    [Header("Tuning estrategia")]
    [Tooltip("Si playerUnits > aiUnits * defendThreshold -> Defensiva")]
    public float defendThreshold = 1.3f;

    [Tooltip("Si aiUnits > playerUnits * (1/attackThreshold) -> Agresiva")]
    public float attackThreshold = 0.8f;

    [Header("Estado")]
    public AIStrategy currentStrategy = AIStrategy.Equilibrada;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    public void DecideStrategy(int aiCount, int playerCount)
    {
        if (playerCount > aiCount * defendThreshold)
            currentStrategy = AIStrategy.Agresiva;
        else if (aiCount > playerCount * (1f / attackThreshold))
            currentStrategy = AIStrategy.Agresiva;
        else
            currentStrategy = AIStrategy.Agresiva;
    }
}
