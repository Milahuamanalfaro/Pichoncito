using UnityEngine;

/// <summary>
/// Singleton por escena que guarda puntos, botellas y vidas.
/// Colócalo en un GameObject vacío llamado "ControladorPuntaje".
/// </summary>
public class ControladorPuntaje : MonoBehaviour
{
    public static ControladorPuntaje Instancia { get; private set; }

    [Header("Configuración")]
    [SerializeField] private int vidasMaximas = 3;
    [SerializeField] private int puntosPorBotella = 10;

    public int Puntos { get; private set; }
    public int Botellas { get; private set; }
    public int TotalBotellas { get; private set; }
    public int Vidas { get; private set; }
    public int VidasMaximas => vidasMaximas;

    /// <summary>Porcentaje (0-100) de botellas recolectadas respecto al total del nivel.</summary>
    public float PorcentajeReciclaje =>
        TotalBotellas > 0 ? (Botellas * 100f) / TotalBotellas : 0f;

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        Vidas = vidasMaximas;
        Puntos = 0;
        Botellas = 0;

        // Cuenta todas las botellas activas del nivel al cargar la escena.
        TotalBotellas = FindObjectsByType<Recolectable>().Length;
    }

    private void OnDestroy()
    {
        if (Instancia == this)
        {
            Instancia = null;
        }
    }

    public void SumarBotella(int cantidad = 1)
    {
        Botellas += cantidad;
        Puntos += puntosPorBotella * cantidad;
    }

    /// <summary>Resta vida. Devuelve true si el jugador se quedó sin vidas.</summary>
    public bool RecibirDano(int dano)
    {
        Vidas = Mathf.Max(0, Vidas - dano);
        return Vidas <= 0;
    }
}
