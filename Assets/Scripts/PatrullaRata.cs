using UnityEngine;

/// <summary>
/// Patrulla horizontal de la rata: camina entre dos límites en el eje X,
/// se detiene un instante en cada extremo, gira el sprite y vuelve.
///
/// Dos formas de definir el recorrido:
///  1) Asignar "Limite Izquierdo" y "Limite Derecho" (objetos vacíos que NO sean hijos de la rata).
///  2) Si no se asignan, patrulla ± "Distancia Por Defecto" desde donde aparece
///     (útil cuando la rata nace desde un prefab con GeneradorObstaculos).
///
/// Si la rata tiene Rigidbody2D, ponlo en Body Type = Kinematic.
/// </summary>
public class PatrullaRata : MonoBehaviour
{
    [Header("Límites de patrulla (eje X)")]
    [SerializeField] private Transform limiteIzquierdo;
    [SerializeField] private Transform limiteDerecho;
    [SerializeField] private float distanciaPorDefecto = 2f;

    [Header("Movimiento")]
    [SerializeField] private float velocidad = 1.5f;
    [SerializeField] private float tiempoPausaEnExtremos = 0.4f;
    [SerializeField] private bool empezarHaciaDerecha = true;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool spriteMiraALaDerecha = false;   // marca esto si el dibujo original mira a la derecha

    private float xMinima;
    private float xMaxima;
    private int direccion = 1;          // 1 = derecha, -1 = izquierda
    private float temporizadorPausa;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning("PatrullaRata: no se encontró un SpriteRenderer en " + name + ". La rata no podrá girar.", this);
        }

        if ((limiteIzquierdo != null) != (limiteDerecho != null))
        {
            Debug.LogWarning("PatrullaRata: asigna los DOS límites o ninguno. Se usará la distancia por defecto.", this);
        }
    }

    private void Start()
    {
        ObtenerLimites(out xMinima, out xMaxima);

        // Si la rata nace fuera del recorrido, se acomoda dentro.
        Vector3 posicion = transform.position;
        posicion.x = Mathf.Clamp(posicion.x, xMinima, xMaxima);
        transform.position = posicion;

        direccion = empezarHaciaDerecha ? 1 : -1;
        AplicarGiro();
    }

    private void Update()
    {
        if (temporizadorPausa > 0f)
        {
            temporizadorPausa -= Time.deltaTime;
            return;
        }

        Vector3 posicion = transform.position;
        posicion.x += direccion * velocidad * Time.deltaTime;

        if (posicion.x >= xMaxima)
        {
            posicion.x = xMaxima;
            CambiarDireccion(-1);
        }
        else if (posicion.x <= xMinima)
        {
            posicion.x = xMinima;
            CambiarDireccion(1);
        }

        transform.position = posicion;
    }

    private void CambiarDireccion(int nuevaDireccion)
    {
        direccion = nuevaDireccion;
        temporizadorPausa = tiempoPausaEnExtremos;
        AplicarGiro();
    }

    private void AplicarGiro()
    {
        if (spriteRenderer == null) return;

        bool vaALaDerecha = direccion > 0;
        spriteRenderer.flipX = spriteMiraALaDerecha ? !vaALaDerecha : vaALaDerecha;
    }

    private void ObtenerLimites(out float minimo, out float maximo)
    {
        bool limitesValidos = limiteIzquierdo != null
                              && limiteDerecho != null
                              && !limiteIzquierdo.IsChildOf(transform)
                              && !limiteDerecho.IsChildOf(transform);

        if (limitesValidos)
        {
            minimo = Mathf.Min(limiteIzquierdo.position.x, limiteDerecho.position.x);
            maximo = Mathf.Max(limiteIzquierdo.position.x, limiteDerecho.position.x);
        }
        else
        {
            if (limiteIzquierdo != null && limiteDerecho != null)
            {
                Debug.LogWarning("PatrullaRata: los límites no pueden ser hijos de la rata (se moverían con ella). Se usará la distancia por defecto.", this);
            }

            float x = transform.position.x;
            minimo = x - Mathf.Abs(distanciaPorDefecto);
            maximo = x + Mathf.Abs(distanciaPorDefecto);
        }
    }

    private void OnDrawGizmosSelected()
    {
        float minimo;
        float maximo;

        if (Application.isPlaying)
        {
            minimo = xMinima;
            maximo = xMaxima;
        }
        else
        {
            ObtenerLimites(out minimo, out maximo);
        }

        float y = transform.position.y;
        Vector3 izquierda = new Vector3(minimo, y, 0f);
        Vector3 derecha = new Vector3(maximo, y, 0f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(izquierda, derecha);
        Gizmos.DrawWireSphere(izquierda, 0.15f);
        Gizmos.DrawWireSphere(derecha, 0.15f);
    }
}
