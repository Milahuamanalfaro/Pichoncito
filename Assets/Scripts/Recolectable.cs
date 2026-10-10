using UnityEngine;

/// <summary>
/// Botella / ItemReciclable. Requiere un Collider2D con "Is Trigger" activado.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Recolectable : MonoBehaviour
{
    [SerializeField] private int valor = 1;

    private bool recogido;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (recogido || !otro.CompareTag("Player")) return;

        recogido = true;

        if (ControladorPuntaje.Instancia != null)
        {
            ControladorPuntaje.Instancia.SumarBotella(valor);
        }

        AudioManager.Botella();   // SONIDO: botella recogida

        Destroy(gameObject);
    }
}
