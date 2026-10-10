using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class MusicaEscena
{
    public string escena;      // nombre exacto de la escena, por ejemplo Nivel1
    public AudioClip musica;
}

// Se coloca UNA vez en la escena MenuInicio y se queda vivo en todas las escenas.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instancia { get; private set; }

    [Header("Música por escena")]
    [SerializeField] private MusicaEscena[] musicaPorEscena;
    [SerializeField] private AudioClip musicaPorDefecto;

    [Header("Efectos")]
    public AudioClip salto;
    public AudioClip botella;
    public AudioClip dano;
    public AudioClip victoria;
    public AudioClip gameOver;
    public AudioClip click;

    [Header("Volumen")]
    [Range(0f, 1f)] public float volumenMusica = 0.4f;
    [Range(0f, 1f)] public float volumenEfectos = 0.8f;

    private AudioSource fuenteMusica;
    private AudioSource fuenteEfectos;

    private void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        DontDestroyOnLoad(gameObject);

        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.loop = true;
        fuenteMusica.playOnAwake = false;
        fuenteMusica.volume = volumenMusica;

        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuenteEfectos.playOnAwake = false;

        SceneManager.sceneLoaded += AlCargarEscena;
        AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        Debug.Log("AudioManager listo. Escena: " + SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instancia == this) SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // Música de la escena
        AudioClip clip = musicaPorDefecto;
        foreach (var m in musicaPorEscena)
            if (m.escena == escena.name) { clip = m.musica; break; }

        if (clip == null)
            Debug.LogWarning("AudioManager: no hay música asignada para la escena " + escena.name);

        if (clip != null && fuenteMusica.clip != clip)
        {
            fuenteMusica.clip = clip;
            fuenteMusica.Play();
        }

        // Sonido de clic en TODOS los botones de la escena, automáticamente
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            b.onClick.AddListener(() => Efecto(click));
    }

    public void Efecto(AudioClip clip)
    {
        if (clip == null) { Debug.LogWarning("AudioManager: falta asignar un efecto en el Inspector."); return; }
        fuenteEfectos.PlayOneShot(clip, volumenEfectos);
    }

    // Atajos para llamar desde otros scripts
    private static bool Listo()
    {
        if (Instancia == null)
        {
            Debug.LogWarning("AudioManager: no existe en la escena. Empieza el juego desde MenuInicio.");
            return false;
        }
        return true;
    }

    public static void Salto()    { if (Listo()) Instancia.Efecto(Instancia.salto); }
    public static void Botella()  { if (Listo()) Instancia.Efecto(Instancia.botella); }
    public static void Dano()     { if (Listo()) Instancia.Efecto(Instancia.dano); }
    public static void Victoria() { if (Listo()) Instancia.Efecto(Instancia.victoria); }
    public static void GameOver() { if (Listo()) Instancia.Efecto(Instancia.gameOver); }
}
