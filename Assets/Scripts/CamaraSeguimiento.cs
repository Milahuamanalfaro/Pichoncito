using UnityEngine;

/// <summary>
/// Cámara 2D que sigue al jugador solo en el eje X (Y y Z se mantienen fijos).
/// </summary>
public class CamaraSeguimiento : MonoBehaviour
{
    [SerializeField] private Transform objetivo;
    [SerializeField] private float desplazamientoX = 4f;

    private void LateUpdate()
    {
        if (objetivo == null) return;

        Vector3 posicion = transform.position;
        posicion.x = objetivo.position.x + desplazamientoX;
        transform.position = posicion;
    }
}
