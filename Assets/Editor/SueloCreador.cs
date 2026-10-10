using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Pone el arte pixel-art al piso del nivel SIN tocar la física.
///
/// No modifica "Suelo" ni "Suelo_Relleno" (ni su escala, ni su posición, ni sus colliders).
/// Solo apaga su Sprite Renderer (el cuadrado) y crea dos objetos nuevos encima, del mismo
/// tamaño y en el mismo lugar:
///   Arte_Suelo    -> calle de tierra con borde superior (Sliced: el borde no se estira)
///   Arte_Relleno  -> tierra con piedras y basura enterrada (Tiled: se repite)
///
/// Requisito: Assets/Sprites/Suelo/suelo_calle.png y suelo_relleno.png
/// Uso: abre el nivel (fuera de Play) y elige Pichoncito > Poner suelo y relleno.
/// Se puede repetir sin problema; también sirve para Nivel2 y Nivel3.
/// Para volver atrás: borra Arte_Suelo y Arte_Relleno y vuelve a activar el Sprite Renderer.
/// </summary>
public static class SueloCreador
{
    private const string RutaSuelo = "Assets/Sprites/Suelo/suelo_calle.png";
    private const string RutaRelleno = "Assets/Sprites/Suelo/suelo_relleno.png";
    private const float BordeSuperiorPx = 32f;   // 8 pixeles de arte x 4

    [MenuItem("Pichoncito/Poner suelo y relleno")]
    public static void PonerSuelo()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Sal del modo Play antes de usar este menú (los cambios se perderían).");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(RutaSuelo) == null ||
            AssetDatabase.LoadAssetAtPath<Texture2D>(RutaRelleno) == null)
        {
            Debug.LogError("Faltan las imágenes. Deben existir " + RutaSuelo + " y " + RutaRelleno + ".");
            return;
        }

        ConfigurarImportacion(RutaSuelo, new Vector4(0f, 0f, 0f, BordeSuperiorPx));
        ConfigurarImportacion(RutaRelleno, Vector4.zero);

        Sprite spriteSuelo = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSuelo);
        Sprite spriteRelleno = AssetDatabase.LoadAssetAtPath<Sprite>(RutaRelleno);

        if (spriteSuelo == null || spriteRelleno == null)
        {
            Debug.LogError("Las imágenes no se importaron como Sprite. Espera a que Unity termine e inténtalo otra vez.");
            return;
        }

        bool a = CrearArte("Suelo", "Arte_Suelo", spriteSuelo, SpriteDrawMode.Sliced, -10);
        bool b = CrearArte("Suelo_Relleno", "Arte_Relleno", spriteRelleno, SpriteDrawMode.Tiled, -11);

        if (a || b)
        {
            Debug.Log("Listo: Arte_Suelo y Arte_Relleno creados. La física no se tocó. Guarda con Ctrl+S.");
        }
    }

    private static bool CrearArte(string nombreOriginal, string nombreArte, Sprite sprite, SpriteDrawMode modo, int orden)
    {
        GameObject original = GameObject.Find(nombreOriginal);
        if (original == null)
        {
            Debug.LogWarning("No encontré el objeto '" + nombreOriginal + "' en la escena abierta.");
            return false;
        }

        SpriteRenderer renderizadorOriginal = original.GetComponent<SpriteRenderer>();
        if (renderizadorOriginal == null || renderizadorOriginal.sprite == null)
        {
            Debug.LogWarning("'" + nombreOriginal + "' no tiene un Sprite Renderer con sprite.");
            return false;
        }

        // Tamaño y centro actuales en el mundo, calculados sin modificar nada
        Bounds local = renderizadorOriginal.sprite.bounds;
        Vector3 centro = original.transform.TransformPoint(local.center);
        Vector3 escala = original.transform.lossyScale;
        Vector2 tamano = new Vector2(
            Mathf.Abs(local.size.x * escala.x),
            Mathf.Abs(local.size.y * escala.y));

        // Quita el arte anterior si se repite el menú
        GameObject anterior = GameObject.Find(nombreArte);
        if (anterior != null)
        {
            Undo.DestroyObjectImmediate(anterior);
        }

        GameObject arte = new GameObject(nombreArte);
        Undo.RegisterCreatedObjectUndo(arte, "Crear " + nombreArte);
        arte.transform.position = new Vector3(centro.x, centro.y, original.transform.position.z);

        SpriteRenderer renderizador = arte.AddComponent<SpriteRenderer>();
        renderizador.sprite = sprite;
        renderizador.drawMode = modo;
        renderizador.tileMode = SpriteTileMode.Continuous;
        renderizador.size = tamano;
        renderizador.color = Color.white;
        renderizador.sortingOrder = orden;

        Undo.RecordObject(renderizadorOriginal, "Apagar dibujo viejo");
        renderizadorOriginal.enabled = false;

        EditorSceneManager.MarkSceneDirty(original.scene);
        return true;
    }

    private static void ConfigurarImportacion(string ruta, Vector4 borde)
    {
        TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador == null)
        {
            return;
        }

        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spritePixelsPerUnit = 100f;
        importador.filterMode = FilterMode.Point;
        importador.wrapMode = TextureWrapMode.Repeat;
        importador.mipmapEnabled = false;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.spriteBorder = borde;

        TextureImporterSettings ajustes = new TextureImporterSettings();
        importador.ReadTextureSettings(ajustes);
        ajustes.spriteMeshType = SpriteMeshType.FullRect;
        ajustes.spriteAlignment = (int)SpriteAlignment.Center;
        importador.SetTextureSettings(ajustes);

        importador.SaveAndReimport();
    }
}
