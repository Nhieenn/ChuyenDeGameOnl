using UnityEngine;

/// <summary>
/// Gắn script này vào child RPG-Character (NƠI CHỨA COMPONENT ANIMATOR)
/// Script này đóng vai trò như 'thùng rác' để bắt tất cả các Animation Events 
/// có sẵn trong pack ExplosiveLLC, giúp Unity không bị văng lỗi No Receiver.
/// </summary>
public class AnimationEventCatcher : MonoBehaviour
{
    // Bắt event đấm (tay chạm mục tiêu)
    public void Hit() { }

    // Bắt event bắn (vũ khí súng)
    public void Shoot() { }

    // Bắt event bước chân
    public void FootR() { }
    public void FootL() { }

    // Bắt event nhảy/chạm đất
    public void Land() { }
    
    // Một số event thông dụng khác của Mixamo / RPG pack
    public void WeaponSwitch() { }
}
