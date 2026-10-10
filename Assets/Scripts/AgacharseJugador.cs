using UnityEngine;

// Pichon2D tiene el Sprite Renderer, el Rigidbody2D y el Capsule Collider en el mismo objeto,
// así que se agacha escalando el objeto en Y y manteniendo los pies en el mismo sitio.
[RequireComponent(typeof(CapsuleCollider2D))]
public class AgacharseJugador : MonoBehaviour
{
    [Header("Teclas")]
    [SerializeField] private KeyCode teclaAgacharse = KeyCode.DownArrow;
    [SerializeField] private KeyCode teclaAlternativa = KeyCode.S;

    [Header("Agacharse")]
    [SerializeField, Range(0.3f, 0.9f)] private float factorAltura = 0.5f;

    private CapsuleCollider2D capsula;
    private float escalaYOriginal;

    public bool EstaAgachado { get; private set; }

    private void Awake()
    {
        capsula = GetComponent<CapsuleCollider2D>();
        escalaYOriginal = transform.localScale.y;
    }

    private float PiesY(float escalaY)
    {
        // Posición mundial de los pies del collider para una escala Y dada
        return transform.position.y + (capsula.offset.y - capsula.size.y / 2f) * escalaY;
    }

    private void Update()
    {
        bool quiere = Input.GetKey(teclaAgacharse) || Input.GetKey(teclaAlternativa);

        if (quiere && !EstaAgachado) CambiarAltura(escalaYOriginal * factorAltura, true);
        else if (!quiere && EstaAgachado) CambiarAltura(escalaYOriginal, false);
    }

    private void CambiarAltura(float nuevaEscalaY, bool agachado)
    {
        float piesAntes = PiesY(transform.localScale.y);

        Vector3 e = transform.localScale;
        e.y = nuevaEscalaY;
        transform.localScale = e;

        float piesDespues = PiesY(nuevaEscalaY);
        transform.position += new Vector3(0f, piesAntes - piesDespues, 0f);  // los pies no se mueven

        Physics2D.SyncTransforms();
        EstaAgachado = agachado;
    }
}
