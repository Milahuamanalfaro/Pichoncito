using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hace que los textos del HUD (Puntos, Botellas, Reciclaje, Meta, Vidas, Game Over...)
/// se lean mejor sobre el fondo: negrita + contorno oscuro grueso.
/// El texto de vidas se pone en rojo. No cambia el tamaño ni la posición,
/// y no toca los textos de los botones.
///
/// Funciona con TextMeshPro (crea un material con contorno en Assets/Materials)
/// y con el componente Text normal de Unity (le agrega un Outline).
///
/// Uso: abre el nivel (fuera de Play) y elige Pichoncito > Mejorar texto del HUD.
/// Se puede repetir sin problema; repítelo en Nivel2 y Nivel3.
/// </summary>
public static class EstiloTextoHUD
{
    private const string CarpetaMateriales = "Assets/Materials";
    private const float GrosorContorno = 0.28f;
    private static readonly Color ColorContorno = new Color(0.07f, 0.08f, 0.12f, 1f);
    private static readonly Color ColorVidas = new Color(1f, 0.22f, 0.22f, 1f);   // rojo para el texto de vidas

    [MenuItem("Pichoncito/Mejorar texto del HUD")]
    public static void Mejorar()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Sal del modo Play antes de usar este menú (los cambios se perderían).");
            return;
        }

        int cantidadTmp = 0;
        int cantidadTexto = 0;
        Dictionary<TMP_FontAsset, Material> materiales = new Dictionary<TMP_FontAsset, Material>();

        TMP_Text[] textosTmp = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text texto in textosTmp)
        {
            if (!EsDeEscena(texto.gameObject) || EsDeBoton(texto.gameObject))
            {
                continue;
            }

            Undo.RecordObject(texto, "Mejorar texto del HUD");
            texto.fontStyle |= FontStyles.Bold;

            if (EsTextoDeVidas(texto.gameObject.name, texto.text))
            {
                texto.color = ColorVidas;
            }

            Material material = ObtenerMaterial(texto, materiales);
            if (material != null)
            {
                texto.fontSharedMaterial = material;
            }

            EditorUtility.SetDirty(texto);
            EditorSceneManager.MarkSceneDirty(texto.gameObject.scene);
            cantidadTmp++;
        }

        Text[] textosNormales = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Text texto in textosNormales)
        {
            if (!EsDeEscena(texto.gameObject) || EsDeBoton(texto.gameObject))
            {
                continue;
            }

            Undo.RecordObject(texto, "Mejorar texto del HUD");
            texto.fontStyle = FontStyle.Bold;

            if (EsTextoDeVidas(texto.gameObject.name, texto.text))
            {
                texto.color = ColorVidas;
            }

            Outline contorno = texto.GetComponent<Outline>();
            if (contorno == null)
            {
                contorno = Undo.AddComponent<Outline>(texto.gameObject);
            }
            contorno.effectColor = ColorContorno;
            contorno.effectDistance = new Vector2(2f, -2f);

            EditorUtility.SetDirty(texto);
            EditorSceneManager.MarkSceneDirty(texto.gameObject.scene);
            cantidadTexto++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Listo: texto mejorado (TextMeshPro: " + cantidadTmp + ", Text normal: " + cantidadTexto + "). Guarda la escena con Ctrl+S.");
    }

    private static bool EsTextoDeVidas(string nombreObjeto, string contenido)
    {
        string nombre = nombreObjeto == null ? "" : nombreObjeto.ToLowerInvariant();
        string texto = contenido == null ? "" : contenido.ToLowerInvariant();
        return nombre.Contains("vida") || texto.Contains("vida");
    }

    private static bool EsDeEscena(GameObject objeto)
    {
        return objeto.scene.IsValid() && !EditorUtility.IsPersistent(objeto);
    }

    private static bool EsDeBoton(GameObject objeto)
    {
        return objeto.GetComponentInParent<Button>(true) != null;
    }

    private static Material ObtenerMaterial(TMP_Text texto, Dictionary<TMP_FontAsset, Material> cache)
    {
        TMP_FontAsset fuente = texto.font;
        if (fuente == null || fuente.material == null)
        {
            return null;
        }

        if (cache.TryGetValue(fuente, out Material guardado))
        {
            return guardado;
        }

        if (!AssetDatabase.IsValidFolder(CarpetaMateriales))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        string ruta = CarpetaMateriales + "/" + fuente.name + " HUD Contorno.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);

        if (material == null)
        {
            material = new Material(fuente.material);
            AssetDatabase.CreateAsset(material, ruta);
        }

        material.EnableKeyword("OUTLINE_ON");
        material.SetColor("_OutlineColor", ColorContorno);
        material.SetFloat("_OutlineWidth", GrosorContorno);
        EditorUtility.SetDirty(material);

        cache[fuente] = material;
        return material;
    }
}
