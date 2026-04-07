using Fusion;
using UnityEngine;

/// <summary>
/// Thành phần xử lý hiệu ứng hình ảnh (VFX) cho trạng thái Nộ.
/// Lắng nghe RageSystem để bật/tắt Particle System.
/// </summary>
public class RageVFXLauncher : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private RageSystem rageSystem;
    [SerializeField] private ParticleSystem rageParticles;
    
    // Lưu lại trạng thái trước đó để tránh gọi Play/Stop liên tục (Dirty flag pattern)
    private bool _wasRaging;

    public override void Spawned()
    {
        if (rageSystem == null) rageSystem = GetComponent<RageSystem>();
        
        // Khởi tạo trạng thái ban đầu
        if (rageParticles != null)
        {
            if (rageSystem.IsRaging) rageParticles.Play();
            else rageParticles.Stop();
        }
        _wasRaging = rageSystem.IsRaging;
    }

    public override void Render()
    {
        if (rageSystem == null || rageParticles == null) return;

        bool isRaging = rageSystem.IsRaging;

        // Chỉ xử lý khi có sự thay đổi trạng thái (Transition)
        if (isRaging != _wasRaging)
        {
            _wasRaging = isRaging;

            if (isRaging)
            {
                Debug.Log($"[RageVFX] BẬT hiệu ứng Nộ cho {gameObject.name}");
                rageParticles.Play(true);
            }
            else
            {
                Debug.Log($"[RageVFX] TẮT hiệu ứng Nộ cho {gameObject.name}");
                rageParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
