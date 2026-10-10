using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú de inicio: Jugar, Cómo Jugar (pop-up de controles) y Salir.
/// Escena "MenuInicio" (índice 0 en Build Settings).
/// </summary>
public class MenuInicio : MonoBehaviour
{
    [SerializeField] private string nombreEscenaNivel1 = "Nivel1";
    [SerializeField] private GameObject panelComoJugar;

    private void Start()
    {
        Time.timeScale = 1f;

        if (panelComoJugar != null)
        {
            panelComoJugar.SetActive(false);
        }
    }

    public void Jugar()
    {
        SceneManager.LoadScene(nombreEscenaNivel1);
    }

    public void AbrirComoJugar()
    {
        if (panelComoJugar != null) panelComoJugar.SetActive(true);
    }

    public void CerrarComoJugar()
    {
        if (panelComoJugar != null) panelComoJugar.SetActive(false);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
