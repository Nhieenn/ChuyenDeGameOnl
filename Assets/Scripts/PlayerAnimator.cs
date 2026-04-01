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
    private static readonly int Jumping    = Animator.StringToHash("IsJumping");
    private static readonly int Trigger    = Animator.StringToHash("Trigger");
    private static readonly int TriggerNum = Animator.StringToHash("TriggerNumber");
    private static readonly int HitHash    = Animator.StringToHash("Hit");
    private static readonly int HitDirHash = Animator.StringToHash("HitX");
    private static readonly int KnockHash  = Animator.StringToHash("Knockdown");
    private static readonly int GetUpHash  = Animator.StringToHash("GetUp");
    private static readonly int RollHash   = Animator.StringToHash("Roll");
    private static readonly int StunHash   = Animator.StringToHash("Stun");
    private static readonly int RagingHash = Animator.StringToHash("IsRaging");

    [Header("Weapon Models")]
    [SerializeField] private GameObject unarmedVisual; // Model tay không
    [SerializeField] private GameObject swordVisual;   // Model kiếm (2Hand-Sword)

    // State animation gửi qua network
    [Networked] private NetworkBool NetMoving  { get; set; }
    [Networked] private float       NetVelX    { get; set; }
    [Networked] private float       NetVelZ    { get; set; }
    [Networked] private NetworkBool NetJumping { get; set; }

    public override void Spawned()
    {
        // Khởi tạo trạng thái model ngay lập tức để không bị chồng mesh
        UpdateVisuals(false);
    }

    private void UpdateVisuals(bool isRaging)
    {
        if (unarmedVisual != null) unarmedVisual.SetActive(!isRaging);
        if (swordVisual != null) swordVisual.SetActive(isRaging);

        // Lấy Animator của model đang hiển thị
        _animator = isRaging 
            ? (swordVisual != null ? swordVisual.GetComponent<Animator>() : null)
            : (unarmedVisual != null ? unarmedVisual.GetComponent<Animator>() : null);

        // Nếu model con không có Animator, thử lấy ở root
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
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
        Rpc_TriggerAttack(attackNumber);
    }

    [Rpc(RpcSources.InputAuthority | RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_TriggerAttack(int attackNumber)
    {
        if (_animator == null) return;
        _animator.SetInteger(TriggerNum, attackNumber);
        _animator.SetTrigger(Trigger);
    }

    /// <summary>Gọi từ HealthSystem khi trúng đòn</summary>
    public void TriggerHit(float hitDirection)
    {
        Rpc_TriggerHit(hitDirection);
    }

    [Rpc(RpcSources.StateAuthority | RpcSources.InputAuthority, RpcTargets.All)]
    private void Rpc_TriggerHit(float hitDirection)
    {
        if (_animator != null)
        {
            _animator.SetFloat(HitDirHash, hitDirection);
            _animator.SetTrigger(HitHash);
        }
    }

    /// <summary>Hạ gục và Hồi sinh</summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_TriggerKnockdown()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(KnockHash);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_TriggerGetUp()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(GetUpHash);
        }
    }

    [Rpc(RpcSources.InputAuthority | RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_TriggerRoll()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(RollHash);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_TriggerStun()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(StunHash);
        }
    }

    public override void Render()
    {
        var rage = GetComponent<RageSystem>();
        if (rage != null)
        {
            bool isRaging = rage.IsRaging;
            UpdateVisuals(isRaging);

            if (_animator != null)
            {
                _animator.SetBool(RagingHash, isRaging);
                
                // Truyền các Parameter trạng thái liên tục
                _animator.SetFloat(VelocityX, NetVelX); 
                _animator.SetFloat(VelocityZ, NetVelZ);
                _animator.SetBool(Jumping, NetJumping);
            }
        }
    }
}
