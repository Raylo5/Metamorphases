using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// T018 · Elemento de color reutilizable: muro azul, bloque amarillo, interruptor rojo...
// Solo cede ante el ARMA de su color (y, opcionalmente, ante un tipo de ataque concreto).
// Con otra arma suena/tiembla como "no funciona" (onWrongHit).
// Al llegar a 0 de vida: se rompe (desaparece), se desactiva, o solo lanza el evento (interruptores).
public class ColorElement : MonoBehaviour, IHittable
{
    public enum BreakBehaviour { Disappear, DisableCollider, EventOnly }

    [Header("Regla")]
    [Tooltip("Color del arma que lo puede romper/activar")]
    public ColorMode requiredMode = ColorMode.Blue;
    [Tooltip("Tipos de ataque que cuentan (se pueden marcar varios). Muro azul: Basic + Heavy · Interruptor rojo: solo Charged")]
    public AttackTypeMask allowedTypes = AttackTypeMask.All;

    [Header("Vida")]
    [Tooltip("Espada: cortes de 1 y 3er corte de 2 · Cañón: 1 por disparo · Guantes: 0,5 por puñetazo")]
    public float health = 3f;
    public BreakBehaviour onBreak = BreakBehaviour.Disappear;
    [Tooltip("Opcional: prefab que aparece al romperse (trozos, partículas...)")]
    public GameObject brokenPrefab;

    [Header("Feedback provisional")]
    public float shakeAmount = 0.06f;
    public float shakeTime = 0.12f;

    [Header("Eventos")]
    public UnityEvent onCorrectHit;   // golpe que cuenta
    public UnityEvent onWrongHit;     // arma o ataque equivocado ("clonc")
    public UnityEvent onBroken;       // roto / activado (abrir puerta, mensaje del SISTEMA...)

    public bool IsBroken { get; private set; }

    float currentHealth;
    Vector3 restPos;
    Coroutine shakeRoutine;

    void Awake()
    {
        currentHealth = health;
        restPos = transform.localPosition;
    }

    public void OnHit(HitInfo hit)
    {
        if (IsBroken) return;

        bool rightColor = hit.mode == requiredMode;
        bool rightType = (allowedTypes & hit.type.ToMask()) != 0;
        if (!rightColor || !rightType)
        {
            onWrongHit?.Invoke();
            Shake(shakeAmount * 0.4f);
            return;
        }

        currentHealth -= hit.damage;
        onCorrectHit?.Invoke();
        Shake(shakeAmount);
        if (currentHealth <= 0f) Break();
    }

    void Break()
    {
        IsBroken = true;
        transform.localPosition = restPos;
        if (brokenPrefab != null) Instantiate(brokenPrefab, transform.position, transform.rotation);
        onBroken?.Invoke();

        switch (onBreak)
        {
            case BreakBehaviour.Disappear:
                gameObject.SetActive(false);
                break;
            case BreakBehaviour.DisableCollider:
                foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
                break;
            case BreakBehaviour.EventOnly:
                break;
        }
    }

    // Para reiniciar la sala (checkpoints, T032)
    public void ResetElement()
    {
        IsBroken = false;
        currentHealth = health;
        transform.localPosition = restPos;
        gameObject.SetActive(true);
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = true;
    }

    void Shake(float amount)
    {
        if (!isActiveAndEnabled) return;
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine(amount));
    }

    IEnumerator ShakeRoutine(float amount)
    {
        float t = 0f;
        while (t < shakeTime)
        {
            t += Time.deltaTime;
            transform.localPosition = restPos + Random.insideUnitSphere * amount;
            yield return null;
        }
        transform.localPosition = restPos;
        shakeRoutine = null;
    }
}
