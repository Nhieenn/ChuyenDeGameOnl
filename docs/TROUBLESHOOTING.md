# Not 1v1 — Troubleshooting Guide

Tài liệu này lưu lại các lỗi kỹ thuật (Bugs) và giải pháp xử lý trong quá trình phát triển dự án.

---

## 1. Lỗi Đồng bộ UI (Missing Status Bars)

### Triệu chứng (Symptoms)
- Hai người chơi vào game (1 Editor, 1 Build EXE).
- Máy Host/Server nhìn thấy đủ các thanh Máu, Thể lực, Nộ.
- Máy Client vào sau **không nhìn thấy thanh trạng thái của chính mình**, nhưng lại nhìn thấy của đối thủ.
- Debug log UI báo số lượng thanh (Bars Count) bằng 0.

### Nguyên nhân (Root Cause)
- **Vòng đời khởi tạo (Initialization Timing)**: Trong Photon Fusion, lệnh `Spawned()` chạy ngay khi Object được tạo ra. Tuy nhiên, ở phía Client, `UIDocument` (UI Toolkit) có thể chưa nạp xong Root Element hoặc `FloatingUIManager` chưa sẵn sàng hoàn toàn.
- **Callback OnChanged**: Nếu giá trị (`CurrentHealth`, `CurrentStamina`, `CurrentRage`) không thay đổi (vẫn là giá trị mặc định lúc mới vào game), các hàm `OnChanged` sẽ không được kích hoạt để vẽ UI.

### Giải pháp (Fix)
Sử dụng vòng lặp **`Render()`** (hàm đồ họa của Fusion, chạy liên tục trên từng frame máy khách) để kiểm tra chủ động:

```csharp
public override void Render()
{
    // Kiểm tra mỗi frame đồ họa xem UI Manager đã có thanh của mình chưa
    if (FloatingUIManager.Instance != null && !FloatingUIManager.Instance.HasRegisteredStatus(this))
    {
        // Nếu chưa có, yêu cầu vẽ ngay lập tức
        FloatingUIManager.Instance.UpdateUI(this, values...);
    }
}
```

*Áp dụng tại: HealthSystem.cs, StaminaSystem.cs, RageSystem.cs.*

---

## 2. Lỗi Model/Animation bị tím (URP Shader)

### Triệu chứng
- Các Object được tạo bằng Code (ví dụ: Khiên bảo vệ) bị hiện màu Hồng/Tím (Magenta).

### Nguyên nhân
- Dự án sử dụng **URP (Universal Render Pipeline)** nhưng Shader được tìm kiếm bằng `Shader.Find("Standard")`. Standard Shader không tương thích với URP.

### Giải pháp
Sử dụng chuẩn Shader của URP: `Universal Render Pipeline/Lit` và cập nhật các thuộc tính màu sắc (`_BaseColor` thay vì `_Color`).

---

## 3. Lỗi Hoạt ảnh bị khựng/giật (Animator Stuttering)

### Triệu chứng
- Nhân vật bị giật frame đầu (nháy) khi bắt đầu thực hiện đòn tấn công.
- Khi nhấn nút đánh liên tục, hoạt ảnh không chơi mượt mà (Combo không ra đủ) mà cứ lặp đi lặp lại những frame đầu tiên.

### Nguyên nhân
- **Can Transition To Self**: Trong Animator, các đường nối từ **Any State** đến các đòn tấn công nếu để tích ô này sẽ cho phép trạng thái nhảy vào chính nó.
- Khi Trigger `Attack` được gửi liên tục, Animator sẽ khởi động lại (Reset) animation về Frame 0 ở mỗi lần trigger, gây ra hiện tượng giật máy.

### Giải pháp
1. Chọn Transition nối từ **Any State** đến trạng thái bị khựng.
2. **Bỏ tích** ô `Can Transition To Self`. Điều này bắt buộc Animator phải chơi hết hoạt ảnh hiện tại hoặc chờ một trạng thái khác can thiệp.
3. Giảm **Transition Duration** xuống mức thấp (0.05 - 0.1) để đòn đánh thoát ra nhanh và nhạy hơn.

---

*(Tiếp tục cập nhật khi phát hiện lỗi mới...)*
