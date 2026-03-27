using Fusion;
using UnityEngine;

public class PlayerWeapon : NetworkBehaviour
{
    [Tooltip("Kéo Bullet.prefab vào đây")]
    public NetworkObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.2f;

    [Networked] private TickTimer delay { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            if (data.isFirePressed && delay.ExpiredOrNotRunning(Runner))
            {
                delay = TickTimer.CreateFromSeconds(Runner, fireRate);

                if (bulletPrefab == null) return;

                Vector3 spawnPos = firePoint != null
                    ? firePoint.position
                    : transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;

                Runner.Spawn(bulletPrefab, spawnPos, Quaternion.LookRotation(transform.forward), Object.InputAuthority);
            }
        }
    }
}

