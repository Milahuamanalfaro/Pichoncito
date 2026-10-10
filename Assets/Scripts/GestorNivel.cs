using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla el HUD, la meta, la victoria (>= 70% de botellas) y el Game Over.
/// Un GestorNivel por escena de nivel (Nivel1, Nivel2, Nivel3).
/// </summary>
public class GestorNivel : MonoBehaviour
{
    public static GestorNivel Instancia { get; private set; }

    [Header("Nivel")]
    [SerializeField] private int numeroNivel = 1;               // 1, 2 o 3
    [SerializeField] private float distanciaNivel = 300f;       // 300 / 500 / 800
    [SerializeField] private float porcentajeMinimo = 70f;
    [SerializeField] private bool colocarMetaAutomaticamente = true;
    [SerializeField] private string nombreEscenaMenu = "MenuInicio";

    [Header("Referencias de escena")]
    [SerializeField] private JugadorPichon jugador;
    [SerializeField] private Transform metaLlegada;

    [Header("HUD (TextMeshPro)")]
    [SerializeField] private TMP_Text textoPuntos;
    [SerializeField] private TMP_Text textoBotellas;
    [SerializeField] private TMP_Text textoReciclaje;
    [SerializeField] private TMP_Text textoVidas;
    [SerializeField] private TMP_Text textoDistancia;

    [Header("Pantalla de Victoria")]
    [SerializeField] private GameObject panelVictoria;
    [SerializeField] private TMP_Text textoResumenVictoria;
    [SerializeField] private TMP_Text textoDatoEducativo;
    [SerializeField] private GameObject botonSiguienteNivel;

    [Header("Pantalla de Game Over")]
    [SerializeField] private GameObject panelGameOver;
    [SerializeField] private TMP_Text textoMotivoGameOver;

    [Header("Datos educativos (uno por nivel, editables)")]
    [SerializeField, TextArea(2, 4)]
    private string[] datosEducativos =
    {
        "Las botellas PET pueden reciclarse y convertirse en nuevos envases o fibras textiles. Separarlas en el origen, como en los mercados, facilita su recolección.",
        "El río Shullcas abastece de agua a Huancayo. Los residuos sólidos que llegan a su cauce contaminan el agua y dañan a los seres vivos que dependen de ella.",
        "Un buen manejo de residuos empieza en casa: separar plástico, papel y orgánicos reduce lo que termina en las calles y botaderos de Huancayo y Chilca."
    };

    private bool terminado;

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instancia == this)
        {
            Instancia = null;
        }
    }

    private void Start()
    {
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(false);

        // Coloca la meta a la distancia exacta del nivel (1 unidad = 1 metro).
        if (colocarMetaAutomaticamente && metaLlegada != null && jugador != null)
        {
            Vector3 posicion = metaLlegada.position;
            posicion.x = jugador.transform.position.x + distanciaNivel;
            metaLlegada.position = posicion;
        }

        ActualizarHUD();
    }

    private void Update()
    {
        if (terminado) return;
        ActualizarHUD();
    }

    private void ActualizarHUD()
    {
        ControladorPuntaje puntaje = ControladorPuntaje.Instancia;
        if (puntaje == null) return;

        if (textoPuntos != null) textoPuntos.text = $"Puntos: {puntaje.Puntos}";
        if (textoBotellas != null) textoBotellas.text = $"Botellas: {puntaje.Botellas}/{puntaje.TotalBotellas}";
        if (textoReciclaje != null) textoReciclaje.text = $"Reciclaje: {puntaje.PorcentajeReciclaje:0}%";
        if (textoVidas != null) textoVidas.text = $"Vidas: {puntaje.Vidas}";

        if (textoDistancia != null && jugador != null && metaLlegada != null)
        {
            float restante = Mathf.Max(0f, metaLlegada.position.x - jugador.transform.position.x);
            textoDistancia.text = $"Meta: {restante:0} m";
        }
    }

    // ---------- Fin de nivel ----------

    /// <summary>Lo llama MetaLlegada cuando el jugador cruza la meta.</summary>
    public void LlegarAMeta()
    {
        if (terminado) return;

        ControladorPuntaje puntaje = ControladorPuntaje.Instancia;
        float porcentaje = puntaje != null ? puntaje.PorcentajeReciclaje : 0f;

        if (porcentaje >= porcentajeMinimo)
        {
            Victoria(porcentaje);
        }
        else
        {
            GameOver($"Recolectaste {porcentaje:0}% de las botellas. Necesitas al menos {porcentajeMinimo:0}%.");
        }
    }

    private void Victoria(float porcentaje)
    {
        terminado = true;
        ActualizarHUD();

        AudioManager.Victoria();   // SONIDO: victoria

        if (jugador != null) jugador.Detener();

        if (textoResumenVictoria != null)
        {
            int puntos = ControladorPuntaje.Instancia != null ? ControladorPuntaje.Instancia.Puntos : 0;
            textoResumenVictoria.text = $"¡Nivel {numeroNivel} completado!\nReciclaje: {porcentaje:0}%  |  Puntos: {puntos}";
        }

        if (textoDatoEducativo != null) textoDatoEducativo.text = ObtenerDatoEducativo();
        if (botonSiguienteNivel != null) botonSiguienteNivel.SetActive(HaySiguienteNivel());
        if (panelVictoria != null) panelVictoria.SetActive(true);

        Time.timeScale = 0f;
    }

    /// <summary>Se llama al llegar a la meta con poco reciclaje, al quedarse sin vidas o al caer al vacío.</summary>
    public void GameOver(string motivo)
    {
        if (terminado) return;

        terminado = true;
        ActualizarHUD();

        AudioManager.GameOver();   // SONIDO: game over (solo suena una vez gracias a 'terminado')

        if (jugador != null) jugador.Detener();
        if (textoMotivoGameOver != null) textoMotivoGameOver.text = motivo;
        if (panelGameOver != null) panelGameOver.SetActive(true);

        Time.timeScale = 0f;
    }

    private string ObtenerDatoEducativo()
    {
        if (datosEducativos == null || datosEducativos.Length == 0) return string.Empty;

        int indice = Mathf.Clamp(numeroNivel - 1, 0, datosEducativos.Length - 1);
        return datosEducativos[indice];
    }

    private bool HaySiguienteNivel()
    {
        return SceneManager.GetActiveScene().buildIndex + 1 < SceneManager.sceneCountInBuildSettings;
    }

    // ---------- Botones (asignar en OnClick) ----------

    public void SiguienteNivel()
    {
        Time.timeScale = 1f;

        if (HaySiguienteNivel())
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else
        {
            SceneManager.LoadScene(nombreEscenaMenu);
        }
    }

    public void Reintentar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscenaMenu);
    }
}
