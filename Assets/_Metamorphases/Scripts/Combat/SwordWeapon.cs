using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// T013 · Espada (modo Azul, mano izquierda). Combo de 3 cortes.
// Pulsar ataque = corte. Si vuelves a pulsar durante un corte, el siguiente se encadena.
// Si tardas más que "Combo Window" en volver a pulsar, el combo vuelve al 1er corte.
// El 3er corte es más lento, hace más daño y es de tipo Heavy (SDD: más largo y grave).
public class SwordWeapon : WeaponBase
{
    [System.Serializable]
    public class Slash
    {
        [Tooltip("Duración total del corte (s)")] public float duration = 0.35f;
        [Tooltip("Momento del corte en que golpea (s)")] public float hitTime = 0.12f;
        public float damage = 1f;
        public AttackType type = AttackType.Basic;
        [Tooltip("Giro del modelo al empezar el corte")] public Vector3 startEuler;
        [Tooltip("Giro del modelo al terminar el corte")] public Vector3 endEuler;
    }

    [Header("Combo")]
    public Slash[] combo = new Slash[]
    {
        new Slash { duration = 0.35f, hitTime = 0.12f, damage = 1f, startEuler = new Vector3(0, -70, -20), endEuler = new Vector3(0, 60, 10) },
        new Slash { duration = 0.35f, hitTime = 0.12f, damage = 1f, startEuler = new Vector3(0, 60, 20),  endEuler = new Vector3(0, -70, -10) },
        new Slash { duration = 0.6f,  hitTime = 0.28f, damage = 2f, type = AttackType.Heavy, startEuler = new Vector3(-80, 0, 0), endEuler = new Vector3(60, 0, 0) },
    };
    [Tooltip("Tiempo para encadenar el siguiente corte tras acabar uno")]
    public float comboWindow = 0.5f;

    [Header("Hitbox (caja delante de la cámara)")]
    public Vector3 hitboxSize = new Vector3(1.8f, 1.4f, 2.2f);
    [Tooltip("Distancia del centro de la caja a la cámara")]
    public float hitboxDistance = 1.3f;

    [Header("Animación provisional")]
    [Tooltip("Pivote que gira al cortar (el padre del modelo). Si está vacío, usa el modelo")]
    public Transform swingPivot;

    [Header("Eventos extra")]
    public UnityEvent<int> onSlash;   // 0, 1 o 2 (para que el 3er corte suene distinto)

    int nextIndex;
    float comboTimer;
    bool attacking, queued;
    Quaternion restRotation;

    public override void Init(Transform aimTransform, LayerMask mask)
    {
        base.Init(aimTransform, mask);
        if (swingPivot == null && model != null) swingPivot = model.transform;
        if (swingPivot != null) restRotation = swingPivot.localRotation;
    }

    public override void Unequip(bool silent = false)
    {
        base.Unequip(silent);
        attacking = queued = false;
        nextIndex = 0;
        if (swingPivot != null) swingPivot.localRotation = restRotation;
    }

    public override void Tick(bool attackPressed, bool attackHeld, bool attackReleased)
    {
        if (!attacking && comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f) nextIndex = 0;   // se ha tardado demasiado: combo reiniciado
        }

        if (!attackPressed) return;
        if (attacking) queued = true;               // se encadena al acabar el corte actual
        else StartCoroutine(DoSlash(nextIndex));
    }

    IEnumerator DoSlash(int index)
    {
        attacking = true;
        queued = false;
        Slash s = combo[index];
        onAttack?.Invoke();
        onSlash?.Invoke(index);

        float t = 0f;
        bool hitDone = false;
        while (t < s.duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / s.duration);
            if (swingPivot != null)
                swingPivot.localRotation = restRotation * Quaternion.Slerp(Quaternion.Euler(s.startEuler), Quaternion.Euler(s.endEuler), EaseOut(k));

            if (!hitDone && t >= s.hitTime)
            {
                hitDone = true;
                DoHitbox(s);
            }
            yield return null;
        }
        if (swingPivot != null) swingPivot.localRotation = restRotation;

        attacking = false;
        bool wasLast = index >= combo.Length - 1;
        nextIndex = wasLast ? 0 : index + 1;
        comboTimer = wasLast ? 0f : comboWindow;

        if (queued && !wasLast) StartCoroutine(DoSlash(nextIndex));
        queued = false;
    }

    void DoHitbox(Slash s)
    {
        Vector3 center = aim.position + aim.forward * hitboxDistance;
        Collider[] cols = Physics.OverlapBox(center, hitboxSize * 0.5f, aim.rotation, hitMask, QueryTriggerInteraction.Ignore);
        HitColliders(cols, s.type, s.damage);
    }

    static float EaseOut(float k) => 1f - (1f - k) * (1f - k);

    void OnDrawGizmosSelected()
    {
        Transform a = aim != null ? aim : (Camera.main != null ? Camera.main.transform : null);
        if (a == null) return;
        Gizmos.color = new Color(0.2f, 0.4f, 1f, 0.35f);
        Gizmos.matrix = Matrix4x4.TRS(a.position + a.forward * hitboxDistance, a.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, hitboxSize);
    }
}
