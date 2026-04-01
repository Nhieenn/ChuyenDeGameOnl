using UnityEngine;

/// <summary>
/// Gắn script này vào child RPG-Character (NƠI CHỨA COMPONENT ANIMATOR)
/// Script này đóng vai trò như 'thùng rác' để bắt tất cả các Animation Events 
/// có sẵn trong pack ExplosiveLLC, giúp Unity không bị văng lỗi No Receiver.
/// </summary>
public class AnimationEventCatcher : MonoBehaviour
{
    // Bắt event đấm (tay chạm mục tiêu)
    // Bắt event đấm (tay chạm mục tiêu)
    public void Hit() 
    {
        // Script này nằm ở con (RPG-Character), ta cần gọi sang script ở Cha (Player Root)
        var melee = GetComponentInParent<MeleeAttack>();
        if (melee != null)
        {
            melee.ExecuteHitDetection();
        }
    }

    // Bắt event bước chân
    public void FootR() { }
    public void FootL() { }

    // Bắt event nhảy/chạm đất
    public void Land() { }
    
    // Một số event thông dụng khác của Mixamo / RPG pack
    public void WeaponSwitch() { }
}
