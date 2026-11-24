using UnityEngine;

// T011 · Ficha de datos de un modo (una por color). Se crea desde
// clic derecho > Create > Metamorphases > Mode Data.
// No hace nada por sí sola: los demás scripts la LEEN.
[CreateAssetMenu(fileName = "MD_NewMode", menuName = "Metamorphases/Mode Data")]
public class ModeData : ScriptableObject
{
    [Header("Identidad")]
    public ColorMode mode = ColorMode.None;
    public string displayName = "Modo";
    [Tooltip("Color con el que se tiñe el escenario")]
    public Color worldColor = Color.white;
    public Sprite icon;

    [Header("Movimiento")]
    [Tooltip("Velocidad al andar (m/s)")]
    public float moveSpeed = 6f;
    [Tooltip("Fuerza del salto normal")]
    public float jumpForce = 5f;
    [Tooltip("Saltos extra en el aire (Guantes = 1)")]
    public int extraJumps = 0;
    [Tooltip("Fuerza de cada salto extra")]
    public float extraJumpForce = 5f;
    [Tooltip("Control en el aire (0 = ninguno, 1 = mucho)")]
    [Range(0f, 1f)] public float airControl = 0.3f;

    [Header("Interacción")]
    [Tooltip("Segundos manteniendo E para activar algo")]
    public float interactTime = 0.5f;
}
