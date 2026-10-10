#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Generador automático de "Pichoncito: Guardián Ecológico de Chilca".
/// Menú: Pichoncito → Generar TODO el juego (Menú + Niveles 1-3).
/// Requisito: abrir la escena que ya tiene los objetos "Pichon2D" y "Suelo".
/// </summary>
public static class GeneradorPichoncito
{
    private const string CarpetaPrefabs = "Assets/Prefabs";
    private const string CarpetaEscenas = "Assets/Scenes";
    private const string CarpetaSprites = "Assets/Sprites";
    private const string RutaSprite = "Assets/Sprites/Cuadrado.png";
    private const string RutaMaterialFisico = "Assets/Prefabs/SinFriccion.asset";
    private const string EscenaMenu = "Assets/Scenes/MenuInicio.unity";

    private static GameObject prefabBotella;
    private static GameObject prefabRata;
    private static GameObject prefabSmog;
    private static Sprite spriteCuadrado;
    private static Material materialSprite;
    private static int capaSuelo;

    // Estado del nivel que se está generando.
    private static Transform contenedor;
    private static float yPiso;

    // ======================================================================
    //  PUNTO DE ENTRADA
    // ======================================================================

    [MenuItem("Pichoncito/Generar TODO el juego (Menú + Niveles 1-3)")]
    public static void GenerarTodo()
    {
        if (!VerificarTMP()) return;

        if (GameObject.Find("Pichon2D") == null || GameObject.Find("Suelo") == null)
        {
            EditorUtility.DisplayDialog("Pichoncito",
                "Abre la escena donde están los objetos 'Pichon2D' y 'Suelo' (tu SampleScene) y vuelve a ejecutar.",
                "OK");
            return;
        }

        bool continuar = EditorUtility.DisplayDialog("Pichoncito",
            "Se crearán prefabs, las escenas Nivel1, Nivel2, Nivel3 y MenuInicio dentro de Assets/.\n" +
            "La escena actual se guardará como Nivel1.\n\n¿Continuar?",
            "Sí, generar", "Cancelar");
        if (!continuar) return;

        AsegurarCarpeta(CarpetaPrefabs);
        AsegurarCarpeta(CarpetaEscenas);
        AsegurarCarpeta(CarpetaSprites);

        PrepararRecursos();

        // Nivel 1 en la escena actual.
        if (!GenerarNivel(1)) return;
        string rutaNivel1 = RutaNivel(1);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), rutaNivel1);

        // Niveles 2 y 3: copia de Nivel1 y se regenera su contenido.
        for (int nivel = 2; nivel <= 3; nivel++)
        {
            string destino = RutaNivel(nivel);
            if (File.Exists(destino)) AssetDatabase.DeleteAsset(destino);

            AssetDatabase.CopyAsset(rutaNivel1, destino);
            EditorSceneManager.OpenScene(destino, OpenSceneMode.Single);

            if (!GenerarNivel(nivel)) return;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        CrearEscenaMenu();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(EscenaMenu, true),
            new EditorBuildSettingsScene(RutaNivel(1), true),
            new EditorBuildSettingsScene(RutaNivel(2), true),
            new EditorBuildSettingsScene(RutaNivel(3), true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(EscenaMenu, OpenSceneMode.Single);

        EditorUtility.DisplayDialog("Pichoncito",
            "¡Listo! Se generaron MenuInicio, Nivel1, Nivel2 y Nivel3 y se configuraron los Build Settings.\n\n" +
            "Dale Play desde MenuInicio.",
            "OK");
    }

    private static string RutaNivel(int nivel)
    {
        return $"{CarpetaEscenas}/Nivel{nivel}.unity";
    }

    // ======================================================================
    //  RECURSOS COMPARTIDOS (layer, sprite, prefabs)
    // ======================================================================

    private static bool VerificarTMP()
    {
        if (TMP_Settings.instance != null) return true;

        EditorUtility.DisplayDialog("Pichoncito",
            "Falta importar los recursos de TextMeshPro.\n\n" +
            "Se abrirá la ventana de importación: pulsa 'Import TMP Essentials' y luego " +
            "vuelve a ejecutar Pichoncito → Generar TODO el juego.",
            "OK");
        EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
        return false;
    }

    private static void PrepararRecursos()
    {
        capaSuelo = AsegurarLayer("Suelo");
        spriteCuadrado = ObtenerSpriteCuadrado();

        SpriteRenderer srSuelo = GameObject.Find("Suelo").GetComponentInChildren<SpriteRenderer>();
        materialSprite = srSuelo != null ? srSuelo.sharedMaterial : null;

        prefabBotella = CrearPrefab("Botella", new Color(0.2f, 0.85f, 0.35f), new Vector3(0.35f, 0.5f, 1f), true, ObstaculoDano.TipoObstaculo.Rata);
        prefabRata = CrearPrefab("Rata", new Color(0.55f, 0.55f, 0.55f), new Vector3(0.7f, 0.4f, 1f), false, ObstaculoDano.TipoObstaculo.Rata);
        prefabSmog = CrearPrefab("Obstaculo_Smog", new Color(0.3f, 0.3f, 0.35f, 0.9f), new Vector3(1.2f, 1.2f, 1f), false, ObstaculoDano.TipoObstaculo.Smog);
    }

    private static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;

        int separador = ruta.LastIndexOf('/');
        string padre = ruta.Substring(0, separador);
        string nombre = ruta.Substring(separador + 1);
        AssetDatabase.CreateFolder(padre, nombre);
    }

    private static int AsegurarLayer(string nombre)
    {
        int existente = LayerMask.NameToLayer(nombre);
        if (existente >= 0) return existente;

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty capas = tagManager.FindProperty("layers");

        for (int i = 8; i < 32; i++)
        {
            SerializedProperty capa = capas.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(capa.stringValue))
            {
                capa.stringValue = nombre;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        Debug.LogError("No hay layers libres para crear 'Suelo'.");
        return 0;
    }

    private static Sprite ObtenerSpriteCuadrado()
    {
        Sprite existente = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite);
        if (existente != null) return existente;

        Texture2D textura = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color32[] pixeles = new Color32[16];
        for (int i = 0; i < pixeles.Length; i++) pixeles[i] = new Color32(255, 255, 255, 255);
        textura.SetPixels32(pixeles);
        textura.Apply();

        File.WriteAllBytes(RutaSprite, textura.EncodeToPNG());
        Object.DestroyImmediate(textura);
        AssetDatabase.ImportAsset(RutaSprite);

        TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(RutaSprite);
        importador.textureType = TextureImporterType.Sprite;
        importador.spritePixelsPerUnit = 4f;      // 4 px = 1 unidad → el cuadrado mide 1x1
        importador.filterMode = FilterMode.Point;
        importador.mipmapEnabled = false;
        importador.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite);
    }

    private static GameObject CrearPrefab(string nombre, Color color, Vector3 escala, bool esBotella,
                                          ObstaculoDano.TipoObstaculo tipo)
    {
        GameObject go = new GameObject(nombre);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteCuadrado;
        sr.color = color;
        sr.sortingOrder = 1;
        if (materialSprite != null) sr.sharedMaterial = materialSprite;

        go.transform.localScale = escala;

        BoxCollider2D caja = go.AddComponent<BoxCollider2D>();
        caja.size = Vector2.one;
        caja.isTrigger = true;

        if (esBotella)
        {
            go.AddComponent<Recolectable>();
        }
        else
        {
            ObstaculoDano obstaculo = go.AddComponent<ObstaculoDano>();
            SerializedObject so = new SerializedObject(obstaculo);
            so.FindProperty("tipo").enumValueIndex = (int)tipo;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{CarpetaPrefabs}/{nombre}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ======================================================================
    //  GENERACIÓN DE UN NIVEL EN LA ESCENA ACTIVA
    // ======================================================================

    private static bool GenerarNivel(int nivel)
    {
        float distancia;
        float velocidad;
        float paso;
        string[] patron;

        switch (nivel)
        {
            case 1:
                distancia = 300f; velocidad = 5f; paso = 19f;
                patron = new[] { "rata" };
                break;
            case 2:
                distancia = 500f; velocidad = 6f; paso = 35f;
                patron = new[] { "hueco", "smog" };
                break;
            default:
                distancia = 800f; velocidad = 8f; paso = 16f;
                patron = new[] { "rata", "smog", "par", "rata", "par" };
                break;
        }

        // Limpieza para poder regenerar sin duplicar objetos.
        EliminarPorNombre("Nivel_Objetos");
        EliminarPorNombre("Meta_Llegada");
        EliminarPorNombre("Canvas_Pichoncito");
        EliminarPorNombre("GestorNivel");
        EliminarPorNombre("ControladorPuntaje");
        EliminarPorNombre("Suelo_Seg");

        GameObject pichon = GameObject.Find("Pichon2D");
        GameObject suelo = GameObject.Find("Suelo");
        if (pichon == null || suelo == null)
        {
            EditorUtility.DisplayDialog("Pichoncito", "No se encontró 'Pichon2D' o 'Suelo' en la escena.", "OK");
            return false;
        }

        SpriteRenderer srSuelo = suelo.GetComponentInChildren<SpriteRenderer>();
        if (srSuelo == null || srSuelo.sprite == null)
        {
            EditorUtility.DisplayDialog("Pichoncito", "'Suelo' necesita un SpriteRenderer con sprite.", "OK");
            return false;
        }

        yPiso = srSuelo.bounds.max.y;
        float inicioX = pichon.transform.position.x;

        contenedor = new GameObject("Nivel_Objetos").transform;

        // ---- Obstáculos, botellas y huecos ----
        List<Vector2> huecos = new List<Vector2>();
        float anchoHueco = 6.5f;
        float x = inicioX + 25f;
        float limite = inicioX + distancia - 25f;
        int indice = 0;

        while (x < limite)
        {
            string tipo = patron[indice % patron.Length];

            switch (tipo)
            {
                case "rata":
                    PonerRata(x);
                    PonerArcoBotellas(x);
                    break;

                case "smog":
                    PonerSmog(x);
                    for (int k = -1; k <= 1; k++) PonerBotella(x + k * 1.2f, yPiso + 0.45f);
                    break;

                case "par":
                    PonerRata(x);
                    PonerArcoBotellas(x);
                    PonerSmog(x + 7.5f);
                    break;

                case "hueco":
                    huecos.Add(new Vector2(x, x + anchoHueco));
                    for (int k = 0; k < 5; k++) PonerBotella(x + 0.9f + k * 1.2f, yPiso + 1.8f);
                    break;
            }

            x += paso;
            indice++;
        }

        // ---- Segmentos de suelo (con huecos de río en el Nivel 2) ----
        ConstruirSuelo(suelo, srSuelo, inicioX - 12f, inicioX + distancia + 40f, huecos);

        // ---- Jugador, cámara, meta, controladores e interfaz ----
        JugadorPichon jugador = ConfigurarJugador(pichon, velocidad);
        ConfigurarCamara(pichon, inicioX);
        Transform meta = CrearMeta(inicioX + distancia);

        new GameObject("ControladorPuntaje").AddComponent<ControladorPuntaje>();
        CrearInterfaz(jugador, meta, nivel, distancia);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Pichoncito] Nivel {nivel} generado ({distancia} m, velocidad {velocidad}).");
        return true;
    }

    private static void EliminarPorNombre(string nombre)
    {
        GameObject encontrado = GameObject.Find(nombre);
        while (encontrado != null)
        {
            Object.DestroyImmediate(encontrado);
            encontrado = GameObject.Find(nombre);
        }
    }

    // ---------- Colocación de objetos ----------

    private static GameObject Colocar(GameObject prefab, Vector3 posicion)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, contenedor);
        go.transform.position = posicion;
        return go;
    }

    private static void PonerBotella(float x, float y)
    {
        Colocar(prefabBotella, new Vector3(x, y, 0f));
    }

    private static void PonerRata(float x)
    {
        Colocar(prefabRata, new Vector3(x, yPiso + 0.2f, 0f));
    }

    private static void PonerSmog(float x)
    {
        Colocar(prefabSmog, new Vector3(x, yPiso + 1.7f, 0f));
    }

    private static void PonerArcoBotellas(float x)
    {
        PonerBotella(x - 1.6f, yPiso + 1.0f);
        PonerBotella(x, yPiso + 1.9f);
        PonerBotella(x + 1.6f, yPiso + 1.0f);
    }

    // ---------- Suelo ----------

    private static void ConstruirSuelo(GameObject sueloBase, SpriteRenderer srBase,
                                       float inicio, float fin, List<Vector2> huecos)
    {
        List<Vector2> segmentos = new List<Vector2>();
        float cursor = inicio;

        foreach (Vector2 hueco in huecos)
        {
            segmentos.Add(new Vector2(cursor, hueco.x));
            cursor = hueco.y;
        }
        segmentos.Add(new Vector2(cursor, fin));

        Sprite sprite = srBase.sprite;
        float anchoSprite = sprite.bounds.size.x;

        for (int i = 0; i < segmentos.Count; i++)
        {
            GameObject segmento;
            if (i == 0)
            {
                segmento = sueloBase;
            }
            else
            {
                segmento = Object.Instantiate(sueloBase);
                segmento.name = "Suelo_Seg";
            }

            float ancho = segmentos[i].y - segmentos[i].x;
            float centroX = (segmentos[i].x + segmentos[i].y) * 0.5f;

            Vector3 escala = segmento.transform.localScale;
            escala.x = ancho / anchoSprite;
            segmento.transform.localScale = escala;

            Vector3 posicion = segmento.transform.position;
            posicion.x = centroX;
            segmento.transform.position = posicion;

            if (segmento.GetComponent<Collider2D>() == null)
            {
                BoxCollider2D caja = segmento.AddComponent<BoxCollider2D>();
                caja.size = sprite.bounds.size;
                caja.offset = sprite.bounds.center;
            }

            foreach (Transform t in segmento.GetComponentsInChildren<Transform>())
            {
                t.gameObject.layer = capaSuelo;
            }
        }
    }

    // ---------- Jugador y cámara ----------

    private static JugadorPichon ConfigurarJugador(GameObject pichon, float velocidad)
    {
        pichon.tag = "Player";

        Rigidbody2D rb = pichon.GetComponent<Rigidbody2D>();
        if (rb == null) rb = pichon.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        SpriteRenderer srJugador = pichon.GetComponentInChildren<SpriteRenderer>();

        // Debe haber un CapsuleCollider2D; se quitan colliders de otro tipo.
        foreach (Collider2D existente in pichon.GetComponents<Collider2D>())
        {
            if (!(existente is CapsuleCollider2D)) Object.DestroyImmediate(existente);
        }

        CapsuleCollider2D capsula = pichon.GetComponent<CapsuleCollider2D>();
        if (capsula == null)
        {
            capsula = pichon.AddComponent<CapsuleCollider2D>();
            if (srJugador != null)
            {
                Bounds mundo = srJugador.bounds;
                Vector3 centroLocal = pichon.transform.InverseTransformPoint(mundo.center);
                Vector3 escala = pichon.transform.lossyScale;

                capsula.direction = CapsuleDirection2D.Vertical;
                capsula.offset = new Vector2(centroLocal.x, centroLocal.y);
                capsula.size = new Vector2(
                    Mathf.Abs(mundo.size.x / escala.x) * 0.85f,
                    Mathf.Abs(mundo.size.y / escala.y));
            }
        }

        // Material sin fricción para que no se pegue a paredes.
        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(RutaMaterialFisico);
        if (material == null)
        {
            material = new PhysicsMaterial2D("SinFriccion") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, RutaMaterialFisico);
        }
        capsula.sharedMaterial = material;

        JugadorPichon jugador = pichon.GetComponent<JugadorPichon>();
        if (jugador == null) jugador = pichon.AddComponent<JugadorPichon>();

        // GroundCheck en los pies.
        Transform groundCheck = pichon.transform.Find("GroundCheck");
        if (groundCheck == null)
        {
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(pichon.transform);
        }
        Bounds limites = srJugador != null ? srJugador.bounds : capsula.bounds;
        groundCheck.position = new Vector3(limites.center.x, limites.min.y - 0.02f, 0f);

        SerializedObject so = new SerializedObject(jugador);
        so.FindProperty("velocidadAvance").floatValue = velocidad;
        so.FindProperty("puntoSuelo").objectReferenceValue = groundCheck;
        so.FindProperty("capaSuelo").intValue = 1 << capaSuelo;
        so.FindProperty("spriteRenderer").objectReferenceValue = srJugador;
        so.ApplyModifiedProperties();

        return jugador;
    }

    private static void ConfigurarCamara(GameObject pichon, float inicioX)
    {
        Camera camara = Camera.main;
        if (camara == null)
        {
            GameObject go = GameObject.Find("Main Camera");
            if (go != null) camara = go.GetComponent<Camera>();
        }
        if (camara == null) return;

        CamaraSeguimiento seguimiento = camara.GetComponent<CamaraSeguimiento>();
        if (seguimiento == null) seguimiento = camara.gameObject.AddComponent<CamaraSeguimiento>();

        SerializedObject so = new SerializedObject(seguimiento);
        so.FindProperty("objetivo").objectReferenceValue = pichon.transform;
        so.FindProperty("desplazamientoX").floatValue = 4f;
        so.ApplyModifiedProperties();

        camara.orthographic = true;
        camara.transform.position = new Vector3(inicioX + 4f, yPiso + 3f, -10f);
    }

    private static Transform CrearMeta(float posicionX)
    {
        GameObject meta = new GameObject("Meta_Llegada");

        SpriteRenderer sr = meta.AddComponent<SpriteRenderer>();
        sr.sprite = spriteCuadrado;
        sr.color = new Color(1f, 0.85f, 0.2f, 0.8f);
        sr.sortingOrder = 1;
        if (materialSprite != null) sr.sharedMaterial = materialSprite;

        meta.transform.localScale = new Vector3(0.6f, 12f, 1f);
        meta.transform.position = new Vector3(posicionX, yPiso + 6f, 0f);

        BoxCollider2D caja = meta.AddComponent<BoxCollider2D>();
        caja.size = Vector2.one;
        caja.isTrigger = true;

        meta.AddComponent<MetaLlegada>();
        return meta.transform;
    }

    // ======================================================================
    //  INTERFAZ DEL NIVEL (HUD + Victoria + Game Over)
    // ======================================================================

    private static void CrearInterfaz(JugadorPichon jugador, Transform meta, int nivel, float distancia)
    {
        GameObject gestorGO = new GameObject("GestorNivel");
        GestorNivel gestor = gestorGO.AddComponent<GestorNivel>();

        Transform canvas = CrearCanvas("Canvas_Pichoncito");
        AsegurarEventSystem();

        // ---- HUD ----
        Vector2 arribaIzq = new Vector2(0f, 1f);
        Vector2 arribaDer = new Vector2(1f, 1f);
        Vector2 arribaCentro = new Vector2(0.5f, 1f);

        TextMeshProUGUI tPuntos = CrearTexto(canvas, "TextoPuntos", "Puntos: 0", 40, TextAlignmentOptions.Left,
            arribaIzq, arribaIzq, new Vector2(30f, -20f), new Vector2(600f, 60f));
        TextMeshProUGUI tBotellas = CrearTexto(canvas, "TextoBotellas", "Botellas: 0/0", 40, TextAlignmentOptions.Left,
            arribaIzq, arribaIzq, new Vector2(30f, -80f), new Vector2(600f, 60f));
        TextMeshProUGUI tReciclaje = CrearTexto(canvas, "TextoReciclaje", "Reciclaje: 0%", 40, TextAlignmentOptions.Left,
            arribaIzq, arribaIzq, new Vector2(30f, -140f), new Vector2(600f, 60f));
        TextMeshProUGUI tVidas = CrearTexto(canvas, "TextoVidas", "Vidas: 3", 40, TextAlignmentOptions.Right,
            arribaDer, arribaDer, new Vector2(-30f, -20f), new Vector2(500f, 60f));
        TextMeshProUGUI tDistancia = CrearTexto(canvas, "TextoDistancia", "Meta: 0 m", 44, TextAlignmentOptions.Center,
            arribaCentro, arribaCentro, new Vector2(0f, -20f), new Vector2(600f, 70f));

        Vector2 centro = new Vector2(0.5f, 0.5f);

        // ---- Panel de Victoria ----
        GameObject panelVictoria = CrearPanel(canvas, "PanelVictoria", new Color(0f, 0f, 0f, 0.8f));
        TextMeshProUGUI tResumen = CrearTexto(panelVictoria.transform, "TextoResumen", "¡Nivel completado!", 50,
            TextAlignmentOptions.Center, centro, centro, new Vector2(0f, 300f), new Vector2(1500f, 160f));
        TextMeshProUGUI tDato = CrearTexto(panelVictoria.transform, "TextoDatoEducativo", "", 38,
            TextAlignmentOptions.Center, centro, centro, new Vector2(0f, 60f), new Vector2(1400f, 320f));
        Button bSiguiente = CrearBoton(panelVictoria.transform, "BotonSiguienteNivel", "Siguiente Nivel",
            new Vector2(-400f, -280f), new Color(0.2f, 0.65f, 0.3f));
        Button bReintentarV = CrearBoton(panelVictoria.transform, "BotonReintentar", "Reintentar",
            new Vector2(0f, -280f), new Color(0.85f, 0.6f, 0.1f));
        Button bMenuV = CrearBoton(panelVictoria.transform, "BotonMenu", "Menú Principal",
            new Vector2(400f, -280f), new Color(0.35f, 0.45f, 0.75f));

        // ---- Panel de Game Over ----
        GameObject panelGameOver = CrearPanel(canvas, "PanelGameOver", new Color(0.25f, 0f, 0f, 0.85f));
        CrearTexto(panelGameOver.transform, "TextoTitulo", "GAME OVER", 80, TextAlignmentOptions.Center,
            centro, centro, new Vector2(0f, 300f), new Vector2(1500f, 130f));
        TextMeshProUGUI tMotivo = CrearTexto(panelGameOver.transform, "TextoMotivo", "", 44, TextAlignmentOptions.Center,
            centro, centro, new Vector2(0f, 90f), new Vector2(1400f, 240f));
        Button bReintentarG = CrearBoton(panelGameOver.transform, "BotonReintentar", "Reintentar",
            new Vector2(-220f, -150f), new Color(0.85f, 0.6f, 0.1f));
        Button bMenuG = CrearBoton(panelGameOver.transform, "BotonMenu", "Menú Principal",
            new Vector2(220f, -150f), new Color(0.35f, 0.45f, 0.75f));

        // ---- Conexión de botones ----
        UnityEventTools.AddPersistentListener(bSiguiente.onClick, gestor.SiguienteNivel);
        UnityEventTools.AddPersistentListener(bReintentarV.onClick, gestor.Reintentar);
        UnityEventTools.AddPersistentListener(bMenuV.onClick, gestor.MenuPrincipal);
        UnityEventTools.AddPersistentListener(bReintentarG.onClick, gestor.Reintentar);
        UnityEventTools.AddPersistentListener(bMenuG.onClick, gestor.MenuPrincipal);

        panelVictoria.SetActive(false);
        panelGameOver.SetActive(false);

        // ---- Referencias del GestorNivel ----
        SerializedObject so = new SerializedObject(gestor);
        so.FindProperty("numeroNivel").intValue = nivel;
        so.FindProperty("distanciaNivel").floatValue = distancia;
        so.FindProperty("jugador").objectReferenceValue = jugador;
        so.FindProperty("metaLlegada").objectReferenceValue = meta;
        so.FindProperty("textoPuntos").objectReferenceValue = tPuntos;
        so.FindProperty("textoBotellas").objectReferenceValue = tBotellas;
        so.FindProperty("textoReciclaje").objectReferenceValue = tReciclaje;
        so.FindProperty("textoVidas").objectReferenceValue = tVidas;
        so.FindProperty("textoDistancia").objectReferenceValue = tDistancia;
        so.FindProperty("panelVictoria").objectReferenceValue = panelVictoria;
        so.FindProperty("textoResumenVictoria").objectReferenceValue = tResumen;
        so.FindProperty("textoDatoEducativo").objectReferenceValue = tDato;
        so.FindProperty("botonSiguienteNivel").objectReferenceValue = bSiguiente.gameObject;
        so.FindProperty("panelGameOver").objectReferenceValue = panelGameOver;
        so.FindProperty("textoMotivoGameOver").objectReferenceValue = tMotivo;
        so.ApplyModifiedProperties();
    }

    // ======================================================================
    //  ESCENA DE MENÚ
    // ======================================================================

    private static void CrearEscenaMenu()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camaraGO = new GameObject("Main Camera");
        camaraGO.tag = "MainCamera";
        Camera camara = camaraGO.AddComponent<Camera>();
        camara.orthographic = true;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = new Color(0.1f, 0.35f, 0.2f);
        camaraGO.AddComponent<AudioListener>();
        camaraGO.transform.position = new Vector3(0f, 0f, -10f);

        Transform canvas = CrearCanvas("Canvas_Menu");
        AsegurarEventSystem();

        Vector2 centro = new Vector2(0.5f, 0.5f);

        GameObject fondo = CrearPanel(canvas, "Fondo", new Color(0.1f, 0.35f, 0.2f, 1f));

        CrearTexto(canvas, "Titulo", "Pichoncito", 120, TextAlignmentOptions.Center,
            centro, centro, new Vector2(0f, 330f), new Vector2(1600f, 160f));
        CrearTexto(canvas, "Subtitulo", "Guardián Ecológico de Chilca", 64, TextAlignmentOptions.Center,
            centro, centro, new Vector2(0f, 210f), new Vector2(1600f, 100f));

        Button bJugar = CrearBoton(canvas, "BotonJugar", "Jugar", new Vector2(0f, 20f), new Color(0.2f, 0.65f, 0.3f));
        Button bComoJugar = CrearBoton(canvas, "BotonComoJugar", "Cómo Jugar", new Vector2(0f, -100f), new Color(0.85f, 0.6f, 0.1f));
        Button bSalir = CrearBoton(canvas, "BotonSalir", "Salir", new Vector2(0f, -220f), new Color(0.75f, 0.3f, 0.3f));

        // Pop-up de controles.
        GameObject panelComoJugar = CrearPanel(canvas, "PanelComoJugar", new Color(0f, 0f, 0f, 0.92f));
        CrearTexto(panelComoJugar.transform, "TituloControles", "Cómo Jugar", 80, TextAlignmentOptions.Center,
            centro, centro, new Vector2(0f, 330f), new Vector2(1500f, 120f));
        CrearTexto(panelComoJugar.transform, "TextoControles",
            "Pichoncito avanza solo hacia la derecha.\n\n" +
            "ESPACIO / W / Flecha Arriba: saltar (mantén para saltar más alto).\n" +
            "Mantén la tecla en el aire: planear con la capa.\n\n" +
            "Recolecta botellas (necesitas al menos 70 %) y evita ratas y smog.\n" +
            "Cruza la meta para completar el nivel.",
            40, TextAlignmentOptions.Center, centro, centro, new Vector2(0f, 60f), new Vector2(1500f, 520f));
        Button bCerrar = CrearBoton(panelComoJugar.transform, "BotonCerrar", "Cerrar",
            new Vector2(0f, -330f), new Color(0.35f, 0.45f, 0.75f));
        panelComoJugar.SetActive(false);

        GameObject menuGO = new GameObject("MenuInicio");
        MenuInicio menu = menuGO.AddComponent<MenuInicio>();

        SerializedObject so = new SerializedObject(menu);
        so.FindProperty("nombreEscenaNivel1").stringValue = "Nivel1";
        so.FindProperty("panelComoJugar").objectReferenceValue = panelComoJugar;
        so.ApplyModifiedProperties();

        UnityEventTools.AddPersistentListener(bJugar.onClick, menu.Jugar);
        UnityEventTools.AddPersistentListener(bComoJugar.onClick, menu.AbrirComoJugar);
        UnityEventTools.AddPersistentListener(bSalir.onClick, menu.Salir);
        UnityEventTools.AddPersistentListener(bCerrar.onClick, menu.CerrarComoJugar);

        // El fondo no debe bloquear clics.
        Image imagenFondo = fondo.GetComponent<Image>();
        if (imagenFondo != null) imagenFondo.raycastTarget = false;

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), EscenaMenu);
    }

    // ======================================================================
    //  HELPERS DE UI
    // ======================================================================

    private static Transform CrearCanvas(string nombre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler escalador = go.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go.transform;
    }

    private static void AsegurarEventSystem()
    {
        if (Object.FindObjectsByType<EventSystem>().Length > 0) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject CrearObjetoUI(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(padre, false);
        return go;
    }

    private static GameObject CrearPanel(Transform padre, string nombre, Color color)
    {
        GameObject go = CrearObjetoUI(nombre, padre);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image imagen = go.AddComponent<Image>();
        imagen.color = color;
        return go;
    }

    private static TextMeshProUGUI CrearTexto(Transform padre, string nombre, string contenido, float tamano,
                                              TextAlignmentOptions alineacion, Vector2 ancla, Vector2 pivote,
                                              Vector2 posicion, Vector2 dimension)
    {
        GameObject go = CrearObjetoUI(nombre, padre);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = dimension;

        TextMeshProUGUI texto = go.AddComponent<TextMeshProUGUI>();
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.alignment = alineacion;
        texto.color = Color.white;
        texto.raycastTarget = false;
        return texto;
    }

    private static Button CrearBoton(Transform padre, string nombre, string etiqueta, Vector2 posicion, Color color)
    {
        Vector2 centro = new Vector2(0.5f, 0.5f);
        Vector2 dimension = new Vector2(380f, 100f);

        GameObject go = CrearObjetoUI(nombre, padre);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = centro;
        rt.anchorMax = centro;
        rt.pivot = centro;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = dimension;

        Image imagen = go.AddComponent<Image>();
        imagen.color = color;

        Button boton = go.AddComponent<Button>();
        boton.targetGraphic = imagen;

        CrearTexto(go.transform, "Texto", etiqueta, 38, TextAlignmentOptions.Center,
            centro, centro, Vector2.zero, dimension);
        return boton;
    }
}
#endif
