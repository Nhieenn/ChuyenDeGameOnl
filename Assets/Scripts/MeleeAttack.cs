using Fusion;
using UnityEngine;

/// <summary>
/// Cận chiến: Physics.Raycast + kích hoạt animation attack.
/// Gắn lên root Player prefab cùng với PlayerMovement.
/// </summary>
public class MeleeAttack : NetworkBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Tầm đánh (units)")]
    public float attackRange = 1.8f;
    [Tooltip("Cooldown giữa 2 đòn (giây)")]
    public float attackRate = 0.7f;
    [Tooltip("Layer bị ảnh hưởng (để trống = tất cả)")]
    public LayerMask hitLayers = ~0;

    [Header("VFX")]
    [Tooltip("Particle effect khi trúng (optional)")]
    public GameObject hitEffectPrefab;

    [Networked] private TickTimer _cooldown { get; set; }

    private PlayerAnimator _anim;

    // Dùng để xoay combo giữa các đòn tay
    private int _comboIndex = 0;
    // Unarmed attacks trong pack: 4=RightAttack1, 5=RightAttack2, 6=RightAttack3
    private static readonly int[] _comboTriggers = { 4, 5, 6 };

    public override void Spawned()
    {
        _anim = GetComponent<PlayerAnimator>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData data)) return;

        if (data.isFirePressed && _cooldown.ExpiredOrNotRunning(Runner))
        {
            _cooldown = TickTimer.CreateFromSeconds(Runner, attackRate);

            // Chỉ State Authority xử lý logic hit để tránh duplicate
            if (Object.HasStateAuthority)
            {
                PerformRaycastHit();
            }

            // Animation chạy trên client bản thân để cảm giác mượt
            int triggerNum = _comboTriggers[_comboIndex % _comboTriggers.Length];
            _comboIndex++;
            _anim?.TriggerAttack(triggerNum);
        }
    }

    private void PerformRaycastHit()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 dir    = transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, attackRange, hitLayers))
        {
            var netObj = hit.collider.GetComponent<NetworkObject>();

            // Bỏ qua nếu trúng chính mình
            if (netObj != null && netObj.InputAuthority == Object.InputAuthority) return;

            // Hiệu ứng trúng (spawn local — không cần network sync cho VFX)
            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(-dir));

            Debug.Log($"[MeleeAttack] Trúng: {hit.collider.name}");

            // Trừ máu (gọi sang HealthSystem của mục tiêu)
            var health = hit.collider.GetComponent<HealthSystem>();
            if (health != null)
            {
                // Mỗi cú đấm trừ 10 máu
                health.TakeDamage(10f, hit.point);
            }
        }
    }
}
