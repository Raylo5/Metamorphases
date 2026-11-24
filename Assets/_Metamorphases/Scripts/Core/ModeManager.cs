using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// T008 · El "cerebro" del cambio de color.
// Guarda qué modo está activo, cuáles están desbloqueados y el cooldown.
// NO pinta, NO suena, NO mueve: solo AVISA con eventos y los demás escuchan
// (shader, música, SFX, armas, HUD...).
public class ModeManager : MonoBehaviour
{
    public static ModeManager Instance { get; private set; }

    [Header("Datos de cada modo (ScriptableObjects)")]
    [Tooltip("MD_None: blanco y negro, antes de tener color")]
    public ModeData noneMode;
    public ModeData blueMode;
    public ModeData redMode;
    public ModeData yellowMode;

    [Header("Cambio de modo")]
    [Tooltip("Segundos de espera entre cambios (1,5 - 2)")]
    public float cooldown = 1.75f;

    [Header("Pruebas")]
    [Tooltip("Empieza con muñequera y los 3 colores (para testear sin recorrer el tutorial)")]
    public bool debugUnlockAll = false;
    public ColorMode debugStartMode = ColorMode.Blue;
    [Tooltip("Escribe en la consola por qué se rechaza un cambio")]
    public bool debugLogs = true;

    [Header("Eventos (para conectar sonidos, VFX, UI...)")]
    public UnityEvent<ColorMode> onModeChanged;     // cambia el color
    public UnityEvent<ColorMode> onModeUnlocked;    // se consigue un color nuevo
    public UnityEvent onWristbandObtained;          // se consigue la muñequera
    public UnityEvent onChangeRejected;             // intento de cambio en cooldown ("error")
    public UnityEvent onCooldownFinished;           // vuelve a estar disponible ("tic")

    // ---- Lectura para otros scripts ----
    public ColorMode CurrentMode { get; private set; } = ColorMode.None;
    public ModeData CurrentData => GetData(CurrentMode);
    public bool HasWristband { get; private set; }
    public bool IsOnCooldown => cooldownTimer > 0f;
    public float CooldownProgress => cooldown <= 0f ? 1f : 1f - cooldownTimer / cooldown; // 0 -> 1
    public int UnlockedCount => unlocked.Count;

    readonly HashSet<ColorMode> unlocked = new HashSet<ColorMode>();
    float cooldownTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (debugUnlockAll)
        {
            HasWristband = true;
            unlocked.Add(ColorMode.Blue);
            unlocked.Add(ColorMode.Red);
            unlocked.Add(ColorMode.Yellow);
            CurrentMode = debugStartMode;
        }
    }

    void Update()
    {
        // Cuenta atrás del cooldown
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                cooldownTimer = 0f;
                onCooldownFinished?.Invoke();
            }
        }
    }

    public bool IsUnlocked(ColorMode mode) => unlocked.Contains(mode);

    public void GiveWristband()
    {
        if (HasWristband) return;
        HasWristband = true;
        onWristbandObtained?.Invoke();
    }

    public void Unlock(ColorMode mode)
    {
        if (mode == ColorMode.None || unlocked.Contains(mode)) return;
        unlocked.Add(mode);
        onModeUnlocked?.Invoke(mode);
    }

    // Cambio normal desde la rueda: respeta desbloqueo y cooldown.
    public bool TryChangeMode(ColorMode mode)
    {
        if (mode == CurrentMode)
        {
            if (debugLogs) Debug.Log($"[ModeManager] {mode} ya es el modo activo: no se cambia.");
            return false;
        }
        if (!IsUnlocked(mode) || IsOnCooldown)
        {
            if (debugLogs) Debug.Log(!IsUnlocked(mode)
                ? $"[ModeManager] {mode} rechazado: no está desbloqueado."
                : $"[ModeManager] {mode} rechazado: cooldown ({cooldownTimer:F2}s restantes).");
            onChangeRejected?.Invoke();
            return false;
        }
        SetMode(mode);
        cooldownTimer = cooldown;
        return true;
    }

    // Para la rueda: se ha pulsado R durante el cooldown -> suena el "error" sin abrir nada.
    public void NotifyChangeRejected()
    {
        if (debugLogs) Debug.Log($"[ModeManager] Rueda bloqueada: cooldown ({cooldownTimer:F2}s restantes).");
        onChangeRejected?.Invoke();
    }

    // Cambio forzado (al entrar en una sala, por ejemplo): sin cooldown.
    public void ForceMode(ColorMode mode)
    {
        if (mode == CurrentMode) return;
        SetMode(mode);
    }

    void SetMode(ColorMode mode)
    {
        CurrentMode = mode;
        onModeChanged?.Invoke(mode);
    }

    public ModeData GetData(ColorMode mode)
    {
        switch (mode)
        {
            case ColorMode.Blue: return blueMode;
            case ColorMode.Red: return redMode;
            case ColorMode.Yellow: return yellowMode;
            default: return noneMode;
        }
    }
}
