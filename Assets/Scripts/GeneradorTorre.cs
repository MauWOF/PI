using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DificultadTorre
{
    public string nombre = "Dificultad";
    [Tooltip("Sectores consecutivos que se dejan vacios.")][Min(1)] public int sectoresHueco = 3;
    [Min(0)] public int sectoresRojos = 2;
    [Tooltip("Sectores seguros que separan el rojo de cada borde del hueco.")][Min(0)] public int margenSeguro = 2;
    [Tooltip("Giro maximo entre pisos. Se respeta un minimo que impide superponer sus huecos.")][Range(30, 180)] public float variacionGiro = 125;
    [Header("Colores de esta dificultad")]
    public Color colorFondo = new Color(0.035f, 0.06f, 0.13f);
    public Color colorPelota = new Color(1f, 0.79f, 0.26f);
    [Tooltip("Los pisos de esta dificultad muestran este color antes de que llegue la pelota.")]
    public Color colorPlataforma = new Color(0.12f, 0.88f, 0.74f);
}

[RequireComponent(typeof(GirarTorre))]
public class GeneradorTorre : MonoBehaviour
{
    [Header("Referencias del juego")]
    public Partida partida;
    public Rigidbody pelota;
    public CamaraSeguimiento camara;
    [Header("Bloques reutilizables")]
    [Range(3, 8)] public int bloquesActivos = 4;
    [Range(3, 12)] public int pisosPorBloque = 6;
    [Min(2)] public float separacion = 3.2f;
    [Tooltip("Distancia por encima de la pelota antes de reutilizar un bloque. Debe superar la altura del rebote.")]
    [Min(4)] public float margenReciclaje = 6;
    [Range(6, 24)] public int sectoresPorPiso = 12;
    [Tooltip("Cero produce un recorrido nuevo en cada partida. Otro valor permite repetirlo.")]
    public int semilla;
    [Header("Puntuacion y niveles")]
    [Min(1)] public int puntosPorPiso = 10;
    [Min(10)] public int puntosPorNivel = 120;
    [Header("Modelo y materiales de las porciones")]
    [Tooltip("Modelo suministrado de un sector de 30 grados, con su superficie superior en Y = 0.")]
    public Mesh modeloSector;
    public Material materialSeguro, materialRojo, materialColumna;
    [Min(0.1f)] public float diametroColumna = 1.2f;
    [Header("Dificultad de cada nivel (el ultimo perfil se mantiene)")]
    public DificultadTorre[] dificultades = {
        new DificultadTorre { nombre = "Inicial", sectoresHueco = 3, sectoresRojos = 2, margenSeguro = 1, variacionGiro = 125, colorFondo = new Color(0.035f, 0.06f, 0.13f), colorPelota = new Color(1f, 0.79f, 0.26f), colorPlataforma = new Color(0.12f, 0.88f, 0.74f) },
        new DificultadTorre { nombre = "Intermedia", sectoresHueco = 2, sectoresRojos = 3, margenSeguro = 1, variacionGiro = 135, colorFondo = new Color(0.025f, 0.08f, 0.15f), colorPelota = new Color(1f, 0.88f, 0.7f), colorPlataforma = new Color(0.25f, 0.62f, 1f) },
        new DificultadTorre { nombre = "Avanzada", sectoresHueco = 2, sectoresRojos = 4, margenSeguro = 0, variacionGiro = 145, colorFondo = new Color(0.085f, 0.045f, 0.14f), colorPelota = new Color(1f, 0.86f, 0.25f), colorPlataforma = new Color(0.67f, 0.48f, 0.94f) },
        new DificultadTorre { nombre = "Experta", sectoresHueco = 1, sectoresRojos = 4, margenSeguro = 0, variacionGiro = 155, colorFondo = new Color(0.105f, 0.075f, 0.035f), colorPelota = new Color(0.65f, 1f, 0.96f), colorPlataforma = new Color(1f, 0.72f, 0.28f) },
        new DificultadTorre { nombre = "Maxima", sectoresHueco = 1, sectoresRojos = 5, margenSeguro = 0, variacionGiro = 165, colorFondo = new Color(0.035f, 0.095f, 0.075f), colorPelota = new Color(0.95f, 0.9f, 1f), colorPlataforma = new Color(0.45f, 0.9f, 0.5f) }
    };
    readonly List<BloqueTorre> bloques = new List<BloqueTorre>();
    System.Random azar;
    int siguientePiso, primero;
    float ultimoCentro = 60;
    float anchoHuecoAnterior;
    double desplazamientoOrigen;
    Material[] materialesSeguros;
    public Mesh Malla { get; private set; }
    public IReadOnlyList<BloqueTorre> Bloques => bloques;
    public int Reciclajes { get; private set; }
    public double DesplazamientoOrigen => desplazamientoOrigen;
    public int PisosSuperados => Mathf.Max(0, (int)Math.Floor((desplazamientoOrigen - pelota.position.y + transform.position.y - 0.45) / separacion) + 1);
    public int PisosPorNivel => Mathf.Max(1, puntosPorNivel / puntosPorPiso);

