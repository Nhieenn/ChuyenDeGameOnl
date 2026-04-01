---
title: "Not 1v1 — Game Feel Document"
version: "0.1.0"
date: "2026-03-30"
author: "Antigravity + User"
status: "Draft"
---

# Game Feel Document

## 1. Triết lý Phản hồi (Feedback Philosophy)

**Quy tắc số 3 (Rule of Three):** Mỗi hành động quan trọng của người chơi phải tạo ra phản hồi ở ít nhất 3 kênh:
1. **Visual (Thị giác):** Particle, Chớp đỏ viền (Vignette), UI nảy.
2. **Audio (Thính giác):** Âm thanh va chạm, Tiếng kết liễu.
3. **Kinesthetic (Cảm giác):** Rung màn hình (Camera Shake), Dừng hình (Death Stop).

---

## 2. Ma trận Phản hồi (Master Feedback Matrix)

| Sự kiện (Event) | VFX (Visual) | Camera (Shake) | Hitstop (Kinesthetic) | Ghi chú |
|-----------------|--------------|----------------|------------------------|---------|
| **Trúng đòn (Hit)** | Tia lửa + Chớp đỏ viền | Rung nhẹ (Light) | Không (Dùng anim) | Để duy trì nhịp độ |
| **Bị hạ gục (Death)** | Nổ hạt lớn + Mờ dần | Rung mạnh (Heavy) | **Dừng 0.15s** (Final Blow) | Điểm nhấn uy lực |
| **Hồi sinh (Respawn)** | Hiệu ứng hào quang | Không | Không | |

---

## 3. Cấu hình Rung Camera (Camera Shake Profiles)

| Profile | Thời gian (Duration) | Cường độ (Magnitude) | Tần số (Frequency) | Mục đích |
|---------|---------------------|----------------------|--------------------|----------|
| **Light** | 0.1s | 0.05 | 15 | Khi bị trúng đòn thường |
| **Heavy** | 0.3s | 0.2 | 20 | Khi bị hạ gục vĩnh viễn |

---

## 4. Cấu hình Dừng hình (Death Stop)

| Profile | Thời gian khựng | Time Scale | Mục đích |
|---------|-----------------|------------|----------|
| **Final Blow** | 0.15s | 0.05 | Chốt hạ trận chiến kịch tính |

---

## 5. Hiệu ứng Viền Đỏ (Red Vignette)
- **Màu sắc:** Đỏ máu (#FF0000) với độ mờ (Alpha) khoảng 30%.
- **Hoạt ảnh:** Xuất hiện ngay lập tức (0.01s) và mờ dần (Fade out) trong 0.5s.
- **Kích hoạt:** Mỗi khi `OnHealthChanged` phát hiện máu bị trừ.

---

## 6. Ghi chú về Âm thanh (Audio)
(Sẽ cập nhật sau khi triển khai hệ thống Sound)
- Đòn đánh thường: Tiếng va chạm đanh.
- Đòn kết liễu: Tiếng vang chậm và nặng nề.
