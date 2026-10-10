using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Pone la imagen del mercado como fondo del nivel.
/// El fondo se crea como hijo de la Main Camera, así siempre llena la pantalla
/// mientras el pichón avanza (no se acaba nunca).
///
/// Requisito: la imagen debe estar en Assets/Sprites/Fondos/fondo_mercado_pixel.png
/// Uso: abre la escena (ej. Nivel1) y elige el menú Pichoncito > Poner fondo del mercado
/// Se puede repetir sin problema: reemplaza el fondo anterior.
/// </summary>
public static class FondoMercadoCreador
{
    private const string RutaImagen = "Assets/Sprites/Fondos/fondo_mercado_pixel.png";
    private const string NombreObjeto = "Fondo_Mercado";
    private const float AspectoMinimoCubierto = 2.2f;   // cubre pantallas anchas (hasta ~21:9)

    [MenuItem("Pichoncito/Poner fondo del mercado")]
    public static void PonerFondo()
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(RutaImagen) == null)
        {
            Debug.LogError("No encuentro " + RutaImagen + ". Copia la carpeta Fondos dentro de Assets/Sprites.");
            return;
        }

        ConfigurarImportacion();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RutaImagen);
        if (sprite == null)
        {
            Debug.LogError("La imagen no se importó como Sprite: " + RutaImagen);
            return;
        }

        Camera camara = Camera.main;
        if (camara == null)
        {
            Debug.LogError("No encuentro la Main Camera (debe tener el tag MainCamera).");
            return;
        }

        // Quita fondos anteriores para no duplicar
        foreach (Transform hijo in camara.GetComponentsInChildren<Transform>(true))
        {
            if (hijo != camara.transform && hijo.name == NombreObjeto)
            {
                Undo.DestroyObjectImmediate(hijo.gameObject);
                break;
            }
        }

        GameObject fondo = new GameObject(NombreObjeto);
        Undo.RegisterCreatedObjectUndo(fondo, "Crear fondo del mercado");
        fondo.transform.SetParent(camara.transform, false);
        fondo.transform.localPosition = new Vector3(0f, 0f, 10f);
        fondo.transform.localRotation = Quaternion.identity;

        SpriteRenderer renderizador = fondo.AddComponent<SpriteRenderer>();
        renderizador.sprite = sprite;
        renderizador.sortingOrder = -100;                    // siempre detrás de todo
        renderizador.color = Color.white;

        float altoCamara = camara.orthographicSize * 2f;
        float anchoCamara = altoCamara * Mathf.Max(camara.aspect, AspectoMinimoCubierto);
        Vector2 tamano = sprite.bounds.size;
        float escala = Mathf.Max(anchoCamara / tamano.x, altoCamara / tamano.y) * 1.05f;
        fondo.transform.localScale = new Vector3(escala, escala, 1f);

        EditorSceneManager.MarkSceneDirty(fondo.scene);
        Selection.activeGameObject = fondo;
        Debug.Log("Listo: fondo del mercado puesto en " + fondo.scene.name + ". Guarda la escena con Ctrl+S.");
    }

    private static void ConfigurarImportacion()
    {
        TextureImporter importador = AssetImporter.GetAtPath(RutaImagen) as TextureImporter;
        if (importador == null)
        {
            return;
        }

        bool cambiado = false;

        if (importador.textureType != TextureImporterType.Sprite)
        {
            importador.textureType = TextureImporterType.Sprite;
            cambiado = true;
        }
        if (importador.spriteImportMode != SpriteImportMode.Single)
        {
            importador.spriteImportMode = SpriteImportMode.Single;
            cambiado = true;
        }
        if (!Mathf.Approximately(importador.spritePixelsPerUnit, 100f))
        {
            importador.spritePixelsPerUnit = 100f;
            cambiado = true;
        }
        if (importador.filterMode != FilterMode.Point)
        {
            importador.filterMode = FilterMode.Point;   // se mantiene el look pixel-art
            cambiado = true;
        }
        if (importador.mipmapEnabled)
        {
            importador.mipmapEnabled = false;
            cambiado = true;
        }
        if (importador.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            cambiado = true;
        }
        if (importador.maxTextureSize < 2048)
        {
            importador.maxTextureSize = 2048;
            cambiado = true;
        }

        if (cambiado)
        {
            importador.SaveAndReimport();
        }
    }
}
