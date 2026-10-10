using UnityEngine;

/// <summary>
/// Se añade al objeto "Meta_Llegada". Requiere un Collider2D con "Is Trigger" activado.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MetaLlegada : MonoBehaviour
{
    private bool activada;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (activada || !otro.CompareTag("Player")) return;

        activada = true;

        if (GestorNivel.Instancia != null)
        {
            GestorNivel.Instancia.LlegarAMeta();
        }
    }
}
