using UnityEngine;
using Fusion;

// Cấu trúc dữ liệu gửi qua mạng (thêm biến nhảy)
public struct NetworkInputData : INetworkInput
{
    public Vector3 direction; // Vector hướng bấm phím WASD
    public Vector3 lookDirection; // Hướng Camera/Crosshair đang nhòm tới
    public NetworkBool isJumpPressed; // Dùng NetworkBool để tối ưu đồng bộ trong Fusion
    public NetworkBool isFirePressed; // Dùng để bắn đạn
    public NetworkBool isSprintPressed; // Phím Shift chạy nhanh
    public NetworkBool isDashPressed;   // Phím F lướt
    public NetworkBool isBlockPressed;  // Chuột Phải: Đỡ đòn
}

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    private CharacterController _controller;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -19.62f; // Để trọng lực cao hơn thực tế một chút để nhân vật rơi nhanh, tạo cảm giác mượt hơn

    [Header("Shield System")]
    [Networked] public NetworkBool IsBlocking { get; set; }
    private GameObject _shieldVisual;

    private Vector3 _velocity;
    
    // Quản lý lực đẩy (knockback) và lướt (dash)
    [Networked] private Vector3 _impactVelocity { get; set; }
    
    // Cooldown lướt
    [Networked] private TickTimer _dashCooldown { get; set; }

    public void ApplyKnockback(Vector3 force)
    {
        _impactVelocity = force;
    }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    public override void Spawned()
    {
        // Ẩn MeshRenderer của Capsule gốc để chỉ hiện model RPG-Character
        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        // --- Tự động tạo Lá Chắn Bảo Vệ (Bán trong suốt) bằng Code ---
        _shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _shieldVisual.transform.SetParent(transform);
        _shieldVisual.transform.localPosition = Vector3.up * 1f; // Ngay giữa người
        
        // Thay vì hình Cầu tròn xoe, ta bóp méo nó thành hình Trứng (Oval) để bọc kín đầu và chân!
        _shieldVisual.transform.localScale = new Vector3(1.6f, 2.5f, 1.6f); 
        
        // Cực kì quan trọng: Xóa Physics Collider để không kẹt vào đạn/nhân vật khác
        Collider shieldCol = _shieldVisual.GetComponent<Collider>();
        if (shieldCol != null) Destroy(shieldCol);

        // Hiện màu tím là do Project của bạn chà chay URP (Universal Render Pipeline)
        // chứ không dùng Standard Shader cũ, mình sẽ cập nhật lại code tải Shader đúng chuẩn URP:
        var renderer = _shieldVisual.GetComponent<MeshRenderer>();
        
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Standard"); // Fallback
        
        var mat = new Material(urpShader);
        
        // Tắt trộn bóng vật lý, bật Transparent (Bán trong suốt)
        mat.SetFloat("_Surface", 1.0f); // 1 = Transparent trong URP
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        
        // Gán màu (URP dùng _BaseColor thay vì _Color)
        Color shieldColor = new Color(0.1f, 0.5f, 1f, 0.35f);
        mat.SetColor("_BaseColor", shieldColor);
        mat.SetColor("_Color", shieldColor); // Fallback cho Standard
        
        renderer.material = mat;
        
        _shieldVisual.SetActive(false);
    }

    public override void Render()
    {
        // Hiển thị bóng khiên mượt mà trên các Clinet
        if (_shieldVisual != null)
        {
            _shieldVisual.SetActive(IsBlocking);
        }
    }


    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            var stamina = GetComponent<StaminaSystem>();
            bool isStunned = stamina != null && stamina.IsStunned;

            // NẾU BỊ CHOÁNG -> KHÔNG THỂ BẤM GÌ HẾT (Trừ rớt tự do do trọng lực)
            if (isStunned)
            {
                data.direction = Vector3.zero;
                data.isJumpPressed = false;
                data.isSprintPressed = false;
                data.isDashPressed = false;
                data.isBlockPressed = false;
            }

            // 0. Xử lý Đỡ đòn (Khiên rùa) - Phải cập nhật trước tốc độ di chuyển
            if (data.isBlockPressed && !isStunned)
            {
                if (stamina != null && stamina.DrainStaminaContinually(10f)) // Tốn 10 điểm Thể Lực/giây
                {
                    IsBlocking = true;
                    data.isSprintPressed = false; // Cấm chạy nhanh
                    data.isDashPressed = false;   // Cấm lướt
                }
                else
                {
                    IsBlocking = false; // Hết thể lực hoặc buông phím
                }
            }
            else
            {
                IsBlocking = false;
            }

            // 1. ÉP KHOÁ HƯỚNG MẶT THEO CROSSHAIR (CAMERA)
            if (data.lookDirection != Vector3.zero && !isStunned)
            {
                data.lookDirection.y = 0; // Chống ngửa cổ
                transform.forward = Vector3.Slerp(transform.forward, data.lookDirection, Runner.DeltaTime * 15f);
            }

            // 2. Chạy nhanh (Sprint) - CHỈ được phép khi đi tới trước!
            float currentSpeed = IsBlocking ? speed * 0.5f : speed; // Giảm 50% tốc độ nếu bê khiên
            bool isSprinting = false;
            if (data.isSprintPressed && data.direction != Vector3.zero && !IsBlocking)
            {
                // Kiểm tra xem mũi nhọn hướng chạy có đồng hướng với mặt nhân vật không (Góc < 60 độ)
                float dotProduct = Vector3.Dot(transform.forward, data.direction.normalized);
                if (dotProduct > 0.5f) // > 0.5 tương đương góc lệch từ 0 đến bự nhất 60 độ sang 2 bên
                {
                    if (stamina != null && stamina.DrainStaminaContinually(20f))
                    {
                        currentSpeed = speed * 1.5f; // Tốc tộ tăng x1.5
                        isSprinting = true;
                    }
                }
            }

            // Tính nháp trước Vector di chuyển Local (để phân biệt đang đi tới hay đi ngang)
            Vector3 localInputVel = transform.InverseTransformDirection(data.direction);
            bool isStrafing = Mathf.Abs(localInputVel.x) > 0.3f; // Kiểm tra có đang bấm A hoặc D trục ngang không

            // 3. Lướt (Dash / Roll) - CẤM khi đang ấn A/D hoặc đang bật Khiên
            if (data.isDashPressed && !isStrafing && !IsBlocking && _dashCooldown.ExpiredOrNotRunning(Runner))
            {
                if (stamina != null && stamina.ConsumeStamina(25f)) // Tốn 25 thể lực cho cú lướt
                {
                    _dashCooldown = TickTimer.CreateFromSeconds(Runner, 1.25f);
                    
                    // Luôn luôn lướt theo hướng mặt của mình (Crosshair)
                    Vector3 dashDir = transform.forward;
                    _impactVelocity = dashDir * 25f; // Bơm dồn lực

                    // Gửi lệnh Anim Roll
                    var anim = GetComponent<PlayerAnimator>();
                    if (anim != null) anim.Rpc_TriggerRoll();
                }
            }

            // 4. Di chuyển tịnh tiến theo WASD (World Space)
            data.direction.Normalize();
            Vector3 move = data.direction * currentSpeed;

            // 3. Xử lý trọng lực TRƯỚC — reset y khi chạm đất, sau đó mới cộng gravity
            if (_controller.isGrounded && _velocity.y < 0f)
                _velocity.y = -2f; // Giữ nhân vật bám sát đất, không để y về 0

            if (_controller.isGrounded && data.isJumpPressed)
            {
                if (stamina == null || stamina.ConsumeStamina(15f)) // Nhảy tốn 15 Thể lực
                {
                    _velocity.y = jumpForce;
                }
            }

            // Áp dụng trọng lực mỗi tick
            _velocity.y += gravity * Runner.DeltaTime;

            // Xử lý Lực đẩy lùi / Dash (Impact Decay)
            if (_impactVelocity.sqrMagnitude > 0.1f)
            {
                _impactVelocity = Vector3.Lerp(_impactVelocity, Vector3.zero, Runner.DeltaTime * 8f);
            }
            else
            {
                _impactVelocity = Vector3.zero;
            }

            // Tổng hợp và di chuyển
            _controller.Move((move + _velocity + _impactVelocity) * Runner.DeltaTime);

            // Tính toán Local Velocity cho Animator (Nhân lên nếu đang Sprint để Blend ra animation xa hơn)
            Vector3 localVel = transform.InverseTransformDirection(data.direction);
            if (isSprinting) localVel *= 1.5f; 

            GetComponent<PlayerAnimator>()?.SetMovement(
                localVel,
                data.direction != Vector3.zero,
                !_controller.isGrounded
            );
        }
    }
}