using UnityEngine;

public class WingFlap : MonoBehaviour
{
    public float flapSpeed = 25f;   // Velocidad del aleteo
    public float maxAngle = 20f;    // Cuánto se abren y cierran las alas
    public bool invertDirection = false; // Para que la otra ala se mueva al revés

    private Quaternion baseRotation;

    void Start()
    {
        // Guardamos la rotación inicial que ya configuraste
        baseRotation = transform.localRotation;
    }

    void Update()
    {
        float angle = Mathf.Sin(Time.time * flapSpeed) * maxAngle;
        
        if (invertDirection)
        {
            angle = -angle;
        }

        // Rota únicamente en el Eje X desde la posición del pivote
        transform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}