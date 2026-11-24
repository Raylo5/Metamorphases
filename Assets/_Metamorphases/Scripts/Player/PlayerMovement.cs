using UnityEngine;
using UnityEngine.Events;

// T004 + T006 + T012 · Movimiento en primera persona.
// Basado en el CharacterMover del profesor (Rigidbody + GroundDetector), con cambios:
//  - La velocidad, el salto y los saltos extra salen del modo activo (ModeData).
//  - Doble salto (cuando el modo tiene extraJumps > 0: Guantes).
//  - Control en el aire y cuerpo siempre mirando hacia donde mira la cámara.
//  - Bloquea el ratón en el centro de la pantalla.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GroundDetector))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La Main Camera (la que controla Cinemachine)")]
    public Camera cam;

    [Header("Sensación al andar")]
    public float movementAcceleration = 8f;
    public float movementDeceleration = 12f;
    [Range(0, 1)] public float speedMovementSideways = 0.85f;
    [Range(0, 1)] public float speedMovementBackwards = 0.7f;

    [Header("Valores si no hay ModeManager en la escena")]
    public float fallbackSpeed = 6f;
    public float fallbackJump = 5f;

    [Header("Ratón")]
    public bool lockCursor = true;

    [Header("Eventos (sonidos, partículas...)")]
    public UnityEvent onJump;
    public UnityEvent onExtraJump;   // doble salto (sonido distinto, SDD)
    public UnityEvent onLanded;

    // Lectura para otros scripts (pasos, animaciones, HUD...)
    public bool IsGrounded => gd != null && gd.grounded && graceTimer <= 0f;
    public Vector3 HorizontalVelocity { get; private set; }

    Rigidbody rb;
    GroundDetector gd;
    Vector3 currentMov;      // dirección + intensidad suavizadas (0..1)
    bool jumpRequested;
    int extraJumpsLeft;
    float graceTimer;        // tras saltar, ignora el suelo un instante
    Vector3 lastPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gd = GetComponent<GroundDetector>();
        if (cam == null) cam = Camera.main;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        gd.groundedDown.AddListener(OnLanded);
        gd.groundedUp.AddListener(OnLeftGround);
        lastPos = rb.position;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        // El input se lee en Update (cada frame) y se usa en FixedUpdate (física).
        if (GameInput.Actions.Player.Jump.WasPressedThisFrame())
            jumpRequested = true;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (graceTimer > 0f) graceTimer -= dt;

        Vector3 delta = (rb.position - lastPos) / dt;
        HorizontalVelocity = new Vector3(delta.x, 0f, delta.z);
        lastPos = rb.position;

        Vector3 wish = GetWishDirection();
        Quaternion bodyRot = GetBodyRotation();

        HandleJump(wish);

        if (IsGrounded)
            GroundMove(wish, bodyRot, dt);
        else
            AirMove(wish, bodyRot, dt);
    }

    // ---------- Pasos ----------

    // Hacia dónde quiere ir el jugador según WASD y la cámara (largo 0..1).
    Vector3 GetWishDirection()
    {
        Vector2 input = GameInput.Actions.Player.Move.ReadValue<Vector2>();
        Vector3 local = new Vector3(input.x * speedMovementSideways, 0f, input.y);
        if (local.z < 0f) local.z *= speedMovementBackwards;

        float magnitude = Mathf.Clamp01(local.magnitude);
        if (magnitude <= 0f) return Vector3.zero;

        Vector3 forward = cam.transform.forward; forward.y = 0f; forward.Normalize();
        Vector3 right = cam.transform.right; right.y = 0f; right.Normalize();
        Vector3 world = forward * local.z + right * local.x;
        return world.normalized * magnitude;
    }

    Quaternion GetBodyRotation()
    {
        Vector3 forward = cam.transform.forward; forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return rb.rotation;
        return Quaternion.LookRotation(forward.normalized);
    }

    void HandleJump(Vector3 wish)
    {
        if (!jumpRequested) return;
        jumpRequested = false;

        ModeData data = CurrentData();
        if (IsGrounded)
        {
            Jump(currentMov * Speed(), data != null ? data.jumpForce : fallbackJump);
            extraJumpsLeft = data != null ? data.extraJumps : 0;
            onJump?.Invoke();
        }
        else if (extraJumpsLeft > 0)
        {
            extraJumpsLeft--;
            // El doble salto permite cambiar de dirección
            Vector3 horizontal = wish.sqrMagnitude > 0.01f ? wish * Speed() : HorizontalVelocity;
            Jump(horizontal, data != null ? data.extraJumpForce : fallbackJump);
            onExtraJump?.Invoke();
        }
    }

    void Jump(Vector3 horizontalVelocity, float force)
    {
        rb.linearVelocity = new Vector3(horizontalVelocity.x, force, horizontalVelocity.z);
        graceTimer = 0.15f;
    }

    void GroundMove(Vector3 wish, Quaternion bodyRot, float dt)
    {
        float lerp = wish.magnitude > currentMov.magnitude ? movementAcceleration : movementDeceleration;
        currentMov = Vector3.Lerp(currentMov, wish, lerp * dt);
        // En el suelo manda rb.Move: quitamos la inercia horizontal que venga del aire
        // para no patinar al aterrizar (se conserva la vertical = gravedad).
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.Move(rb.position + currentMov * Speed() * dt, bodyRot);
    }

    void AirMove(Vector3 wish, Quaternion bodyRot, float dt)
    {
        ModeData data = CurrentData();
        float airControl = data != null ? data.airControl : 0.3f;

        Vector3 v = rb.linearVelocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        if (wish.sqrMagnitude > 0.01f)
        {
            // Empuja suavemente la velocidad hacia donde apunta el jugador
            Vector3 target = wish * Speed();
            horizontal = Vector3.MoveTowards(horizontal, target, airControl * Speed() * 4f * dt);
        }
        rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
        rb.MoveRotation(bodyRot);
        currentMov = horizontal / Mathf.Max(Speed(), 0.01f); // para aterrizar sin frenazo
    }

    // Si se cae de un borde SIN saltar, conserva su inercia y sus saltos extra
    void OnLeftGround()
    {
        if (graceTimer > 0f) return; // ha sido un salto: ya está gestionado
        Vector3 h = currentMov * Speed();
        rb.linearVelocity = new Vector3(h.x, rb.linearVelocity.y, h.z);
        ModeData data = CurrentData();
        extraJumpsLeft = data != null ? data.extraJumps : 0;
    }

    void OnLanded()
    {
        extraJumpsLeft = 0;
        onLanded?.Invoke();
    }

    // ---------- Datos del modo ----------

    ModeData CurrentData() => ModeManager.Instance != null ? ModeManager.Instance.CurrentData : null;

    float Speed()
    {
        ModeData data = CurrentData();
        return data != null ? data.moveSpeed : fallbackSpeed;
    }
}