    void OnValidate()
    {
        bloquesActivos = Mathf.Clamp(bloquesActivos, 3, 8);
        pisosPorBloque = Mathf.Clamp(pisosPorBloque, 3, 12);
        sectoresPorPiso = Mathf.Clamp(sectoresPorPiso, 6, 24);
        separacion = Mathf.Max(2, separacion);
        margenReciclaje = Mathf.Clamp(margenReciclaje, 4, (bloquesActivos - 1) * pisosPorBloque * separacion * 0.5f);
        puntosPorPiso = Mathf.Max(1, puntosPorPiso);
        puntosPorNivel = Mathf.Max(puntosPorPiso, Mathf.RoundToInt((float)puntosPorNivel / puntosPorPiso) * puntosPorPiso);
        diametroColumna = Mathf.Max(0.1f, diametroColumna);
        if (dificultades == null || dificultades.Length == 0) dificultades = new[] { new DificultadTorre() };
        foreach (var d in dificultades)
        {
            if (d == null) continue;
            d.sectoresHueco = Mathf.Clamp(d.sectoresHueco, 1, sectoresPorPiso / 2 - 1);
            d.sectoresRojos = Mathf.Clamp(d.sectoresRojos, 0, sectoresPorPiso - d.sectoresHueco - 1);
            d.margenSeguro = Mathf.Clamp(d.margenSeguro, 0, (sectoresPorPiso - d.sectoresHueco - 1) / 2);
            d.variacionGiro = Mathf.Clamp(d.variacionGiro, 30, 180);
        }
    }
    public void Inicializar()
    {
        if (bloques.Count > 0) return;
        OnValidate();
        if (modeloSector == null || materialSeguro == null || materialRojo == null || materialColumna == null)
            throw new InvalidOperationException("Asigna el modelo y los tres materiales en Generador Torre.");
        if (!modeloSector.isReadable)
            throw new InvalidOperationException("El modelo de sector necesita Read/Write para adaptar sus porciones.");
        azar = new System.Random(semilla == 0 ? Environment.TickCount : semilla);
        // Una sola malla compartida adapta la pieza de 30 grados a la cantidad de sectores elegida.
        Malla = Instantiate(modeloSector);
        Malla.name = "Sector de la torre";
        var vertices = Malla.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            float radio = new Vector2(vertices[i].x, vertices[i].z).magnitude;
            if (radio < 0.001f) continue;
            float angulo = (Mathf.Atan2(vertices[i].x, vertices[i].z) * Mathf.Rad2Deg - 30) * (360f / sectoresPorPiso / 30) * Mathf.Deg2Rad;
            vertices[i] = new Vector3(Mathf.Sin(angulo) * radio, vertices[i].y, Mathf.Cos(angulo) * radio);
        }
        Malla.vertices = vertices; Malla.RecalculateNormals(); Malla.RecalculateBounds();
        // Un material compartido por dificultad; los sectores rojos conservan su material.
        materialesSeguros = new Material[dificultades.Length];
        for (int i = 0; i < materialesSeguros.Length; i++)
        {
            var dificultad = DificultadDelNivel(i + 1);
            materialesSeguros[i] = new Material(materialSeguro);
            materialesSeguros[i].name = "Plataforma " + dificultad.nombre;
            materialesSeguros[i].color = dificultad.colorPlataforma;
        }
        for (int i = 0; i < bloquesActivos; i++)
        {
            var bloque = new GameObject("Bloque " + (i + 1)).AddComponent<BloqueTorre>();
            bloque.transform.SetParent(transform, false);
            bloque.Inicializar(this);
            Preparar(bloque);
            bloques.Add(bloque);
        }
        Physics.SyncTransforms();
    }
    public DificultadTorre DificultadDelPiso(int piso)
    {
        return DificultadDelNivel(piso / PisosPorNivel + 1);
    }
    public DificultadTorre DificultadDelNivel(int nivel)
    {
        int indice = Mathf.Clamp(nivel - 1, 0, dificultades.Length - 1);
        return dificultades[indice] ?? dificultades[0] ?? new DificultadTorre();
    }
    public Material MaterialSeguroDelPiso(int piso)
    {
        return materialesSeguros[Mathf.Clamp(piso / PisosPorNivel, 0, materialesSeguros.Length - 1)];
    }
    public float SiguienteCentro(int piso, DificultadTorre dificultad)
    {
        float anchoActual = Mathf.Clamp(dificultad.sectoresHueco, 1, sectoresPorPiso / 2 - 1) * (360f / sectoresPorPiso);
        if (piso > 0)
        {
            // Deja una separacion angular entre ambos huecos, tambien al cambiar de bloque o nivel.
            float giroMinimo = (anchoHuecoAnterior + anchoActual) * 0.5f + 10;
            float giroMaximo = Mathf.Clamp(dificultad.variacionGiro, giroMinimo, 180);
            float giro = Mathf.Lerp(giroMinimo, giroMaximo, (float)azar.NextDouble());
            ultimoCentro += azar.Next(2) == 0 ? -giro : giro;
        }
        anchoHuecoAnterior = anchoActual;
        ultimoCentro = Mathf.Repeat(ultimoCentro, 360);
        return ultimoCentro;
    }
    void Preparar(BloqueTorre bloque)
    {
        bloque.Configurar(siguientePiso, desplazamientoOrigen);
        siguientePiso += pisosPorBloque;
    }
    void FixedUpdate()
    {
        if (bloques.Count == 0 || !partida.Jugando) return;
        // Conserva espacio por encima del rebote; solo se reutilizan pisos inalcanzables.
        float alturaRebote = pelota.GetComponent<Pelota>().fuerzaRebote;
        float margen = Mathf.Max(margenReciclaje, alturaRebote * alturaRebote / (2 * Mathf.Abs(Physics.gravity.y)) + 1);
        bool cambio = false;
        while (bloques[primero].AlturaInferior > pelota.position.y + margen)
        {
            Preparar(bloques[primero]);
            primero = (primero + 1) % bloques.Count;
            Reciclajes++; cambio = true;
        }
        // Mantiene la fisica cerca del origen incluso en partidas muy largas.
        if (pelota.position.y < -256)
        {
            Vector3 ajuste = Vector3.up * 256;
            desplazamientoOrigen += 256;
            pelota.position += ajuste;
            foreach (var bloque in bloques) bloque.transform.position += ajuste;
            if (camara != null) camara.DesplazarOrigen(ajuste);
            cambio = true;
        }
        if (cambio) Physics.SyncTransforms();
    }
    void OnDestroy()
    {
        if (Malla != null) Destroy(Malla);
        if (materialesSeguros != null)
            foreach (var material in materialesSeguros) if (material != null) Destroy(material);
    }
}
