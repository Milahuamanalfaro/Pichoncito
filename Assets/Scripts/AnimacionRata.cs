using UnityEngine;

[DisallowMultipleComponent]
public class AnimacionRata : MonoBehaviour
{
    [Header("Referencia")]
    [Tooltip("Hijo que contiene el SpriteRenderer. Si se deja vacío, se busca automáticamente.")]
    [SerializeField] private Transform visual;

    [Header("Rebote (trote)")]
    [SerializeField] private float velocidadTrote = 9f;
    [SerializeField] private float alturaRebote = 0.06f;

    [Header("Aplastar y estirar")]
    [SerializeField] private float cantidadEstiramiento = 0.06f;

    [Header("Balanceo")]
    [SerializeField] private float anguloBalanceo = 3f;

    private Vector3 posicionInicial;
    private Vector3 escalaInicial;
    private float desfase;

    private void Awake()
    {
        if (visual == null)
        {
            SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
            {
                visual = sprite.transform;
            }
        }

        if (visual == null)
        {
            Debug.LogError("[AnimacionRata] No se encontró ningún SpriteRenderer. Se desactiva la animación.", this);
            enabled = false;
            return;
        }

        if (visual == transform)
        {
            Debug.LogWarning("[AnimacionRata] El sprite está en el mismo objeto que el collider; " +
                             "el collider también se moverá. Conviene usar un hijo 'Visual'.", this);
        }

        posicionInicial = visual.localPosition;
        escalaInicial = visual.localScale;

        // Cada rata empieza en un punto distinto para que no se muevan todas igual
        desfase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float t = Time.time * velocidadTrote + desfase;

        // 0 = apoyada en el piso, 1 = en lo más alto del salto
        float rebote = Mathf.Abs(Mathf.Sin(t));

        visual.localPosition = posicionInicial + Vector3.up * (rebote * alturaRebote);

        // Aplastada al tocar el piso, estirada en el aire
        float estiramiento = (rebote - 0.5f) * 2f * cantidadEstiramiento;
        visual.localScale = new Vector3(
            escalaInicial.x * (1f - estiramiento * 0.5f),
            escalaInicial.y * (1f + estiramiento),
            escalaInicial.z);

        visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.5f) * anguloBalanceo);
    }
}
