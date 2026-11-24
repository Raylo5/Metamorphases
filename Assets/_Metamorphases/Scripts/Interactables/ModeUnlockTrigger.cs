using UnityEngine;
using UnityEngine.Events;

// T016 · Zona invisible que desbloquea la muñequera o un color al entrar el jugador.
// Uso: un cubo con su Collider en "Is Trigger" + este script.
//  - Pasillo inicial: tipo Wristband.
//  - Entrada de cada sala: tipo Color + el color de la sala.
[RequireComponent(typeof(Collider))]
public class ModeUnlockTrigger : MonoBehaviour
{
    public enum UnlockType { Wristband, Color }

    public UnlockType type = UnlockType.Color;
    public ColorMode mode = ColorMode.Blue;
    [Tooltip("Cambia al color desbloqueado al momento (sin cooldown)")]
    public bool switchToMode = true;
    public bool onlyOnce = true;

    [Header("Eventos (mensaje del SISTEMA, sonido, paredes que aparecen...)")]
    public UnityEvent onTriggered;

    bool used;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (used && onlyOnce) return;
        if (other.GetComponentInParent<PlayerMovement>() == null) return; // solo el jugador

        ModeManager mm = ModeManager.Instance;
        if (mm == null) return;

        if (type == UnlockType.Wristband)
        {
            mm.GiveWristband();
        }
        else
        {
            mm.Unlock(mode);
            if (switchToMode) mm.ForceMode(mode);
        }

        used = true;
        onTriggered?.Invoke();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = type == UnlockType.Wristband ? new Color(1, 1, 1, 0.25f) :
            mode == ColorMode.Blue ? new Color(0.2f, 0.4f, 1f, 0.25f) :
            mode == ColorMode.Red ? new Color(1f, 0.2f, 0.2f, 0.25f) : new Color(1f, 0.85f, 0.2f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}
