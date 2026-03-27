using UnityEngine;
using Fusion;

// Cấu trúc dữ liệu gửi qua mạng (thêm biến nhảy)
public struct NetworkInputData : INetworkInput
{
    public Vector3 direction;
    public NetworkBool isJumpPressed; // Dùng NetworkBool để tối ưu đồng bộ trong Fusion
    public NetworkBool isFirePressed; // Dùng để bắn đạn
}

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    private CharacterController _controller;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -19.62f; // Để trọng lực cao hơn thực tế một chút để nhân vật rơi nhanh, tạo cảm giác mượt hơn

    private Vector3 _velocity;

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
    }


    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            // 1. Di chuyển trên mặt phẳng XZ
            data.direction.Normalize();
            Vector3 move = data.direction * speed;

            // Xoay nhân vật theo hướng di chuyển
            if (data.direction != Vector3.zero)
                transform.forward = Vector3.Slerp(transform.forward, data.direction, Runner.DeltaTime * 10f);

            // 2. Xử lý trọng lực TRƯỚC — reset y khi chạm đất, sau đó mới cộng gravity
            if (_controller.isGrounded && _velocity.y < 0f)
                _velocity.y = -2f; // Giữ nhân vật bám sát đất, không để y về 0

            if (_controller.isGrounded && data.isJumpPressed)
                _velocity.y = jumpForce;

            // Áp dụng trọng lực mỗi tick (chỉ tích lũy khi không chạm đất)
            _velocity.y += gravity * Runner.DeltaTime;

            // 3. Tổng hợp và di chuyển
            _controller.Move((move + _velocity) * Runner.DeltaTime);

            // 4. Sync animation state (chuẩn hóa velocity về [-1, 1] để phù hợp với BlendTree)
            Vector3 localVel = transform.InverseTransformDirection(move);
            if (move.magnitude > 0) localVel.Normalize(); // Normalize để Velocity X/Z nằm trong khoảng -1 đến 1

            GetComponent<PlayerAnimator>()?.SetMovement(
                localVel,
                data.direction != Vector3.zero,
                !_controller.isGrounded
            );
        }
    }
}