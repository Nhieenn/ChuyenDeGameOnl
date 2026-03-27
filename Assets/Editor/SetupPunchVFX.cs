#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Script tự tạo hạt Particle System Action siêu đẹp chỉ bằng 1 nút bấm!
/// Thuộc về quy trình Automation của AntiGravity.
/// </summary>
public class SetupPunchVFX
{
    [MenuItem("AntiGravity/Tạo Hiệu Ứng Đấm Xịn (VFX)")]
    public static void CreateVFX()
    {
        // 1. Tạo cục trống
        var go = new GameObject("Hit_Punch_Sparks");
        var ps = go.AddComponent<ParticleSystem>();
        
        // 2. Chỉnh Main Settings (Tia lửa ngắn, văng nhanh, vụt tắt)
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new Color(1f, 0.8f, 0.2f, 1f); // Tia lửa vàng cam
        main.gravityModifier = 2f; // Rơi xuống nhanh
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // 3. Chỉnh Burst (Phụt 1 phát 30 hạt đạn ra xung quanh)
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
        
        // 4. Định hình khối nổ (Sphere)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // 5. Cầu vồng / Phai màu mờ dần
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.3f, 0f), 1f) }, // Trắng -> Cam nhạt
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) } // Rõ -> Mờ nín
        );
        col.color = grad;
        
        // 6. Kéo dài hạt ra tạo vết xước (Stretch Render)
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 3f; // Kéo dãn hạt ra gấp 3 lần để ra "Tia lửa"
        renderer.cameraVelocityScale = 0f;
        renderer.velocityScale = 0.1f;

        // Fix: URP hiển thị màu tím nếu không có Material chuẩn
        if (!System.IO.Directory.Exists("Assets/Prefabs/Materials"))
            System.IO.Directory.CreateDirectory("Assets/Prefabs/Materials");
            
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit"); // Fallback
        
        Material particleMat = new Material(shader);
        particleMat.SetColor("_BaseColor", Color.white);
        
        string matPath = "Assets/Prefabs/Materials/PunchParticleMat.mat";
        AssetDatabase.CreateAsset(particleMat, matPath);
        renderer.material = particleMat;
        
        // 7. Lưu thành Prefab tự động
        if (!System.IO.Directory.Exists("Assets/Prefabs")) System.IO.Directory.CreateDirectory("Assets/Prefabs");
        string path = "Assets/Prefabs/PunchVFX.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        
        // Xóa rác ngoài mản hình
        Object.DestroyImmediate(go);
        
        // Báo kết quả
        Debug.Log($"[AntiGravity AI] ✅ Đã rèn xong 1 thanh gươm Vàng ở {path} ! Kéo thả ngay vào HitEffectManager để chiêm ngưỡng!");
    }
}
#endif
