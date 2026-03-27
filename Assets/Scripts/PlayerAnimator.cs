using Fusion;
using UnityEngine;

/// <summary>
/// Đồng bộ animation qua mạng cho RPG Character.
/// Gắn lên root Player prefab.
/// </summary>
public class PlayerAnimator : NetworkBehaviour
{
    private Animator _animator;

    // Hash các parameter trong RPG-Character-Animation-Controller
    private static readonly int VelocityX  = Animator.StringToHash("Velocity X");
    private static readonly int VelocityZ  = Animator.StringToHash("Velocity Z");
    private static readonly int Moving     = Animator.StringToHash("Moving");
    private static readonly int Jumping    = Animator.StringToHash("Jumping");
    private static readonly int Trigger    = Animator.StringToHash("Trigger");
    private static readonly int TriggerNum = Animator.StringToHash("TriggerNumber");

    // Hash bổ sung
    private static readonly int Weapon         = Animator.StringToHash("Weapon");
    private static readonly int AnimationSpeed = Animator.StringToHash("AnimationSpeed");

    // State animation gửi qua network
    [Networked] private NetworkBool NetMoving  { get; set; }
    [Networked] private float       NetVelX    { get; set; }
    [Networked] private float       NetVelZ    { get; set; }
    [Networked] private NetworkBool NetJumping { get; set; }
    [Networked] private int         NetTrigger { get; set; }

    public override void Spawned()
    {
        _animator = GetComponentInChildren<Animator>();
        if (_animator == null)
            Debug.LogWarning("[PlayerAnimator] Không tìm thấy Animator trong children!");
        else
            Debug.Log("[PlayerAnimator] Tìm thấy Animator: " + _animator.gameObject.name);
    }

    /// <summary>Gọi từ PlayerMovement mỗi FixedUpdateNetwork</summary>
    public void SetMovement(Vector3 localVelocity, bool isMoving, bool isJumping)
    {
        if (!Object.HasInputAuthority) return;
        NetMoving  = isMoving;
        NetVelX    = localVelocity.x;
        NetVelZ    = localVelocity.z;
        NetJumping = isJumping;
    }

    /// <summary>Gọi từ MeleeAttack khi đánh</summary>
    public void TriggerAttack(int attackNumber)
    {
        if (!Object.HasInputAuthority) return;
        NetTrigger = attackNumber;
    }

    public override void Render()
    {
        if (_animator == null) return;

        // Chỉ truyền đúng 2 Parameter của Blend Tree Locomotion mới
        _animator.SetFloat(VelocityX, NetVelX); 
        _animator.SetFloat(VelocityZ, NetVelZ);

        // Truyền trigger tấn công nếu có
        if (NetTrigger > 0)
        {
            _animator.SetInteger(TriggerNum, NetTrigger);
            _animator.SetTrigger(Trigger);
            NetTrigger = 0;
        }
    }
}
