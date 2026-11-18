using UnityEngine;

public class CamaraSeguimiento : MonoBehaviour
{
    public Transform pelota;
    public float suavizado = 7f;
    Vector3 inicio;
    float alturaInicial, menorAltura;
    void Awake()
    {
        inicio = transform.position;
        alturaInicial = pelota.position.y;
        menorAltura = alturaInicial;
    }
    void LateUpdate()
    {
        // No sube en cada rebote: conserva X, Z y la rotación inicial.
        menorAltura = Mathf.Min(menorAltura, pelota.position.y);
        Vector3 destino = inicio + Vector3.up * (menorAltura - alturaInicial);
        transform.position = Vector3.Lerp(transform.position, destino, 1 - Mathf.Exp(-suavizado * Time.deltaTime));
    }
    public void DesplazarOrigen(Vector3 ajuste)
    {
        transform.position += ajuste;
        inicio += ajuste;
        alturaInicial += ajuste.y;
        menorAltura += ajuste.y;
    }
}
