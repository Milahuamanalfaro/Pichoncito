using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Pone un arco de meta pixel-art en el nivel: cartel verde "META" con cuadros de llegada,
/// botellas, banderines y contenedores de reciclaje.
///
/// NO modifica "Meta_Llegada" (ni su posición, escala o collider/trigger): solo apaga su dibujo
/// si tiene Sprite Renderer, y crea un objeto nuevo "Arte_Meta" centrado en ella y apoyado en el piso.
/// El arco queda detrás del pichón, así que pasa "por dentro" al llegar.
///
/// Requisito: Assets/Sprites/Meta/meta_arco.png
/// Uso: abre el nivel (fuera de Play) y elige Pichoncito > Poner arco de meta.
/// Se puede repetir; también sirve para Nivel2 y Nivel3.
/// Para volver atrás: borra Arte_Meta y vuelve a activar el Sprite Renderer de Meta_Llegada.
/// </summary>
public static class MetaCreador
{
    private const string RutaImagen = "Assets/Sprites/Meta/meta_arco.png";
    private const string NombreMeta = "Meta_Llegada";
    private const string NombreSuelo = "Suelo";
    private const string NombreArte = "Arte_Meta";
    private const int OrdenDibujo = -5;   // delante del fondo y del piso, detrás del pichón

    [MenuItem("Pichoncito/Poner arco de meta")]
    public static void PonerMeta()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Sal del modo Play antes de usar este menú (los cambios se perderían).");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(RutaImagen) == null)
        {
            Debug.LogError("No encuentro " + RutaImagen + ". Copia la carpeta Meta dentro de Assets/Sprites.");
            return;
        }

        ConfigurarImportacion();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RutaImagen);
        if (sprite == null)
        {
            Debug.LogError("La imagen no se importó como Sprite. Espera a que Unity termine e inténtalo otra vez.");
            return;
        }

        GameObject meta = GameObject.Find(NombreMeta);
        if (meta == null)
        {
            Debug.LogError("No encontré el objeto '" + NombreMeta + "' en la escena abierta.");
            return;
        }

        Bounds limitesMeta = ObtenerLimites(meta);
        float x = limitesMeta.center.x;

        // Altura del piso: parte de arriba del Suelo (si no existe, la base de la meta)
        float y = limitesMeta.min.y;
        GameObject suelo = GameObject.Find(NombreSuelo);
        if (suelo != null)
        {
            y = ObtenerLimites(suelo).max.y;
        }

        GameObject anterior = GameObject.Find(NombreArte);
        if (anterior != null)
        {
            Undo.DestroyObjectImmediate(anterior);
        }

        GameObject arte = new GameObject(NombreArte);
        Undo.RegisterCreatedObjectUndo(arte, "Crear arco de meta");
        arte.transform.position = new Vector3(x, y, meta.transform.position.z);

        SpriteRenderer renderizador = arte.AddComponent<SpriteRenderer>();
        renderizador.sprite = sprite;
        renderizador.sortingOrder = OrdenDibujo;

        SpriteRenderer dibujoViejo = meta.GetComponent<SpriteRenderer>();
        if (dibujoViejo != null)
        {
            Undo.RecordObject(dibujoViejo, "Apagar dibujo viejo de la meta");
            dibujoViejo.enabled = false;
        }

        EditorSceneManager.MarkSceneDirty(meta.scene);
        Selection.activeGameObject = arte;
        Debug.Log("Listo: arco de meta puesto en x = " + x.ToString("0.0") + ". La meta (trigger) no se tocó. Guarda con Ctrl+S.");
    }

    // Tamaño y posición en el mundo sin modificar nada
    private static Bounds ObtenerLimites(GameObject objeto)
    {
        Collider2D colisionador = objeto.GetComponent<Collider2D>();
        if (colisionador != null)
        {
            return colisionador.bounds;
        }

        SpriteRenderer dibujo = objeto.GetComponent<SpriteRenderer>();
        if (dibujo != null && dibujo.sprite != null)
        {
            Bounds local = dibujo.sprite.bounds;
            Vector3 centro = objeto.transform.TransformPoint(local.center);
            Vector3 escala = objeto.transform.lossyScale;
            Vector3 tamano = new Vector3(
                Mathf.Abs(local.size.x * escala.x),
                Mathf.Abs(local.size.y * escala.y),
                0f);
            return new Bounds(centro, tamano);
        }

        return new Bounds(objeto.transform.position, Vector3.zero);
    }

    private static void ConfigurarImportacion()
    {
        TextureImporter importador = AssetImporter.GetAtPath(RutaImagen) as TextureImporter;
        if (importador == null)
        {
            return;
        }

        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spritePixelsPerUnit = 100f;
        importador.filterMode = FilterMode.Point;
        importador.mipmapEnabled = false;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.alphaIsTransparency = true;

        TextureImporterSettings ajustes = new TextureImporterSettings();
        importador.ReadTextureSettings(ajustes);
        ajustes.spriteAlignment = (int)SpriteAlignment.Custom;
        ajustes.spritePivot = new Vector2(0.5f, 0f);   // pivote abajo-centro
        importador.SetTextureSettings(ajustes);

        importador.SaveAndReimport();
    }
}
