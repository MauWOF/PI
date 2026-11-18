using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public static class DatosPartida
{
    public const string ClaveRecord = "HelixJump.Record";
    public static int Record => Mathf.Max(0, PlayerPrefs.GetInt(ClaveRecord, 0));
    public static void GuardarRecord(int puntos)
    {
        if (puntos <= Record) return;
        PlayerPrefs.SetInt(ClaveRecord, puntos);
        PlayerPrefs.Save();
    }
}

public class Partida : MonoBehaviour
{
    public Rigidbody pelota;
    public GeneradorTorre generador;
    public GameObject panelPausa, panelResultado;
    public Text textoPuntos, textoResultado;
    public Slider progreso;
    public AudioClip rebote, victoria, derrota;
    [Header("Cambio de color al subir de nivel")]
    [Min(0.1f)] public float velocidadCambioColor = 3f;
    public bool Jugando { get; private set; }
    public int Nivel => Puntos / generador.puntosPorNivel + 1;
    public int Puntos => superados * generador.puntosPorPiso;
    public int Superados => superados;
    AudioSource sonido;
    Text textoRecord, textoNivel;
    int superados, nivelAnterior = 1;
    bool terminada;
    Camera camaraJuego;
    Renderer aspectoPelota;
    MaterialPropertyBlock propiedadesPelota;
    Color fondoDestino, pelotaDestino, colorPelotaActual;

    void Start()
    {
        Time.timeScale = 1;
        Application.targetFrameRate = 60;
        sonido = GetComponent<AudioSource>();
        generador.Inicializar();
        ConfigurarInterfaz();
        PrepararColores();
        Jugando = true;
        panelPausa.SetActive(false);
        panelResultado.SetActive(false);
        ActualizarInterfaz();
    }
    void ConfigurarInterfaz()
    {
        Transform area = textoPuntos.transform.parent;
        foreach (var texto in area.GetComponentsInChildren<Text>(true))
            if (texto.text.StartsWith("NIVEL")) textoNivel = texto;
        textoPuntos.fontSize = 40;
        textoPuntos.rectTransform.sizeDelta = new Vector2(400, 80);
        textoRecord = Instantiate(textoPuntos, area);
        textoRecord.name = "Record";
        textoRecord.fontSize = 22;
        textoRecord.rectTransform.anchorMin = textoRecord.rectTransform.anchorMax = new Vector2(0.5f, 0.79f);
        textoRecord.rectTransform.anchoredPosition = Vector2.zero;
        textoRecord.rectTransform.sizeDelta = new Vector2(420, 45);
        progreso.fillRect.anchorMin = Vector2.zero;
        progreso.fillRect.anchorMax = Vector2.one;
        progreso.fillRect.offsetMin = progreso.fillRect.offsetMax = Vector2.zero;
        progreso.value = 1; progreso.value = 0;
        var reintentar = panelResultado.GetComponentInChildren<Button>(true);
        reintentar.onClick = new Button.ButtonClickedEvent();
        reintentar.onClick.AddListener(Reiniciar);
        reintentar.GetComponentInChildren<Text>().text = "VOLVER A JUGAR";
        textoResultado.fontSize = 28;
    }
    void PrepararColores()
    {
        camaraJuego = generador.camara.GetComponent<Camera>();
        camaraJuego.clearFlags = CameraClearFlags.SolidColor;
        aspectoPelota = pelota.GetComponent<Renderer>();
        propiedadesPelota = new MaterialPropertyBlock();
        aspectoPelota.GetPropertyBlock(propiedadesPelota);
        ElegirColores();
        camaraJuego.backgroundColor = fondoDestino;
        colorPelotaActual = pelotaDestino;
        AplicarColorPelota();
    }
    void ElegirColores()
    {
        var dificultad = generador.DificultadDelNivel(Nivel);
        fondoDestino = dificultad.colorFondo;
        pelotaDestino = dificultad.colorPelota;
    }
    void AplicarColorPelota()
    {
        // Cambia solo el color; el material mate no necesita copias por nivel.
        propiedadesPelota.SetColor("_BaseColor", colorPelotaActual);
        propiedadesPelota.SetColor("_Color", colorPelotaActual);
        aspectoPelota.SetPropertyBlock(propiedadesPelota);
    }
    void Update()
    {
        float transicion = 1 - Mathf.Exp(-Mathf.Max(0.1f, velocidadCambioColor) * Time.deltaTime);
        camaraJuego.backgroundColor = Color.Lerp(camaraJuego.backgroundColor, fondoDestino, transicion);
        colorPelotaActual = Color.Lerp(colorPelotaActual, pelotaDestino, transicion);
        AplicarColorPelota();
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !terminada) Pausar();
        if (!Jugando) return;
        int alcanzados = generador.PisosSuperados;
        // El maximo impide restar o duplicar puntos al rebotar y al reutilizar bloques.
        if (alcanzados > superados)
        {
            superados = alcanzados;
            DatosPartida.GuardarRecord(Puntos);
            if (Nivel > nivelAnterior)
            {
                nivelAnterior = Nivel;
                ElegirColores();
                if (victoria != null) sonido.PlayOneShot(victoria, 0.35f);
            }
            ActualizarInterfaz();
        }
    }
    void ActualizarInterfaz()
    {
        textoPuntos.text = "PUNTOS " + Puntos.ToString("000");
        textoRecord.text = "RÉCORD " + DatosPartida.Record.ToString("000");
        textoNivel.text = "NIVEL " + Nivel.ToString("00");
        int avance = Puntos % generador.puntosPorNivel;
        progreso.value = (float)avance / generador.puntosPorNivel;
    }
    public void SonarRebote() { if (rebote != null) sonido.PlayOneShot(rebote, 0.35f); }
    public void Pausar()
    {
        if (terminada) return;
        Jugando = !Jugando;
        panelPausa.SetActive(!Jugando);
        Time.timeScale = Jugando ? 1 : 0;
    }
    public void Terminar(bool victoriaDelNivel)
    {
        // En el recorrido infinito no hay plataforma final: solo termina al tocar rojo.
        if (terminada || victoriaDelNivel) return;
        terminada = true;
        Jugando = false;
        pelota.linearVelocity = Vector3.zero;
        pelota.isKinematic = true;
        DatosPartida.GuardarRecord(Puntos);
        textoResultado.text = "¡INTÉNTALO OTRA VEZ!\n" + Puntos + " PUNTOS · NIVEL " + Nivel;
        panelPausa.SetActive(false);
        panelResultado.SetActive(true);
        if (derrota != null) sonido.PlayOneShot(derrota, 0.6f);
    }
    public void Reiniciar() { Time.timeScale = 1; SceneManager.LoadScene("Juego"); }
    public void Menu() { Time.timeScale = 1; SceneManager.LoadScene("Menu"); }
    void OnApplicationPause(bool pausa) { if (pausa && Jugando) Pausar(); }
    void OnDestroy() { Time.timeScale = 1; }
}
