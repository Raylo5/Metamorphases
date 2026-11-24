using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Base común de las 3 armas. Cada arma concreta (espada, cañón, guantes) hereda de aquí
// y solo tiene que decidir qué hace en Tick() con el botón de ataque.
public abstract class WeaponBase : MonoBehaviour
{
    [Header("Arma")]
    [Tooltip("Color/modo al que pertenece esta arma")]
    public ColorMode mode = ColorMode.Blue;
    [Tooltip("Modelo visual del arma (se enciende al equiparla)")]
    public GameObject model;

    [Header("Eventos (sonidos, partículas...)")]
    public UnityEvent onEquip;
    public UnityEvent onUnequip;
    public UnityEvent onAttack;   // cada corte / disparo / puñetazo
    public UnityEvent onHit;      // cuando golpea algo que puede recibir golpes

    public bool IsEquipped { get; private set; }

    protected Transform aim;      // la cámara: desde donde se apunta
    protected LayerMask hitMask;

    public virtual void Init(Transform aimTransform, LayerMask mask)
    {
        aim = aimTransform;
        hitMask = mask;
    }

    public virtual void Equip()
    {
        IsEquipped = true;
        if (model != null) model.SetActive(true);
        onEquip?.Invoke();
    }

    public virtual void Unequip(bool silent = false)
    {
        IsEquipped = false;
        StopAllCoroutines();
        if (model != null) model.SetActive(false);
        if (!silent) onUnequip?.Invoke();
    }

    // Lo llama WeaponController cada frame mientras el arma está equipada.
    public abstract void Tick(bool attackPressed, bool attackHeld, bool attackReleased);

    // ---------- Ayudas para golpear ----------

    protected static bool IsPlayer(Collider c) => c.GetComponentInParent<PlayerMovement>() != null;

    // Golpea a todo lo que haya dentro de una lista de colliders (una vez por objeto).
    protected int HitColliders(Collider[] cols, AttackType type, float damage)
    {
        int count = 0;
        var done = new HashSet<IHittable>();
        foreach (Collider c in cols)
        {
            if (c == null || IsPlayer(c)) continue;
            IHittable h = c.GetComponentInParent<IHittable>();
            if (h == null || done.Contains(h)) continue;
            done.Add(h);
            Vector3 point = c.ClosestPointOnBounds(aim.position); // seguro con cualquier collider
            h.OnHit(new HitInfo(mode, type, damage, point, aim.forward));
            count++;
        }
        if (count > 0) onHit?.Invoke();
        return count;
    }

    // Golpea a lo que tenga un RaycastHit concreto (cañón).
    protected bool HitRaycast(RaycastHit rh, AttackType type, float damage)
    {
        IHittable h = rh.collider.GetComponentInParent<IHittable>();
        if (h == null) return false;
        h.OnHit(new HitInfo(mode, type, damage, rh.point, aim.forward));
        onHit?.Invoke();
        return true;
    }
}
