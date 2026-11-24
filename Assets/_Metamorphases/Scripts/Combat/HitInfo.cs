using UnityEngine;

// Tipos de ataque (algunos elementos solo ceden a un tipo concreto, p. ej. el interruptor rojo = Charged).
public enum AttackType
{
    Basic,    // corte, disparo o puñetazo normal
    Heavy,    // 3er corte del combo de espada
    Charged,  // ataques cargados (Realista: T023, T025, T028)
    Special
}

// Lista de tipos que acepta un elemento (se pueden marcar varios a la vez en el Inspector).
[System.Flags]
public enum AttackTypeMask
{
    Basic   = 1 << 0,
    Heavy   = 1 << 1,
    Charged = 1 << 2,
    Special = 1 << 3,
    All     = Basic | Heavy | Charged | Special
}

public static class AttackTypeExtensions
{
    public static AttackTypeMask ToMask(this AttackType type) => (AttackTypeMask)(1 << (int)type);
}

// "Paquete" de información que viaja con cada golpe.
public struct HitInfo
{
    public ColorMode mode;      // color del arma que golpea
    public AttackType type;
    public float damage;
    public Vector3 point;       // dónde ha golpeado
    public Vector3 direction;   // hacia dónde iba el golpe

    public HitInfo(ColorMode mode, AttackType type, float damage, Vector3 point, Vector3 direction)
    {
        this.mode = mode; this.type = type; this.damage = damage;
        this.point = point; this.direction = direction;
    }
}

// Cualquier cosa que pueda recibir golpes (elementos de color, dianas, enemigos...) implementa esto.
public interface IHittable
{
    void OnHit(HitInfo hit);
}
