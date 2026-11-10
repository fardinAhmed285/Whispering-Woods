using UnityEngine;

public class EstructuraInteractuable : MonoBehaviour
{
    // Llama a este método desde el script del Jugador cuando presione el botón de interactuar
    public void InteractuarConEstructura()
    {
        // El índice 0 corresponde a la capa "Default" en Unity.
        // Al cambiarlo a Default, el OverlapSphere del hada lo ignorará automáticamente.
        gameObject.layer = 0; 
        
        // (Opcional) Cambiar el color para feedback visual de que ya se usó
        Renderer renderizador = GetComponent<Renderer>();
        if(renderizador != null)
        {
            renderizador.material.color = Color.gray;
        }

        Debug.Log("El jugador interactuó. La estructura ahora es invisible para el hada.");
    }
}