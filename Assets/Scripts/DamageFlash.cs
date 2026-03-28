using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý hiệu ứng chớp đỏ (Flash) trên toàn bộ Mesh của nhân vật khi bị đánh trúng.
/// </summary>
public class DamageFlash : MonoBehaviour
{
    [Tooltip("Màu chớp nháy (Thường là màu Trắng hoặc Đỏ)")]
    public Color flashColor = Color.red;

    [Tooltip("Thời gian chớp (giây)")]
    public float flashDuration = 0.15f;

    private Renderer[] _renderers;
    private Color[] _originalColors;

    private Coroutine _flashCoroutine;

    private void Awake()
    {
        // Lấy toàn bộ Renderer của cả nhân vật và các vật dụng cầm tay
        _renderers = GetComponentsInChildren<Renderer>();
        _originalColors = new Color[_renderers.Length];

        // Lưu lại màu nguyên bản gốc để trả về sau khi chớp
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i].material.HasProperty("_BaseColor"))
            {
                _originalColors[i] = _renderers[i].material.GetColor("_BaseColor");
            }
            else if (_renderers[i].material.HasProperty("_Color"))
            {
                _originalColors[i] = _renderers[i].material.color;
            }
            else
            {
                _originalColors[i] = Color.white;
            }
        }
    }

    /// <summary>
    /// Kích hoạt hiệu ứng chớp. (Nên gọi từ HealthSystem khi máu bị trừ)
    /// </summary>
    public void Flash()
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }
        _flashCoroutine = StartCoroutine(DoFlash());
    }

    private IEnumerator DoFlash()
    {
        // 1. Chuyển tất cả sang màu Đỏ
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                if (_renderers[i].material.HasProperty("_BaseColor"))
                    _renderers[i].material.SetColor("_BaseColor", flashColor);
                else if (_renderers[i].material.HasProperty("_Color"))
                    _renderers[i].material.color = flashColor;
            }
        }

        // 2. Chờ 0.15s
        yield return new WaitForSeconds(flashDuration);

        // 3. Trả lại màu gốc
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                if (_renderers[i].material.HasProperty("_BaseColor"))
                    _renderers[i].material.SetColor("_BaseColor", _originalColors[i]);
                else if (_renderers[i].material.HasProperty("_Color"))
                    _renderers[i].material.color = _originalColors[i];
            }
        }

        _flashCoroutine = null;
    }
}
