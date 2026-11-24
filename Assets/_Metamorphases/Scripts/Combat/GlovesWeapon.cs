using UnityEngine;

// T015 · Guantes (modo Amarillo, ambas manos). Mantener ataque = ráfaga de puñetazos alternando manos.
// Cada puñetazo golpea lo que haya en una esfera justo delante de la cámara.
// (Realista: correr con stamina T027, puñetazo ascendente cargado T028.)
public class GlovesWeapon : WeaponBase
{
    [Header("Ráfaga")]
    [Tooltip("Segundos entre puñetazo y puñetazo")]
    public float punchInterval = 0.12f;
    public float damage = 0.5f;
    [Tooltip("Distancia del centro del golpe a la cámara")]
    public float reach = 1.1f;
    public float radius = 0.6f;

    [Header("Manos (animación provisional)")]
    public Transform leftHand;
    public Transform rightHand;
    public float punchDistance = 0.35f;
    public float returnSpeed = 20f;

    float nextPunchTime;
    bool useRight;
    Vector3 leftRest, rightRest;

    public override void Init(Transform aimTransform, LayerMask mask)
    {
        base.Init(aimTransform, mask);
        if (leftHand != null) leftRest = leftHand.localPosition;
        if (rightHand != null) rightRest = rightHand.localPosition;
    }

    public override void Unequip(bool silent = false)
    {
        base.Unequip(silent);
        if (leftHand != null) leftHand.localPosition = leftRest;
        if (rightHand != null) rightHand.localPosition = rightRest;
    }

    public override void Tick(bool attackPressed, bool attackHeld, bool attackReleased)
    {
        // Las manos vuelven a su sitio poco a poco
        float k = returnSpeed * Time.deltaTime;
        if (leftHand != null) leftHand.localPosition = Vector3.Lerp(leftHand.localPosition, leftRest, k);
        if (rightHand != null) rightHand.localPosition = Vector3.Lerp(rightHand.localPosition, rightRest, k);

        if (attackHeld && Time.time >= nextPunchTime)
        {
            nextPunchTime = Time.time + punchInterval;
            Punch();
        }
    }

    void Punch()
    {
        onAttack?.Invoke();

        Transform hand = useRight ? rightHand : leftHand;
        Vector3 rest = useRight ? rightRest : leftRest;
        if (hand != null) hand.localPosition = rest + Vector3.forward * punchDistance;
        useRight = !useRight;

        Vector3 center = aim.position + aim.forward * reach;
        Collider[] cols = Physics.OverlapSphere(center, radius, hitMask, QueryTriggerInteraction.Ignore);
        HitColliders(cols, AttackType.Basic, damage);
    }

    void OnDrawGizmosSelected()
    {
        Transform a = aim != null ? aim : (Camera.main != null ? Camera.main.transform : null);
        if (a == null) return;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
        Gizmos.DrawSphere(a.position + a.forward * reach, radius);
    }
}
