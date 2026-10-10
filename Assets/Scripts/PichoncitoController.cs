using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PichoncitoController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidadHorizontal = 6f;

    [Header("Salto")]
    [SerializeField] private float fuerzaSalto = 9f;

    [Header("Planeo con capa")]
    [SerializeField] private float gravedadNormal = 3f;
    [SerializeField] private float gravedadPlaneo = 0.5f;
    [SerializeField] private float velocidadMaxCaidaPlaneo = -2f;

    [Header("Detección de suelo")]
    [SerializeField] private Transform puntoSuelo;
    [SerializeField] private float radioSuelo = 0.2f;
    [SerializeField] private LayerMask capaSuelo;

    [Header("Knockback")]
    [SerializeField] private float fuerzaRetroceso = 4f;
    [SerializeField] private float duracionKnockback = 0.25f;

    [Header("Diagnóstico (temporal)")]
    [SerializeField] private bool mostrarDiagnostico = true;

    private Rigidbody2D rb;
    private bool enSuelo;
    private bool saltoSolicitado;
    private bool manteniendoSalto;
    private float temporizadorKnockback;
    private GUIStyle estiloDiagnostico;

    public bool EnSuelo => enSuelo;
    public bool EstaPlaneando { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravedadNormal;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        bool teclaSalto = Input.GetKey(KeyCode.Space)
                       || Input.GetKey(KeyCode.UpArrow)
                       || Input.GetKey(KeyCode.W);

        bool teclaSaltoPresionada = Input.GetKeyDown(KeyCode.Space)
                                 || Input.GetKeyDown(KeyCode.UpArrow)
                                 || Input.GetKeyDown(KeyCode.W);

        manteniendoSalto = teclaSalto;

        if (teclaSaltoPresionada)
        {
            Debug.Log("Tecla de salto detectada. EnSuelo = " + enSuelo);
        }

        if (teclaSaltoPresionada && enSuelo)
        {
            saltoSolicitado = true;
        }

        if (temporizadorKnockback > 0f)
        {
            temporizadorKnockback -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        enSuelo = Physics2D.OverlapCircle(puntoSuelo.position, radioSuelo, capaSuelo);

        // Movimiento horizontal (retroceso temporal si hubo daño)
        float velocidadX = temporizadorKnockback > 0f ? -fuerzaRetroceso : velocidadHorizontal;
        rb.linearVelocity = new Vector2(velocidadX, rb.linearVelocity.y);

        // Salto
        if (saltoSolicitado)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
            saltoSolicitado = false;
        }

        // Planeo: salto mantenido, en el aire y cayendo
        EstaPlaneando = !enSuelo && manteniendoSalto && rb.linearVelocity.y < 0f;

        if (EstaPlaneando)
        {
            rb.gravityScale = gravedadPlaneo;
            if (rb.linearVelocity.y < velocidadMaxCaidaPlaneo)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, velocidadMaxCaidaPlaneo);
            }
        }
        else
        {
            rb.gravityScale = gravedadNormal;
        }
    }

    public void AplicarKnockback()
    {
        temporizadorKnockback = duracionKnockback;
    }

    private void OnGUI()
    {
        if (!mostrarDiagnostico || rb == null) return;

        if (estiloDiagnostico == null)
        {
            estiloDiagnostico = new GUIStyle(GUI.skin.label);
            estiloDiagnostico.fontSize = 22;
            estiloDiagnostico.normal.textColor = Color.white;
        }

        GUI.Label(new Rect(10, 10, 900, 30), "EnSuelo: " + enSuelo + "   |   Planeando: " + EstaPlaneando, estiloDiagnostico);
        GUI.Label(new Rect(10, 40, 900, 30), "Tecla de salto mantenida: " + manteniendoSalto, estiloDiagnostico);
        GUI.Label(new Rect(10, 70, 900, 30), "Velocidad Y: " + rb.linearVelocity.y.ToString("F2"), estiloDiagnostico);
    }

    private void OnDrawGizmosSelected()
    {
        if (puntoSuelo == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(puntoSuelo.position, radioSuelo);
    }
}
