using UnityEngine;

public class BloqueTorre : MonoBehaviour
{
    [SerializeField] int pisoInicial;
    [SerializeField] int cantidadPisos;
    GeneradorTorre generador;
    Superficie[,] sectores;
    public int PisoInicial => pisoInicial;
    public float AlturaInferior => transform.position.y - (cantidadPisos - 1) * generador.separacion;

    public void Inicializar(GeneradorTorre configuracion)
    {
        generador = configuracion;
        cantidadPisos = generador.pisosPorBloque;
        sectores = new Superficie[cantidadPisos, generador.sectoresPorPiso];
        for (int piso = 0; piso < cantidadPisos; piso++)
            for (int sector = 0; sector < generador.sectoresPorPiso; sector++)
            {
                var pieza = new GameObject("Sector", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(Superficie));
                pieza.transform.SetParent(transform, false);
                pieza.transform.localPosition = Vector3.down * piso * generador.separacion;
                pieza.GetComponent<MeshFilter>().sharedMesh = generador.Malla;
                var collider = pieza.GetComponent<MeshCollider>();
                collider.sharedMesh = generador.Malla; collider.convex = true;
                sectores[piso, sector] = pieza.GetComponent<Superficie>();
            }
        var columna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        columna.name = "Columna del bloque";
        columna.transform.SetParent(transform, false);
        float altura = cantidadPisos * generador.separacion;
        columna.transform.localPosition = Vector3.up * (generador.separacion - altura) * 0.5f;
        columna.transform.localScale = new Vector3(generador.diametroColumna, altura * 0.5f, generador.diametroColumna);
        columna.GetComponent<Renderer>().sharedMaterial = generador.materialColumna;
    }
    public void Configurar(int primerPiso, double desplazamiento)
    {
        pisoInicial = primerPiso;
        transform.localPosition = Vector3.up * (float)(desplazamiento - (double)primerPiso * generador.separacion);
        float paso = 360f / generador.sectoresPorPiso;
        for (int piso = 0; piso < cantidadPisos; piso++)
        {
            int indice = primerPiso + piso;
            var dificultad = generador.DificultadDelPiso(indice);
            Material materialDelPiso = generador.MaterialSeguroDelPiso(indice);
            int hueco = Mathf.Clamp(dificultad.sectoresHueco, 1, generador.sectoresPorPiso / 2 - 1);
            float centro = generador.SiguienteCentro(indice, dificultad);
            // Marca peligros desde ambos bordes hacia dentro, respetando el margen seguro.
            int[] ordenPeligro = new int[generador.sectoresPorPiso];
            for (int s = 0; s < ordenPeligro.Length; s++) ordenPeligro[s] = int.MaxValue;
            int orden = 0;
            for (int margen = dificultad.margenSeguro; margen < generador.sectoresPorPiso; margen++)
            {
                int izquierda = hueco + margen, derecha = generador.sectoresPorPiso - 1 - margen;
                if (izquierda > derecha) break;
                ordenPeligro[izquierda] = orden++;
                if (derecha != izquierda) ordenPeligro[derecha] = orden++;
            }
            int rojos = indice < 2 ? 0 : Mathf.Min(dificultad.sectoresRojos, generador.sectoresPorPiso - hueco - 1);
            for (int s = 0; s < generador.sectoresPorPiso; s++)
            {
                var pieza = sectores[piso, s];
                pieza.gameObject.SetActive(s >= hueco);
                pieza.transform.localRotation = Quaternion.Euler(0, centro - hueco * paso * 0.5f + s * paso, 0);
                pieza.peligrosa = s >= hueco && ordenPeligro[s] < rojos;
                pieza.name = pieza.peligrosa ? "Sector rojo" : "Sector seguro";
                pieza.GetComponent<Renderer>().sharedMaterial = pieza.peligrosa ? generador.materialRojo : materialDelPiso;
            }
        }
    }
}
