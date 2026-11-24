using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Cinemachine;

// T009 · Rueda de colores (versión de prueba).
//  - Siempre: un indicador abajo a la izquierda muestra el color activo.
//  - Durante el cooldown la rueda NO se abre (solo suena el "error") y el indicador se ve apagado.
//  - Mantener R: aparece la rueda grande en el centro. El color ACTIVO siempre arriba,
//    los otros dos abajo-izquierda y abajo-derecha (la pantalla se divide en 3 tercios).
//    Apuntar arriba (o no mover el ratón) = quedarse igual.
//  - Soltar R: se pide el cambio a ModeManager (que decide si se puede por el cooldown).
//  - Opcional: cámara lenta mientras eliges y tiempo mínimo de pulsación (anti-pulsación sin querer).
public class ColorWheelUI : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public ColorMode mode = ColorMode.Blue;
        [Tooltip("Image del sector dentro de la rueda grande")]
        public RectTransform root;
    }

    [Header("Rueda grande (al mantener R)")]
    [Tooltip("Panel de la rueda grande, centrado en pantalla")]
    public GameObject wheelRoot;
    [Tooltip("En orden de la rueda (sentido horario): Azul, Rojo, Amarillo")]
    public Slot[] slots = new Slot[3];
    [Tooltip("Distancia del centro a cada sector (píxeles)")]
    public float slotRadius = 160f;
    [Tooltip("El 'Cinemachine Input Axis Controller' de FirstPersonCamera (para congelar SOLO el giro de la cámara). Si se deja vacío, se busca solo.")]
    public CinemachineInputAxisController cameraInputController;

    [Header("Indicador pequeño (abajo a la izquierda)")]
    [Tooltip("Image que muestra el color activo")]
    public Image activeIndicator;
    public Color noColorTint = new Color(0.5f, 0.5f, 0.5f, 1f);
    [Tooltip("Opacidad del indicador mientras dura el cooldown (provisional, hasta tener la animación)")]
    [Range(0f, 1f)] public float cooldownAlpha = 0.35f;

    [Header("Ajustes de selección")]
    [Tooltip("Distancia mínima que hay que mover el ratón para elegir otro color")]
    public float deadZone = 40f;
    public float mouseSensitivity = 1f;
    public float highlightScale = 1.2f;
    [Tooltip("Segundos mínimos manteniendo R para que el cambio cuente (evita pulsaciones sin querer)")]
    public float minHoldTime = 0.15f;
    [Tooltip("Velocidad del tiempo mientras eliges (1 = sin cámara lenta)")]
    [Range(0.05f, 1f)] public float slowMotionScale = 1f;
    [Tooltip("Escribe en la consola qué se ha elegido y por qué no ha cambiado (para pruebas)")]
    public bool debugLogs = true;

    [Header("Eventos (sonidos: abrir, seleccionar...)")]
    public UnityEvent onWheelOpened;
    public UnityEvent onWheelClosed;
    public UnityEvent onSlotHovered;

    public bool IsOpen { get; private set; }

    Vector2 virtualCursor;
    Slot hovered;
    float openTime;
    float savedTimeScale = 1f, savedFixedDelta = 0.02f;
    readonly float[] slotAngles = new float[3]; // ángulo de cada sector en esta apertura

    void Start()
    {
        if (wheelRoot != null) wheelRoot.SetActive(false);
        if (cameraInputController == null)
            cameraInputController = FindAnyObjectByType<CinemachineInputAxisController>();
        if (cameraInputController == null)
            Debug.LogWarning("ColorWheelUI: no encuentro el CinemachineInputAxisController; la cámara girará con la rueda abierta.");
    }

    void Update()
    {
        ModeManager mm = ModeManager.Instance;
        if (mm == null) return;

        UpdateIndicator(mm);

        var player = GameInput.Actions.Player;
        if (!IsOpen)
        {
            if (player.ColorWheel.WasPressedThisFrame() && mm.HasWristband && mm.UnlockedCount > 0)
            {
                // Durante el cooldown la rueda NO se abre: solo se avisa (sonido de "error" del SDD)
                if (mm.IsOnCooldown) mm.NotifyChangeRejected();
                else Open(mm);
            }
            return;
        }

        // Ratón: acumula el movimiento como un cursor invisible. Mando: el stick apunta directo.
        Vector2 look = player.Look.ReadValue<Vector2>();
        if (player.Look.activeControl != null && player.Look.activeControl.device is Gamepad)
            virtualCursor = look * deadZone * 3f;
        else
            virtualCursor = Vector2.ClampMagnitude(virtualCursor + look * mouseSensitivity, deadZone * 3f);

        UpdateHover(mm);

        // Se cierra en cuanto R deja de estar pulsada (más fiable que esperar el frame exacto de "soltar")
        if (!player.ColorWheel.IsPressed())
            Close(mm);
    }

    // ---------- Indicador pequeño ----------

    void UpdateIndicator(ModeManager mm)
    {
        if (activeIndicator == null) return;
        bool show = mm.HasWristband;
        if (activeIndicator.gameObject.activeSelf != show) activeIndicator.gameObject.SetActive(show);
        ModeData data = mm.CurrentData;
        Color c = (mm.CurrentMode != ColorMode.None && data != null) ? data.worldColor : noColorTint;
        // Provisional: durante el cooldown el indicador se ve apagado y se va "llenando" hasta estar disponible
        if (mm.IsOnCooldown) c.a = Mathf.Lerp(cooldownAlpha, 1f, mm.CooldownProgress);
        activeIndicator.color = c;
    }

    // ---------- Rueda grande ----------

    void Open(ModeManager mm)
    {
        IsOpen = true;
        virtualCursor = Vector2.zero;
        hovered = null;
        openTime = Time.unscaledTime;

        // Colocar los sectores: el activo arriba (0°), el siguiente a la derecha (120°),
        // el otro a la izquierda (240°). Si aún no hay color, el primero va arriba.
        int activeIndex = 0;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].mode == mm.CurrentMode) activeIndex = i;

        for (int i = 0; i < slots.Length; i++)
        {
            Slot s = slots[i];
            int offset = (i - activeIndex + slots.Length) % slots.Length; // 0 = activo
            slotAngles[i] = offset * 120f;
            if (s.root == null) continue;
            float rad = slotAngles[i] * Mathf.Deg2Rad;
            s.root.anchoredPosition = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * slotRadius;
            s.root.localScale = Vector3.one;
            s.root.gameObject.SetActive(mm.IsUnlocked(s.mode));
        }

        if (wheelRoot != null) wheelRoot.SetActive(true);
        if (cameraInputController != null) cameraInputController.enabled = false;

        if (slowMotionScale < 1f)
        {
            savedTimeScale = Time.timeScale;
            savedFixedDelta = Time.fixedDeltaTime;
            Time.timeScale = slowMotionScale;
            Time.fixedDeltaTime = savedFixedDelta * slowMotionScale; // física suave a cámara lenta
        }
        onWheelOpened?.Invoke();
    }

    void Close(ModeManager mm)
    {
        IsOpen = false;
        if (wheelRoot != null) wheelRoot.SetActive(false);
        if (cameraInputController != null) cameraInputController.enabled = true;

        if (slowMotionScale < 1f)
        {
            Time.timeScale = savedTimeScale;
            Time.fixedDeltaTime = savedFixedDelta;
        }

        float held = Time.unscaledTime - openTime;
        bool heldLongEnough = held >= minHoldTime;
        if (hovered != null && heldLongEnough)
        {
            bool changed = mm.TryChangeMode(hovered.mode);
            if (debugLogs) Debug.Log($"[Rueda] Elegido {hovered.mode} · mantenida {held:F2}s · {(changed ? "CAMBIADO" : "NO cambiado (mira el aviso de ModeManager)")}");
        }
        else if (debugLogs)
        {
            if (hovered == null) Debug.Log($"[Rueda] Cerrada sin elegir (cursor en el centro o arriba) · mantenida {held:F2}s");
            else Debug.Log($"[Rueda] {hovered.mode} NO aplicado: R soltada demasiado rápido ({held:F2}s < Min Hold Time {minHoldTime:F2}s)");
        }
        onWheelClosed?.Invoke();
    }

    void UpdateHover(ModeManager mm)
    {
        Slot best = null;
        if (virtualCursor.magnitude >= deadZone)
        {
            // Ángulo del cursor: 0 arriba, crece en sentido horario -> 3 tercios de 120°
            float cursorAngle = Mathf.Atan2(virtualCursor.x, virtualCursor.y) * Mathf.Rad2Deg;
            if (cursorAngle < 0f) cursorAngle += 360f;

            float bestDiff = float.MaxValue;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!mm.IsUnlocked(slots[i].mode)) continue;
                float diff = Mathf.Abs(Mathf.DeltaAngle(cursorAngle, slotAngles[i]));
                if (diff < bestDiff) { bestDiff = diff; best = slots[i]; }
            }
        }

        if (best == hovered) return;
        hovered = best;
        foreach (Slot s in slots)
            if (s.root != null) s.root.localScale = Vector3.one * (s == hovered ? highlightScale : 1f);
        if (hovered != null) onSlotHovered?.Invoke();
    }

    void OnDisable()
    {
        // Seguridad: si se desactiva con la rueda abierta, no dejar el tiempo lento
        if (IsOpen && slowMotionScale < 1f)
        {
            Time.timeScale = savedTimeScale;
            Time.fixedDeltaTime = savedFixedDelta;
        }
    }
}
