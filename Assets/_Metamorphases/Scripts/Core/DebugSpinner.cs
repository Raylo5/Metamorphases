using UnityEngine;

// SOLO PARA PRUEBAS · Hace girar y flotar un objeto usando el tiempo del juego.
// Sirve para ver la cámara lenta de la rueda (y cualquier cambio de Time.timeScale).
// Cuando ya no haga falta, quítalo del objeto.
public class DebugSpinner : MonoBehaviour
{
    public float degreesPerSecond = 90f;
    public float bobHeight = 0.5f;
    public float bobSpeed = 2f;

    Vector3 startPos;

    void Start() => startPos = transform.position;

    void Update()
    {
        // Time.deltaTime y Time.time van más lentos cuando timeScale < 1
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
    }
}
