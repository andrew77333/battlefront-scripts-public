using UnityEngine;

//Что даёт этот код
//Движение работает через MoveSpeed (Blend Tree).
//Прыжок (пробел).
//Атака (левая кнопка мыши или F). Причём рандомно выбирается Attack или Attack2.
//Alert (Q), Victory (V), Death (K) — можно тестить вручную.
//Код безопасный: если параметра в Animator нет → ошибок не будет.

[RequireComponent(typeof(CharacterController)), RequireComponent(typeof(Animator))]
public class UnitController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2.2f;
    public float runSpeed = 4.5f;
    public float rotationSpeed = 720f; // deg/sec

    [Header("Jump/Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;

    Animator animator;
    CharacterController cc;
    Transform cam;

    bool runMode = true;   // Shift — тумблер бега/ходьбы
    float yVel;
    bool isDead = false;   // состояние смерти

    void Awake()
    {
        animator = GetComponent<Animator>();
        cc = GetComponent<CharacterController>();
        cam = Camera.main ? Camera.main.transform : null;
    }

    void Update()
    {
        if (isDead) return; // если умер — ничего не делать

        // --- Переключатель бега/ходьбы
        if (Input.GetKeyDown(KeyCode.LeftShift)) runMode = !runMode;

        // --- Ввод (WASD), камера-ориентированно
        //Vector3 input = Vector3.zero;
        //float h = Input.GetAxisRaw("Horizontal");
        //float v = Input.GetAxisRaw("Vertical");
        //if (cc.isGrounded) // ввод только если на земле
        //{
        //    //float h = Input.GetAxisRaw("Horizontal");
        //    //float v = Input.GetAxisRaw("Vertical");
        //    input = new Vector3(h, 0f, v);
        //    input = Vector3.ClampMagnitude(input, 1f);
        //}

        // --- Ввод (WASD), камера-ориентированно
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);

        Vector3 moveDir = input;
        if (cam != null)
        {
            Vector3 f = cam.forward; f.y = 0; f.Normalize();
            Vector3 r = cam.right; r.y = 0; r.Normalize();
            moveDir = (f * v + r * h).normalized;
        }

        float targetSpeed = (runMode ? runSpeed : walkSpeed) * input.magnitude;
        Vector3 horizontalVel = moveDir * targetSpeed;

        // --- Поворот к движению
        if (horizontalVel.sqrMagnitude > 0.0001f)
        {
            Quaternion to = Quaternion.LookRotation(horizontalVel);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, to, rotationSpeed * Time.deltaTime);
        }

        // --- Прыжок и гравитация
        if (cc.isGrounded)
        {
            yVel = -1f;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                yVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
                SetTriggerSafe("Jump");
            }
        }
        yVel += gravity * Time.deltaTime;

        // --- Движение контроллером
        Vector3 velocity = horizontalVel;
        velocity.y = yVel;
        cc.Move(velocity * Time.deltaTime);

        // --- Параметры аниматора
        float normalized = targetSpeed / runSpeed; // 0..1
        animator.SetFloat("MoveSpeed", normalized, 0.1f, Time.deltaTime);

        // --- Атаки (ЛКМ или F)
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F))
        {
            if (normalized > 0.6f && HasState("AttackJump")) SetTriggerSafe("AttackJump");
            else if (Random.value < 0.5f && HasState("Attack2")) SetTriggerSafe("Attack2");
            else SetTriggerSafe("Attack");
        }

        // --- Alert (R), Victory (V)
        if (Input.GetKeyDown(KeyCode.R)) SetTriggerSafe("Alert");
        if (Input.GetKeyDown(KeyCode.V)) SetTriggerSafe("Victory");

        // --- Death (K)
        if (Input.GetKeyDown(KeyCode.K) && !isDead)
        {
            isDead = true;
            SetTriggerSafe("Death");
        }
    }

    // --- Вспомогалки ---
    void SetTriggerSafe(string name)
    {
        if (HasParameter(name, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(name);
    }

    bool HasState(string stateName)
    {
        return animator.HasState(0, Animator.StringToHash(stateName));
    }

    bool HasParameter(string name, AnimatorControllerParameterType type)
    {
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }
}
