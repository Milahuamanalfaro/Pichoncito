using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Crea las animaciones Idle, Running y Gliding del Pichoncito héroe a partir de
/// los sprites de Assets/Sprites/PichonHeroe/ y arma (o reconstruye) el Animator
/// Controller "Pichon" con los parámetros bool "EnSuelo" y "Detenido".
///
/// Lógica del Animator:
///   Running  -> estado por defecto (el pichón avanza solo)
///   Gliding  -> cuando EnSuelo = false (salto o planeo)
///   Idle     -> cuando Detenido = true (JugadorPichon.Detener())
///
/// Uso: menú Pichoncito > Crear animaciones del Pichón
/// </summary>
public static class PichonAnimacionesCreador
{
    private const string CarpetaSprites = "Assets/Sprites/PichonHeroe";
    private const string CarpetaAnimaciones = "Assets/Animations";
    private const string SubcarpetaPichon = "Assets/Animations/Pichon";

    [MenuItem("Pichoncito/Crear animaciones del Pichón")]
    public static void CrearAnimaciones()
    {
        if (!AssetDatabase.IsValidFolder(CarpetaSprites))
        {
            Debug.LogError("No existe la carpeta " + CarpetaSprites + ". Importa primero los sprites (Paso 1).");
            return;
        }

        AsegurarCarpeta("Assets", "Animations");
        AsegurarCarpeta(CarpetaAnimaciones, "Pichon");

        AnimationClip clipIdle = CrearClip("idle", "Pichon_Idle", 8f);
        AnimationClip clipRunning = CrearClip("running", "Pichon_Running", 12f);
        AnimationClip clipGliding = CrearClip("gliding", "Pichon_Gliding", 10f);

        if (clipIdle == null || clipRunning == null || clipGliding == null)
        {
            Debug.LogError("No se pudieron crear todas las animaciones. Revisa los mensajes anteriores.");
            return;
        }

        // Se reutiliza el controller si ya existe para que Pichon2D no pierda la referencia.
        string rutaControlador = SubcarpetaPichon + "/Pichon.controller";
        AnimatorController controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaControlador);

        if (controlador == null)
        {
            controlador = AnimatorController.CreateAnimatorControllerAtPath(rutaControlador);
        }

        if (controlador == null)
        {
            Debug.LogError("No se pudo crear el Animator Controller en " + rutaControlador);
            return;
        }

        LimpiarControlador(controlador);

        AnimatorControllerParameter parametroEnSuelo = new AnimatorControllerParameter
        {
            name = "EnSuelo",
            type = AnimatorControllerParameterType.Bool,
            defaultBool = true
        };
        controlador.AddParameter(parametroEnSuelo);
        controlador.AddParameter("Detenido", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine maquina = controlador.layers[0].stateMachine;

        AnimatorState estadoRunning = maquina.AddState("Running");
        estadoRunning.motion = clipRunning;

        AnimatorState estadoGliding = maquina.AddState("Gliding");
        estadoGliding.motion = clipGliding;

        AnimatorState estadoIdle = maquina.AddState("Idle");
        estadoIdle.motion = clipIdle;

        maquina.defaultState = estadoRunning;

        // Running <-> Gliding según toque el suelo o no
        CrearTransicion(estadoRunning, estadoGliding, "EnSuelo", false, "Detenido", false);
        CrearTransicion(estadoGliding, estadoRunning, "EnSuelo", true, "Detenido", false);

        // Desde cualquier estado -> Idle cuando el jugador se detiene
        AnimatorStateTransition haciaIdle = maquina.AddAnyStateTransition(estadoIdle);
        haciaIdle.hasExitTime = false;
        haciaIdle.hasFixedDuration = true;
        haciaIdle.duration = 0f;
        haciaIdle.canTransitionToSelf = false;
        haciaIdle.AddCondition(AnimatorConditionMode.If, 0f, "Detenido");

        // Idle -> Running / Gliding si se reanuda
        CrearTransicion(estadoIdle, estadoRunning, "Detenido", false, "EnSuelo", true);
        CrearTransicion(estadoIdle, estadoGliding, "Detenido", false, "EnSuelo", false);

        EditorUtility.SetDirty(controlador);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = controlador;
        EditorGUIUtility.PingObject(controlador);
        Debug.Log("Listo: animaciones y Animator creados en " + SubcarpetaPichon);
    }

    private static void AsegurarCarpeta(string padre, string nombre)
    {
        string ruta = padre + "/" + nombre;
        if (!AssetDatabase.IsValidFolder(ruta))
        {
            AssetDatabase.CreateFolder(padre, nombre);
        }
    }

    private static void LimpiarControlador(AnimatorController controlador)
    {
        controlador.parameters = new AnimatorControllerParameter[0];

        AnimatorStateMachine maquina = controlador.layers[0].stateMachine;

        foreach (AnimatorStateTransition transicion in maquina.anyStateTransitions)
        {
            maquina.RemoveAnyStateTransition(transicion);
        }

        foreach (ChildAnimatorState hijo in maquina.states)
        {
            maquina.RemoveState(hijo.state);
        }
    }

    private static AnimationClip CrearClip(string prefijo, string nombreClip, float fps)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite " + prefijo + "_", new[] { CarpetaSprites });
        List<string> rutas = new List<string>();

        foreach (string guid in guids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            string nombre = Path.GetFileNameWithoutExtension(ruta);
            if (nombre.StartsWith(prefijo + "_") && !rutas.Contains(ruta))
            {
                rutas.Add(ruta);
            }
        }

        rutas.Sort(System.StringComparer.Ordinal);

        if (rutas.Count == 0)
        {
            Debug.LogError("No se encontraron sprites '" + prefijo + "_##' en " + CarpetaSprites);
            return null;
        }

        List<Sprite> sprites = new List<Sprite>();
        foreach (string ruta in rutas)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
            if (sprite == null)
            {
                Debug.LogError("El archivo " + ruta + " no está importado como Sprite. Revisa el Paso 1.");
                return null;
            }
            sprites.Add(sprite);
        }

        // Un cuadro extra al final para que el último sprite dure lo mismo que los demás
        ObjectReferenceKeyframe[] cuadros = new ObjectReferenceKeyframe[sprites.Count + 1];
        for (int i = 0; i < sprites.Count; i++)
        {
            cuadros[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        }
        cuadros[sprites.Count] = new ObjectReferenceKeyframe
        {
            time = sprites.Count / fps,
            value = sprites[sprites.Count - 1]
        };

        AnimationClip clip = new AnimationClip
        {
            frameRate = fps,
            wrapMode = WrapMode.Loop
        };

        EditorCurveBinding enlace = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, enlace, cuadros);

        AnimationClipSettings ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);

        string rutaClip = SubcarpetaPichon + "/" + nombreClip + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(rutaClip) != null)
        {
            AssetDatabase.DeleteAsset(rutaClip);
        }
        AssetDatabase.CreateAsset(clip, rutaClip);

        return clip;
    }

    private static void CrearTransicion(
        AnimatorState origen,
        AnimatorState destino,
        string parametro1,
        bool valor1,
        string parametro2,
        bool valor2)
    {
        AnimatorStateTransition transicion = origen.AddTransition(destino);
        transicion.hasExitTime = false;
        transicion.hasFixedDuration = true;
        transicion.duration = 0f;

        transicion.AddCondition(valor1 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parametro1);

        if (!string.IsNullOrEmpty(parametro2))
        {
            transicion.AddCondition(valor2 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parametro2);
        }
    }
}
