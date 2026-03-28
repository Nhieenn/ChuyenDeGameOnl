using UnityEngine;
using TMPro;
using UnityEngine.Pool;

/// <summary>
/// Gắn vào Prefab DamageText để tự động bay lên, mờ dần và chui lại vào Pool
/// </summary>
public class FloatingTextAnim : MonoBehaviour
{
    public ObjectPool<GameObject> pool;
    private TextMeshPro _tmp;
    private float _timer;
    private float _speed = 2.5f;
    private Color _originColor;

    private void Awake()
    {
        _tmp = GetComponent<TextMeshPro>();
        if (_tmp != null) _originColor = _tmp.color;
    }

    private void OnEnable()
    {
        _timer = 1f; // Sống 1 giây
        if (_tmp != null) _tmp.color = _originColor; // Khôi phục độ rõ
    }

    private void Update()
    {
        if (_tmp == null) return;
        
        // 1. Bay lên
        transform.position += Vector3.up * _speed * Time.deltaTime;
        
        // 2. Mờ dần
        _timer -= Time.deltaTime;
        float alpha = Mathf.Clamp01(_timer);
        _tmp.color = new Color(_originColor.r, _originColor.g, _originColor.b, alpha);
        
        // 3. Hướng mặt về Camera (Billboard)
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }

        // 4. Hết giờ thì thu hồi
        if (_timer <= 0f)
        {
            if (pool != null) pool.Release(gameObject);
            else Destroy(gameObject);
        }
    }
    
    public void Setup(string text, Color color)
    {
        if (_tmp == null) _tmp = GetComponent<TextMeshPro>();
        _tmp.text = text;
        _tmp.color = color;
        _originColor = color;
    }
}
