using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Camera 3rd Person controller.
/// Gắn vào Main Camera. Cursor chỉ bị khóa SAU KHI spawn player.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0, 2.5f, -5f);

    [Header("Orbit Settings")]
    public float mouseSensitivity = 3f;
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 55f;
    public float smoothSpeed = 12f;

    private float _yaw;
    private float _pitch;
    private Transform _target;
    private bool _active = false;

    private void Start()
    {
        // Tìm player định kỳ (0.5s/lần) thay vì mỗi frame — tối ưu performance
        InvokeRepeating(nameof(TryFindPlayer), 0.2f, 0.5f);
    }

    private void TryFindPlayer()
    {
        if (_target != null) { CancelInvoke(nameof(TryFindPlayer)); return; }
        
        // Cực kì quan trọng trong Game Mạng (Multiplayer):
        // Không được dùng GameObject.FindWithTag vì nó sẽ bắt nhầm nhân vật của Host.
        // Phải tìm đúng Player thuộc quyền điều khiển của máy này (HasInputAuthority).
        var players = FindObjectsByType<Fusion.NetworkObject>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.CompareTag("Player") && p.HasInputAuthority)
            {
                _target = p.transform;
                LockCursor(true);
                CancelInvoke(nameof(TryFindPlayer));
                break;
            }
        }
    }

    private void Update()
    {
        if (_target == null) return;

        // ESC để mở/khóa chuột
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            LockCursor(!_active);
        }

        if (!_active) return;

        // Đọc mouse delta từ New Input System
        var mouse = Mouse.current;
        if (mouse != null)
        {
            var delta = mouse.delta.ReadValue();
            _yaw   += delta.x * mouseSensitivity * Time.deltaTime * 10f;
            _pitch -= delta.y * mouseSensitivity * Time.deltaTime * 10f;
            _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);
        }
    }

    private Vector3 _shakeOffset;
    private Coroutine _shakeCoroutine;

    private void LateUpdate()
    {
        if (_target == null || !_active) return;

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 desiredPos = _target.position + rotation * offset;

        // Nội suy vị trí Camera mượt mà
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * smoothSpeed) + _shakeOffset;
        
        // Luôn nhìn vào phía trên đầu nhân vật một chút
        transform.LookAt(_target.position + Vector3.up * 1.2f);
    }

    /// <summary>
    /// Kích hoạt rung màn hình theo thời gian và cường độ chỉ định.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
    }

    private System.Collections.IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Tạo độ lệch ngẫu nhiên trong vòng tròn đơn vị
            Vector2 randomPoint = Random.insideUnitCircle * magnitude;
            _shakeOffset = new Vector3(randomPoint.x, randomPoint.y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _shakeOffset = Vector3.zero;
        _shakeCoroutine = null;
    }

    private void LockCursor(bool locked)
    {
        _active = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void OnDestroy()
    {
        LockCursor(false);
    }
}
