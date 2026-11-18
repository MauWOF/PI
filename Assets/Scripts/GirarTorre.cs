using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class GirarTorre : MonoBehaviour
{
    public Partida partida;
    public float sensibilidad = 260f;
    Rigidbody cuerpo;
    bool arrastrando;
    float anterior, pendiente;
    void Awake() { cuerpo = GetComponent<Rigidbody>(); }
    void Update()
    {
        if (!partida.Jugando) { arrastrando = false; pendiente = 0; return; }
        bool pulsado = false;
        float x = 0;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            pulsado = true;
            x = Touchscreen.current.primaryTouch.position.ReadValue().x;
        }
        else if (Mouse.current != null)
        {
            pulsado = Mouse.current.leftButton.isPressed;
            x = Mouse.current.position.ReadValue().x;
        }
        if (pulsado && !arrastrando && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (pulsado && arrastrando) pendiente -= (x - anterior) / Screen.width * sensibilidad;
        anterior = x;
        arrastrando = pulsado;
        if (Keyboard.current != null)
        {
            float direccion = (Keyboard.current.rightArrowKey.isPressed ? 1 : 0) - (Keyboard.current.leftArrowKey.isPressed ? 1 : 0);
            pendiente += direccion * 110 * Time.deltaTime;
        }
    }
    void FixedUpdate()
    {
        if (!partida.Jugando) return;
        cuerpo.MoveRotation(cuerpo.rotation * Quaternion.Euler(0, pendiente, 0));
        pendiente = 0;
    }
}
