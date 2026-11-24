using UnityEngine;

// Va en el jugador. Escucha a ModeManager y equipa el arma del color activo:
// Azul -> Espada, Rojo -> Cañón, Amarillo -> Guantes, None -> nada.
// Cada frame le pasa al arma equipada el estado del botón de ataque.
public class WeaponController : MonoBehaviour
{
    [Tooltip("Desde dónde se apunta (Main Camera). Si se deja vacío, usa Camera.main")]
    public Transform aim;
    [Tooltip("Las 3 armas (hijas de la cámara). Cada una tiene su 'Mode'")]
    public WeaponBase[] weapons;
    [Tooltip("Qué capas pueden recibir golpes")]
    public LayerMask hitMask = ~0;

    public WeaponBase Current { get; private set; }

    ColorWheelUI wheel;

    void Start()
    {
        if (aim == null && Camera.main != null) aim = Camera.main.transform;
        wheel = FindAnyObjectByType<ColorWheelUI>();

        foreach (WeaponBase w in weapons)
        {
            if (w == null) continue;
            w.Init(aim, hitMask);
            w.Unequip(silent: true);
        }

        ModeManager mm = ModeManager.Instance;
        if (mm == null) { Debug.LogWarning("WeaponController: no hay ModeManager en la escena."); return; }
        mm.onModeChanged.AddListener(OnModeChanged);
        OnModeChanged(mm.CurrentMode);
    }

    void OnDestroy()
    {
        if (ModeManager.Instance != null) ModeManager.Instance.onModeChanged.RemoveListener(OnModeChanged);
    }

    void OnModeChanged(ColorMode mode)
    {
        WeaponBase next = null;
        foreach (WeaponBase w in weapons)
            if (w != null && w.mode == mode) next = w;

        if (next == Current) return;
        if (Current != null) Current.Unequip();
        Current = next;
        if (Current != null) Current.Equip();
    }

    void Update()
    {
        if (Current == null) return;

        // Con la rueda abierta (o el juego en pausa) no se ataca
        if ((wheel != null && wheel.IsOpen) || Time.timeScale == 0f)
        {
            Current.Tick(false, false, false);
            return;
        }

        var attack = GameInput.Actions.Player.Attack;
        Current.Tick(attack.WasPressedThisFrame(), attack.IsPressed(), attack.WasReleasedThisFrame());
    }
}
