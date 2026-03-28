using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Menu tạo/vào phòng — đọc layout từ NewUXMLTemplate.uxml.
/// Gắn script này lên GameManager. UIDocument cần Source Asset = NewUXMLTemplate.uxml
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class NetworkMenu : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("References")]
    [Tooltip("Kéo Player Prefab (NetworkObject) vào đây")]
    public NetworkObject PlayerPrefab;

    private NetworkRunner _runner;
    private UIDocument _document;
    private TextField _roomInput;

    private void Start()
    {
        _document = GetComponent<UIDocument>();
        var root = _document.rootVisualElement;

        // Lấy các element theo name đã khai báo trong UXML
        _roomInput = root.Q<TextField>("room-input");

        var btnJoin = root.Q<Button>("btn-join");
        if (btnJoin != null)
            btnJoin.clicked += () => StartGame(GameMode.Shared, _roomInput?.value ?? "Room01");

        var btnQuit = root.Q<Button>("btn-quit");
        if (btnQuit != null)
            btnQuit.clicked += () => Application.Quit();
    }

    async void StartGame(GameMode mode, string roomName)
    {
        if (_runner != null) return;

        // Ẩn menu
        _document.rootVisualElement.style.display = DisplayStyle.None;

        var go = new GameObject("NetworkRunner");
        _runner = go.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var pool = go.AddComponent<ObjectPoolManager>();
        var sceneManager = go.AddComponent<NetworkSceneManagerDefault>();

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName.Trim(),
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = sceneManager,
            ObjectProvider = pool
        });

        if (!result.Ok)
        {
            Debug.LogError($"[NetworkMenu] Lỗi: {result.ShutdownReason}");
            _document.rootVisualElement.style.display = DisplayStyle.Flex;
            Destroy(go);
            _runner = null;
        }
    }

    // ===== INetworkRunnerCallbacks =====
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer && PlayerPrefab != null)
        {
            float rx = UnityEngine.Random.Range(-5f, 5f);
            float rz = UnityEngine.Random.Range(-5f, 5f);
            runner.Spawn(PlayerPrefab, new Vector3(rx, 0, rz), Quaternion.identity, player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        if (kb != null)
        {
            Vector3 move = Vector3.zero;
            if (kb.wKey.isPressed) move += Vector3.forward;
            if (kb.sKey.isPressed) move += Vector3.back;
            if (kb.aKey.isPressed) move += Vector3.left;
            if (kb.dKey.isPressed) move += Vector3.right;

            if (Camera.main != null)
            {
                Vector3 camF = Camera.main.transform.forward; camF.y = 0; camF.Normalize();
                Vector3 camR = Camera.main.transform.right;   camR.y = 0; camR.Normalize();
                
                if (move != Vector3.zero)
                {
                    data.direction = camF * move.z + camR * move.x;
                }
                
                // Thu thập hướng Camera để khoá Lưng nhân vật trên Server
                data.lookDirection = camF;
            }
            data.isJumpPressed = kb.spaceKey.isPressed;
            data.isSprintPressed = kb.shiftKey.isPressed;
            data.isDashPressed = kb.fKey.isPressed;
        }
        if (mouse != null)
        {
            data.isFirePressed = mouse.leftButton.isPressed;
            data.isBlockPressed = mouse.rightButton.isPressed;
        }

        input.Set(data);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
