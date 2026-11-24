using UnityEngine;

// T010 (parte de código) · Tiñe TODO el escenario.
// Escucha a ModeManager y manda dos valores GLOBALES a todos los shaders:
//   _MM_ModeColor  -> el color del modo (azul / rojo / amarillo)
//   _MM_ColorAmount -> 0 = blanco y negro, 1 = teñido del todo
// El Shader Graph del entorno (SG_MonoWorld) lee esos dos valores.
public class WorldColorController : MonoBehaviour
{
    [Tooltip("Rapidez de la transición de color (más alto = más rápido)")]
    public float transitionSpeed = 6f;

    [Header("Niebla")]
    [Tooltip("Tiñe también la niebla (debe coincidir con el horizonte del skybox)")]
    public bool tintFog = true;
    [Tooltip("Pon el mismo valor que 'Fuerza del tinte (cielo)' del material del skybox")]
    [Range(0f, 1f)] public float fogTintStrength = 0.6f;

    Color baseFogColor;

    static readonly int ColorID = Shader.PropertyToID("_MM_ModeColor");
    static readonly int AmountID = Shader.PropertyToID("_MM_ColorAmount");

    Color currentColor = Color.white, targetColor = Color.white;
    float currentAmount, targetAmount;

    void Start()
    {
        baseFogColor = RenderSettings.fogColor;  // el color que pusiste en Lighting > Fog

        ModeManager mm = ModeManager.Instance;
        if (mm == null) { Debug.LogWarning("WorldColorController: no hay ModeManager en la escena."); return; }

        mm.onModeChanged.AddListener(OnModeChanged);
        SetTarget(mm.CurrentMode);
        currentColor = targetColor;   // al empezar, sin transición
        currentAmount = targetAmount;
        Push();
    }

    void OnDestroy()
    {
        if (ModeManager.Instance != null)
            ModeManager.Instance.onModeChanged.RemoveListener(OnModeChanged);
    }

    void OnModeChanged(ColorMode mode) => SetTarget(mode);

    void SetTarget(ColorMode mode)
    {
        ModeData data = ModeManager.Instance.GetData(mode);
        if (mode == ColorMode.None)
        {
            targetAmount = 0f;          // vuelve a blanco y negro (mantiene el último color)
        }
        else
        {
            if (currentAmount < 0.01f && data != null) currentColor = data.worldColor; // de B/N a color: sin mezcla rara
            targetColor = data != null ? data.worldColor : Color.white;
            targetAmount = 1f;
        }
    }

    void Update()
    {
        // Acercamiento suave (como un "Lerp" con amortiguación)
        float t = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime);
        currentColor = Color.Lerp(currentColor, targetColor, t);
        currentAmount = Mathf.Lerp(currentAmount, targetAmount, t);
        Push();
    }

    void Push()
    {
        Shader.SetGlobalColor(ColorID, currentColor);
        Shader.SetGlobalFloat(AmountID, currentAmount);

        if (tintFog)
        {
            Color tint = Color.Lerp(Color.white, currentColor, currentAmount * fogTintStrength);
            RenderSettings.fogColor = baseFogColor * tint;
        }
    }

    void OnDisable()
    {
        // Deja la niebla como estaba (si no, al parar el Play se quedaría teñida en la escena)
        if (tintFog) RenderSettings.fogColor = baseFogColor;
    }
}
