using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class Pelota : MonoBehaviour
{
    public Partida partida;
    public float fuerzaRebote = 7f;
    Rigidbody cuerpo;
    float ultimoRebote = -1;
    void Awake() { cuerpo = GetComponent<Rigidbody>(); }
    void OnCollisionEnter(Collision choque)
    {
        if (!partida.Jugando) return;
        // Solo cuenta la cara superior de las plataformas.
        bool encima = false;
        foreach (var contacto in choque.contacts)
            if (contacto.normal.y > 0.5f) encima = true;
        if (!encima) return;
        var superficie = choque.collider.GetComponent<Superficie>();
        if (superficie == null) return;
        if (superficie.meta) { partida.Terminar(true); return; }
        if (superficie.peligrosa) { partida.Terminar(false); return; }
        if (Time.time - ultimoRebote < 0.08f) return;
        ultimoRebote = Time.time;
        cuerpo.linearVelocity = Vector3.up * fuerzaRebote;
        partida.SonarRebote();
    }
}
