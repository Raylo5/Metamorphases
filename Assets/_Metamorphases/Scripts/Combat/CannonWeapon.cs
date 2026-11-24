using System.Collections;
using UnityEngine;

// T014 · Cañón láser (modo Rojo, mano derecha). Mantener ataque = disparo automático por raycast.
// Un rayo invisible sale del centro de la cámara; el láser visible (LineRenderer) va del cañón al punto de impacto.
// (Realista: zoom + sobrecalentamiento T024, disparo cargado T025.)
public class CannonWeapon : WeaponBase
{
    [Header("Disparo")]
    [Tooltip("Disparos por segundo manteniendo el botón")]
    public float fireRate = 6f;
    public float range = 60f;
    public float damage = 1f;

    [Header("Láser visible")]
    [Tooltip("Punta del cañón (de donde sale el láser visible)")]
    public Transform muzzle;
    [Tooltip("LineRenderer del láser (asígnale un material Unlit rojo)")]
    public LineRenderer laser;
    public float laserVisibleTime = 0.05f;

    [Header("Retroceso provisional")]
    public float recoilDistance = 0.08f;
    public float recoilReturnSpeed = 18f;

    float nextShotTime;
    Vector3 modelRestPos;
    Transform modelT;

    public override void Init(Transform aimTransform, LayerMask mask)
    {
        base.Init(aimTransform, mask);
        if (model != null) { modelT = model.transform; modelRestPos = modelT.localPosition; }
        if (laser != null) laser.enabled = false;
    }

    public override void Unequip(bool silent = false)
    {
        base.Unequip(silent);
        if (laser != null) laser.enabled = false;
        if (modelT != null) modelT.localPosition = modelRestPos;
    }

    public override void Tick(bool attackPressed, bool attackHeld, bool attackReleased)
    {
        // El retroceso vuelve a su sitio poco a poco
        if (modelT != null)
            modelT.localPosition = Vector3.Lerp(modelT.localPosition, modelRestPos, recoilReturnSpeed * Time.deltaTime);

        if (attackHeld && Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + 1f / Mathf.Max(fireRate, 0.01f);
            Fire();
        }
    }

    void Fire()
    {
        onAttack?.Invoke();

        Vector3 end = aim.position + aim.forward * range;
        // RaycastAll + el más cercano que no sea el jugador
        RaycastHit[] hits = Physics.RaycastAll(aim.position, aim.forward, range, hitMask, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        RaycastHit? closest = null;
        foreach (RaycastHit h in hits)
        {
            if (IsPlayer(h.collider)) continue;
            if (h.distance < best) { best = h.distance; closest = h; }
        }
        if (closest.HasValue)
        {
            end = closest.Value.point;
            HitRaycast(closest.Value, AttackType.Basic, damage);
        }

        if (modelT != null) modelT.localPosition = modelRestPos - Vector3.forward * recoilDistance;
        if (laser != null) StartCoroutine(ShowLaser(end));
    }

    IEnumerator ShowLaser(Vector3 end)
    {
        Vector3 start = muzzle != null ? muzzle.position : aim.position;
        laser.useWorldSpace = true;
        laser.positionCount = 2;
        laser.SetPosition(0, start);
        laser.SetPosition(1, end);
        laser.enabled = true;
        yield return new WaitForSeconds(laserVisibleTime);
        laser.enabled = false;
    }
}
