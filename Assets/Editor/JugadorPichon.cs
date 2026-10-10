using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Control del jugador "Pichon2D": avance automático a la derecha, salto variable,
/// planeo con capa, daño con knockback e invulnerabilidad temporal.
/// Requiere el Tag "Player" y la capa "Suelo" configurada en el GroundCheck.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class JugadorPichon : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidadAvance = 5f;   // Nivel 1: 5 | Nivel 2: 6 | Nivel 3: 8

    [Header("Salto")]
    [SerializeField] private float fuerzaSalto = 12f;
    [SerializeField, Range(0.1f, 1f)] private float factorCorteSalto = 0.5f; // Al soltar la tecla en ascenso
    [SerializeField] private float gravedadNormal = 3f;

    [Header("Planeo con capa")]
    [SerializeField] private float gravedadPlaneo = 0.5f;

    [Header("Detección de suelo")]
    [SerializeField] private Transform puntoSuelo;
    [SerializeField] private float radioSuelo = 0.15f;
    [SerializeField] private LayerMask capaSuelo;

    [Header("Daño")]
    [SerializeField] private Vector2 fuerzaKnockback = new Vector2(-4f, 6f);
    [SerializeField] private float duracionKnockback = 0.3f;
    [SerializeField] private float duracionInvulnerable = 1.2f;
    [SerializeField] private float limiteCaidaY = -8f;

    [Header("Visual")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // Hashes del Animator (parámetros opcionales: si no existen, se ignoran).
    private static readonly int HashEnSuelo = Animator.StringToHash("EnSuelo");
    private static readonly int HashVelocidadY = Animator.StringToHash("VelocidadY");
    private static readonly int HashPlaneando = Animator.StringToHash("Planeando");
    private static readonly int HashDano = Animator.StringToHash("Dano");
    private static readonly int HashDetenido = Animator.StringToHash("Detenido");

    private Rigidbody2D rb;
    private readonly HashSet<int> parametrosAnimator = new HashSet<int>();

    private bool activo = true;
    private bool enSuelo;
    private bool planeando;
    private bool saltoPedido;
    private bool saltoSoltado;
    private bool saltoMantenido;
    private float tiempoKnockback;
    private float tiempoInvulnerable;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravedadNormal;
        rb.freezeRotation = true;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter parametro in animator.parameters)
            {
                parametrosAnimator.Add(parametro.nameHash);
            }
        }
    }

    private void Update()
    {
        if (!activo) return;

        // Entrada (Espacio / W / Flecha Arriba). Requiere Active Input Handling = Both o Legacy.
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            saltoPedido = true;
        }

        if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow))
        {
            saltoSoltado = true;
        }

        saltoMantenido = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);

        if (tiempoInvulnerable > 0f)
        {
            tiempoInvulnerable -= Time.deltaTime;
        }

        // Caída al vacío (por ejemplo, huecos del río en el Nivel 2).
        if (transform.position.y < limiteCaidaY && GestorNivel.Instancia != null)
        {
            GestorNivel.Instancia.GameOver("Caíste al vacío. ¡Usa el planeo para cruzar!");
            return;
        }

        ActualizarAnimador();
    }

    private void FixedUpdate()
    {
        if (!activo) return;

        // Detección de suelo con OverlapCircle2D.
        Vector2 centro = puntoSuelo != null ? (Vector2)puntoSuelo.position : (Vector2)transform.position;
        enSuelo = Physics2D.OverlapCircle(centro, radioSuelo, capaSuelo) != null;

        Vector2 velocidad = rb.linearVelocity;

        // Avance automático (se pausa durante el knockback).
        if (tiempoKnockback > 0f)
        {
            tiempoKnockback -= Time.fixedDeltaTime;
        }
        else
        {
            velocidad.x = velocidadAvance;
        }

        // Salto.
        if (saltoPedido && enSuelo)
        {
            velocidad.y = fuerzaSalto;
            enSuelo = false;
        }
        saltoPedido = false;

        // Salto variable: soltar la tecla mientras sube recorta el salto.
        if (saltoSoltado && velocidad.y > 0f)
        {
            velocidad.y *= factorCorteSalto;
        }
        saltoSoltado = false;

        // Planeo: mantener salto en el aire mientras cae reduce la gravedad.
        planeando = !enSuelo && saltoMantenido && velocidad.y <= 0f;
        rb.gravityScale = planeando ? gravedadPlaneo : gravedadNormal;

        rb.linearVelocity = velocidad;
    }

    // ---------- Colisiones 2D ----------

    private void OnCollisionEnter2D(Collision2D colision)
    {
        IntentarRecibirDano(colision.collider);
    }

    private void OnCollisionStay2D(Collision2D colision)
    {
        IntentarRecibirDano(colision.collider);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        IntentarRecibirDano(otro);
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        IntentarRecibirDano(otro);
    }

    private void IntentarRecibirDano(Collider2D otro)
    {
        if (!activo || tiempoInvulnerable > 0f) return;

        ObstaculoDano obstaculo = otro.GetComponentInParent<ObstaculoDano>();
        if (obstaculo == null) return;

        AplicarDano(obstaculo.Dano);
    }

    private void AplicarDano(int dano)
    {
        bool sinVidas = ControladorPuntaje.Instancia != null &&
                        ControladorPuntaje.Instancia.RecibirDano(dano);

        tiempoInvulnerable = duracionInvulnerable;
        tiempoKnockback = duracionKnockback;
        rb.linearVelocity = fuerzaKnockback;

        if (animator != null && parametrosAnimator.Contains(HashDano))
        {
            animator.SetTrigger(HashDano);
        }

        if (sinVidas)
        {
            if (GestorNivel.Instancia != null)
            {
                GestorNivel.Instancia.GameOver("Te quedaste sin vidas.");
            }
            return;
        }

        StartCoroutine(Parpadeo());
    }

    private IEnumerator Parpadeo()
    {
        while (tiempoInvulnerable > 0f)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
        }

        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }

    // ---------- API pública ----------

    /// <summary>Detiene el control del jugador (lo llama GestorNivel al terminar el nivel).</summary>
    public void Detener()
    {
        activo = false;
        StopAllCoroutines();
        if (spriteRenderer != null) spriteRenderer.enabled = true;

        // Al detenerse (fin de nivel o Game Over) el Animator pasa a Idle.
        if (animator != null && parametrosAnimator.Contains(HashDetenido))
        {
            animator.SetBool(HashDetenido, true);
        }

        rb.linearVelocity = Vector2.zero;
    }

    // ---------- Animator ----------

    private void ActualizarAnimador()
    {
        if (animator == null) return;

        if (parametrosAnimator.Contains(HashEnSuelo)) animator.SetBool(HashEnSuelo, enSuelo);
        if (parametrosAnimator.Contains(HashVelocidadY)) animator.SetFloat(HashVelocidadY, rb.linearVelocity.y);
        if (parametrosAnimator.Contains(HashPlaneando)) animator.SetBool(HashPlaneando, planeando);
    }

    private void OnDrawGizmosSelected()
    {
        if (puntoSuelo == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(puntoSuelo.position, radioSuelo);
    }
}
