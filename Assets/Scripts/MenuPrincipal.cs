using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    public GameObject instrucciones;
    void Start()
    {
        Transform area = instrucciones.transform.parent;
        var jugar = area.Find("JUGAR").GetComponent<Button>();
        jugar.GetComponentInChildren<Text>().text = "JUGAR · MODO INFINITO";
        jugar.GetComponentInChildren<Text>().fontSize = 23;
        jugar.onClick = new Button.ButtonClickedEvent();
        jugar.onClick.AddListener(Jugar);
        foreach (var texto in area.GetComponentsInChildren<Text>(true))
        {
            if (texto.text.StartsWith("PROYECTO FINAL")) texto.gameObject.SetActive(false);
            if (texto.text.StartsWith("Desliza el dedo"))
            {
                texto.fontSize = 22;
                texto.text = "Desliza para girar y busca los huecos.\nEvita los sectores rojos.\n\nCada piso superado suma 10 puntos.\nCada 120 puntos subes de nivel\ny aumenta la dificultad.\n\nLa torre continúa sin final.\nTu récord se guarda automáticamente.\n\nPC: arrastra el ratón o usa las flechas.";
            }
        }
        var record = Instantiate(jugar.GetComponentInChildren<Text>(), area);
        record.name = "Record del menu";
        record.fontSize = 22; record.color = Color.white;
        record.text = "RÉCORD " + DatosPartida.Record.ToString("000");
        record.rectTransform.anchorMin = record.rectTransform.anchorMax = new Vector2(0.5f, 0.61f);
        record.rectTransform.anchoredPosition = Vector2.zero;
        record.rectTransform.sizeDelta = new Vector2(450, 45);
        instrucciones.transform.SetAsLastSibling();
    }
    public void Jugar() { Time.timeScale = 1; SceneManager.LoadScene("Juego"); }
    public void Ayuda() { instrucciones.SetActive(!instrucciones.activeSelf); }
}
