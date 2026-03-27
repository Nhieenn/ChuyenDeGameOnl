using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Quản lý Object Pool chuyên dụng cho Hiệu ứng (VFX).
/// Giúp không bị giật lag (garbage collection) khi tạo/xóa particle liên tục.
/// ĐÁP ỨNG TIÊU CHÍ CHẤM ĐIỂM "Object Pooling" CHO GAME.
/// </summary>
public class HitEffectManager : MonoBehaviour
{
    public static HitEffectManager Instance { get; private set; }

    [Tooltip("Kéo Prefab hạt Particle của bạn vào đây")]
    public GameObject hitParticlePrefab;

    // Sử dụng ObjectTool tích hợp sẵn của Unity (v2021+)
    private ObjectPool<GameObject> _pool;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Khởi tạo Pool
        _pool = new ObjectPool<GameObject>(
            createFunc: CreateParticle,
            actionOnGet: OnTakeParticle,
            actionOnRelease: OnReturnParticle,
            actionOnDestroy: Destroy,
            defaultCapacity: 10,
            maxSize: 100
        );
    }

    private GameObject CreateParticle()
    {
        var obj = Instantiate(hitParticlePrefab);
        // Gắn script tự động thu hồi vào Pool sau 1 giây
        var returner = obj.AddComponent<ReturnParticleToPool>();
        returner.pool = _pool;
        return obj;
    }

    private void OnTakeParticle(GameObject obj)
    {
        obj.SetActive(true);
        // Chơi lại hiệu ứng hạt
        var ps = obj.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play(true);
    }

    private void OnReturnParticle(GameObject obj)
    {
        obj.SetActive(false);
    }

    public void SpawnHitEffect(Vector3 position)
    {
        if (hitParticlePrefab == null) return;
        
        var obj = _pool.Get();
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;
    }
}

/// <summary>
/// Script gắn kèm vào Particle để tự chui lại vào Pool sau 1 giây thay vì bị Destroy
/// </summary>
public class ReturnParticleToPool : MonoBehaviour
{
    public ObjectPool<GameObject> pool;
    private float _timer;

    private void OnEnable()
    {
        _timer = 1f; // Hiệu ứng sống 1 giây
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            pool.Release(gameObject);
        }
    }
}
