using Fusion;
using UnityEngine;

/// <summary>
/// Đạn bắn ra. Dùng Raycast (Lag Compensation) thay vì OnTriggerEnter
/// để đảm bảo va chạm chính xác trong Fusion Shared Mode.
/// </summary>
public class Bullet : NetworkBehaviour
{
    [Tooltip("Tốc độ bay của đạn (units/giây)")]
    public float speed = 25f;
    [Tooltip("Thời gian tồn tại tối đa (giây)")]
    public float lifeTime = 3f;

    [Networked] private TickTimer _lifeTimer { get; set; }

    // Lưu lại vị trí frame trước để raycast theo đoạn di chuyển
    private Vector3 _prevPosition;

    public override void Spawned()
    {
        _lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        _prevPosition = transform.position;
    }

    public override void FixedUpdateNetwork()
    {
        if (_lifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        // Di chuyển đạn
        Vector3 movement = transform.forward * speed * Runner.DeltaTime;
        transform.position += movement;

        // === Hit Detection bằng Physics.Raycast (dùng cho Shared Mode) ===
        // Trong Shared Mode, tất cả client simulate giống nhau nên Physics.Raycast là đúng và đủ.
        // LagCompensation chỉ dùng cho Server/Host Mode.
        if (Object.HasStateAuthority)
        {
            if (Physics.Raycast(_prevPosition, transform.forward, out RaycastHit hit, movement.magnitude))
            {
                var netObj = hit.collider.GetComponent<NetworkObject>();

                // Bỏ qua nếu trúng chính người bắn
                if (netObj != null && netObj.InputAuthority == Object.InputAuthority)
                {
                    _prevPosition = transform.position;
                    return;
                }

                // TODO: gọi health system khi có
                // hit.collider.GetComponent<HealthSystem>()?.TakeDamage(damage);

                Runner.Despawn(Object);
                return;
            }
        }

        _prevPosition = transform.position;
    }
}
