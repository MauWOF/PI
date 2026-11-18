using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

// Prueba manual: solo usa la consola y restaura el record al salir de Play Mode.
[InitializeOnLoad]
public static class VerificarHelix
{
    static int fase, comprobaciones, destino, cantidadSectores;
    static double inicio;
    static bool reboto, huboErrores;
    static HashSet<BloqueTorre> bloquesOriginales;
    static VerificarHelix()
    {
        EditorApplication.update += Actualizar;
        Application.logMessageReceived += (mensaje, pila, tipo) => {
            if (SessionState.GetBool("HelixPruebaActiva", false) && (tipo == LogType.Error || tipo == LogType.Exception)) huboErrores = true;
        };
        EditorApplication.playModeStateChanged += estado => {
            if (estado != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("HelixPruebaActiva", false)) return;
            if (SessionState.GetBool("HelixRecordExistia", false)) PlayerPrefs.SetInt(DatosPartida.ClaveRecord, SessionState.GetInt("HelixRecordAnterior", 0));
            else PlayerPrefs.DeleteKey(DatosPartida.ClaveRecord);
            PlayerPrefs.Save();
            SessionState.SetBool("HelixPruebaActiva", false);
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
        };
    }
    [MenuItem("Helix/Verificar funcionamiento")]
    public static void Preparar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("HelixRecordExistia", PlayerPrefs.HasKey(DatosPartida.ClaveRecord));
        SessionState.SetInt("HelixRecordAnterior", PlayerPrefs.GetInt(DatosPartida.ClaveRecord));
        SessionState.SetBool("HelixPruebaActiva", true);
        PlayerPrefs.SetInt(DatosPartida.ClaveRecord, 0);
        EditorSceneManager.OpenScene("Assets/Scenes/Juego.unity");
        EditorApplication.isPlaying = true;
    }
    static void Comprobar(bool bien, string mensaje)
    {
        if (!bien) throw new System.Exception("Verificacion Helix: " + mensaje);
        comprobaciones++; Debug.Log("Helix OK: " + mensaje);
    }
    static void Esperar(int siguiente) { fase = siguiente; inicio = EditorApplication.timeSinceStartup; }
    static bool Paso(double tiempo = 0.3) => EditorApplication.timeSinceStartup - inicio > tiempo;
    static void Colocar(Partida p, int pisos)
    {
        p.pelota.isKinematic = true;
        float profundidad = (pisos - 1) * p.generador.separacion + 0.8f;
        p.pelota.position = new Vector3(0.75f, (float)p.generador.DesplazamientoOrigen - profundidad, -2.3f);
    }
    static void RevisarBloques(GeneradorTorre g)
    {
        var ordenados = g.Bloques.OrderBy(b => b.PisoInicial).ToArray();
        Comprobar(ordenados.Length == g.bloquesActivos, "Cantidad fija de bloques");
        Comprobar(bloquesOriginales.SetEquals(ordenados), "Se reutilizan los mismos bloques");
        Comprobar(g.GetComponentsInChildren<Superficie>(true).Length == cantidadSectores, "No aumenta la cantidad de sectores");
        for (int i = 1; i < ordenados.Length; i++)
            Comprobar(ordenados[i].PisoInicial == ordenados[i - 1].PisoInicial + g.pisosPorBloque, "Bloques consecutivos sin tramos faltantes");
        foreach (var b in ordenados)
        {
            for (int piso = 0; piso < g.pisosPorBloque; piso++)
            {
                var sectores = b.GetComponentsInChildren<Superficie>(true).Where(s => Mathf.Abs(s.transform.localPosition.y + piso * g.separacion) < 0.01f).ToArray();
                var d = g.DificultadDelPiso(b.PisoInicial + piso);
                Comprobar(sectores.Count(s => s.gameObject.activeSelf) == g.sectoresPorPiso - d.sectoresHueco, "Hueco correspondiente al piso " + (b.PisoInicial + piso));
                Comprobar(sectores.Where(s => s.gameObject.activeSelf).Any(s => !s.peligrosa), "Siempre queda una superficie segura");
            }
        }
    }
    static void Actualizar()
    {
        if (!SessionState.GetBool("HelixPruebaActiva", false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try { Ejecutar(); }
        catch (System.Exception error) { Debug.LogException(error); EditorApplication.isPlaying = false; }
    }
    static void Ejecutar()
    {
        if (fase == 11)
        {
            var menu = Object.FindAnyObjectByType<MenuPrincipal>();
            if (menu == null || !Paso()) return;
            Comprobar(DatosPartida.Record >= 6000 && PlayerPrefs.GetInt(DatosPartida.ClaveRecord) >= 6000, "Record persistente al regresar al menu");
            Comprobar(!menu.instrucciones.transform.parent.GetComponentsInChildren<Text>(true).Any(t => t.text.StartsWith("PROYECTO FINAL")), "Sin texto inferior de fase");
            menu.Ayuda(); Comprobar(menu.instrucciones.activeSelf, "Instrucciones del modo infinito"); menu.Ayuda();
            Comprobar(!huboErrores, "Sin errores durante la prueba de ejecucion");
            Debug.Log("HELIX INFINITO VERIFICACION COMPLETA: " + comprobaciones + " comprobaciones correctas. Se restaura el record previo.");
            fase = 12; EditorApplication.isPlaying = false; return;
        }
        var p = Object.FindAnyObjectByType<Partida>();
        if (p == null || p.generador.Bloques.Count == 0) return;
        var g = p.generador;
        if (fase == 0)
        {
            bloquesOriginales = new HashSet<BloqueTorre>(g.Bloques);
            cantidadSectores = g.GetComponentsInChildren<Superficie>(true).Length;
            Comprobar(g.Malla.isReadable && g.Malla.vertices.Length > 0, "Modelo legible y adaptable a la cantidad de sectores");
            Comprobar(g.bloquesActivos == 4 && cantidadSectores == 288, "Cuatro bloques de seis pisos generados por codigo");
            Comprobar(!Object.FindObjectsByType<Superficie>().Any(s => s.meta), "No hay plataforma final");
            RevisarBloques(g); Esperar(1);
        }
        else if (fase == 1)
        {
            if (p.pelota.linearVelocity.y > 5) reboto = true;
            if (!Paso(3)) return;
            Comprobar(reboto, "Rebote fisico sobre un bloque generado");
            Comprobar(p.Puntos == 0 && p.progreso.fillRect.rect.width < 1, "Puntuacion y barra comienzan en cero");
            p.Pausar(); Comprobar(!p.Jugando && Time.timeScale == 0, "Pausa"); p.Pausar();
            Colocar(p, 1); Esperar(2);
        }
        else if (fase == 2 && Paso())
        {
            Comprobar(p.Puntos == 10, "Diez puntos por piso");
            p.pelota.position = new Vector3(0.75f, 1, -2.3f); Esperar(3);
        }
        else if (fase == 3 && Paso())
        {
            Comprobar(p.Puntos == 10, "El rebote no resta puntos"); Colocar(p, 1); Esperar(4);
        }
        else if (fase == 4 && Paso())
        {
            Comprobar(p.Puntos == 10, "No se duplica la puntuacion del mismo piso"); Colocar(p, 12); Esperar(5);
        }
        else if (fase == 5 && Paso())
        {
            Comprobar(p.Puntos == 120 && p.Nivel == 2 && p.progreso.value == 0 && p.Jugando, "Nivel 2 a los 120 puntos, sin cambiar de escena");
            Colocar(p, 13); Esperar(6);
        }
        else if (fase == 6 && Paso())
        {
            Comprobar(p.Puntos == 130 && Mathf.Abs(p.progreso.value - 10f / 120) < 0.001f, "Barra mide el avance del nivel actual");
            destino = 24; Colocar(p, destino); Esperar(7);
        }
        else if (fase == 7 && Paso(0.1))
        {
            if (destino < 600) { destino += 12; Colocar(p, destino); inicio = EditorApplication.timeSinceStartup; return; }
            Comprobar(p.Puntos == 6000 && p.Nivel == 51, "Recorrido continuo de 600 pisos y 51 niveles");
            Comprobar(g.Reciclajes > 90 && g.DesplazamientoOrigen > 0 && Mathf.Abs(p.pelota.position.y) < 256, "Reciclaje repetido y fisica cerca del origen");
            RevisarBloques(g);
            Comprobar(object.ReferenceEquals(g.DificultadDelPiso(6000), g.dificultades[g.dificultades.Length - 1]), "La dificultad maxima se mantiene sin cerrar el recorrido");
            var bloque = g.Bloques.OrderBy(b => b.PisoInicial).First(b => b.AlturaInferior < p.pelota.position.y);
            var rojo = bloque.GetComponentsInChildren<Superficie>().First(s => s.peligrosa);
            Vector3 posicion = rojo.transform.TransformPoint(new Vector3(0.6f, 1, 2.25f));
            p.pelota.constraints = RigidbodyConstraints.FreezeRotation;
            p.pelota.isKinematic = false; p.pelota.position = posicion; p.pelota.linearVelocity = Vector3.down * 2; Esperar(8);
        }
        else if (fase == 8 && Paso(1.5))
        {
            Comprobar(!p.Jugando && p.panelResultado.activeSelf, "Colision roja termina una partida infinita");
            p.panelResultado.GetComponentInChildren<Button>().onClick.Invoke(); Esperar(9);
        }
        else if (fase == 9 && Paso(0.8))
        {
            Comprobar(p.Puntos == 0 && p.Nivel == 1 && g.Reciclajes == 0, "Reiniciar crea una partida nueva desde nivel 1");
            p.Menu(); Esperar(11);
        }
    }
}
