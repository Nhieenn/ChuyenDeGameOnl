using UnityEngine;

/// <summary>
/// Xử lý hiệu ứng hình ảnh cho Ngọc: Bay bổng, xoay và nhấp nháy ánh sáng.
/// Giúp vật phẩm trông sinh động và "phép thuật" hơn.
/// </summary>
public class OrbVisuals : MonoBehaviour
{
    [Header("Floating Movement")]
    public float bobSpeed = 2f;
    public float bobAmount = 0.2f;

    [Header("Rotation")]
    public float rotateSpeed = 60f;

    [Header("Light Pulse")]
    public Light orbLight;
    public float minIntensity = 1f;
    public float maxIntensity = 3f;
    public float pulseSpeed = 1.5f;

    private Vector3 _startPos;

    void Start()
    {
        _startPos = transform.position;
        if (orbLight == null) orbLight = GetComponentInChildren<Light>();
    }

    void Update()
    {
        // 1. Hiệu ứng bay bổng (Bobbing)
        float newY = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // 2. Hiệu ứng xoay (Rotation)
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        // 3. Hiệu ứng nhấp nháy ánh sáng (Light Pulse)
        if (orbLight != null)
        {
            float lerp = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            orbLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, lerp);
        }
    }

    /// <summary>
    /// Reset vị trí bắt đầu khi Ngọc được tái sử dụng từ Object Pool.
    /// </summary>
    public void ResetStartPos()
    {
        _startPos = transform.position;
    }
}
