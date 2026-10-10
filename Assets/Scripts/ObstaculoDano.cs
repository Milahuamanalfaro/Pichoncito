using UnityEngine;

/// <summary>
/// Se añade a 'Rata' (suelo), 'Obstaculo_Smog' y 'NubeToxica' (aire).
/// Funciona con colisión sólida o con trigger.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ObstaculoDano : MonoBehaviour
{
    public enum TipoObstaculo
    {
        Rata,
        Smog
    }

    [SerializeField] private TipoObstaculo tipo = TipoObstaculo.Rata;
    [SerializeField] private int dano = 1;

    public TipoObstaculo Tipo => tipo;
    public int Dano => dano;
}
