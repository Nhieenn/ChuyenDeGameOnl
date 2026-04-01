---
title: "Not 1v1 — Game Design Document"
version: "0.1.0"
date: "2026-03-30"
author: "Antigravity + User"
status: "Draft"
---

# Game Design Document: "Not 1v1"

## 1. Vision

### One-Sentence Pitch
Một đấu trường Arena 3D góc nhìn thứ 3, nơi người chơi bước vào những cuộc hỗn chiến nhiều người (Brawl) "cực cháy" để tìm ra kẻ mạnh nhất.

### Core Fantasy
Cảm giác của một "Chiến thần" giữa đám đông: Sự hỗn loạn, kỹ năng né tránh điêu luyện và những cú đấm đầy uy lực giữa chiến trường không hồi kết.

### Target Emotions
- **Hưng phấn (Adrenaline):** Nhịp độ cực nhanh, hiểm họa đến từ mọi phía.
- **Hỗn loạn vui nhộn:** Những tình huống bất ngờ khi "ngư ông đắc lợi" giữa các đám đông đang đánh nhau.
- **Tự hào:** Nhìn tên mình tỏa sáng trên Top 10 của server toàn cầu.

### Reference Games
| Game | Điểm muốn học hỏi |
|------|-------------------|
| **Sifu / Absolver** | Góc nhìn thứ 3 và cảm giác đòn đánh chân thực |
| **Brawl Stars (3D version)** | Tính chất hỗn chiến nhiều người và nhịp độ nhanh |
| **Naraka: Bladepoint** | Sự kịch tính của đấu trường multiplayer |

---

## 2. Core Loop (Vòng lặp chính)

```
[Chọn Phòng] → [Vào Trận] → [Đấm/Bắn đối thủ] → [Tích lũy Kills] → [End Game / Leaderboard]
```

### Bảng định nghĩa Loop

| Phase | System | Description |
|-------|--------|-------------|
| Input | PlayerMovement | Người chơi điều khiển bằng Keyboard/Mouse |
| Action | Combat System | Thực hiện MeleeAttack |
| Feedback | DamageFlash / HitEffect | Hiệu ứng hình ảnh khi trúng đòn |
| Reward | PlayFab Statistic | Điểm Kill được cộng vào Bảng xếp hạng Global |

---

## 3. Mechanics (Cơ chế đặc trưng)

### 3.1 Quy tắc Hạ gục & Hồi sinh
```gherkin
Feature: Chiến đấu & Hồi sinh (Photon Fusion)

  Scenario: Hạ gục đối thủ
    Given Đối thủ có Máu (HP) > 0
    When Player đấm/bắn làm HP đối thủ về 0
    Then Đối thủ biến mất (Die)
    And Player được cộng 1 Kill vào Statistic
    And Điểm số được gửi lên PlayFab Cloud

  Scenario: Hồi sinh
    Given Player đã bị hạ gục
    When Hết thời gian chờ Respawn
    Then Player xuất hiện lại tại vị trí xuất phát
    And HP được hồi đầy
```

### 3.2 Hệ thống Stamina (Thể lực)
```gherkin
Feature: Quản lý Thể lực

  Scenario: Thực hiện hành động tốn sức
    Given Player có Stamina đầy
    When Player thực hiện tấn công (Melee)
    Then Stamina bị trừ một lượng tương ứng
    And Nếu Stamina hết, không thể thực hiện đòn đánh tiếp theo
```

---

## 4. Entities & Balance Data (Thông số cân bằng)

### Player Stats (Dựa trên code hiện tại)

| Stat | Default | Min | Max | Notes |
|------|---------|-----|-----|-------|
| Health | 100 | 0 | 100 | Quản lý bởi HealthSystem |
| Move Speed | 5.0 | 0 | - | Quản lý bởi PlayerMovement |
| Match Time | 180s | 0 | - | Thời gian mỗi trận đấu |

---

## 5. Input Mapping (Điều khiển)

| Action | Keyboard/Mouse | Notes |
|--------|---------------|-------|
| Move | WASD | Điều khiển hướng |
| Attack | Chuột trái / Phím E | Tùy vào Weapon đang cầm |
| Jump | Space | Nhảy |

---

## 6. Backend Integration (Phần kĩ thuật đặc biệt)
- **Networking:** Photon Fusion (Shared Mode) - Đảm bảo đồng bộ mượt mà 1-vs-1.
- **Data Persistence:** PlayFab - Lưu trữ Top Killer vĩnh viễn trên mây.

---

## 8. Open Design Questions
1. Có nên thêm hệ thống **Shop đồ** (Mua Skin, nâng cấp vũ khí) không?
2. Có cần hệ thống **Rank** (Đồng, Bạc, Vàng) dựa trên số Kill không?
3. Bản đồ Arena hiện tại có cần thêm các **Bẫy môi trường** (Environment Hazards) không?
