using UnityEngine;

public class DetectorCube : MonoBehaviour
{
    public FairyBehavior referenciaHada; 
    public float duracionEvasion = 10f;

    // Se ejecuta automáticamente cuando un objeto entra en su área
    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto que entró tiene la etiqueta "Player"
        if (other.CompareTag("Player"))
        {
            EjecutarEfecto();
        }
    }

    private void EjecutarEfecto()
    {
        if (referenciaHada != null)
        {
            referenciaHada.ActivarEvasionTemporal(duracionEvasion);
        }
        else
        {
            Debug.LogWarning("Falta asignar el hada en el Inspector del Detector Cube");
        }

        // Destruye el cubo al recogerlo
        Destroy(gameObject);
    }
}