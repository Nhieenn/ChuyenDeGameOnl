using Fusion;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Quản lý Hệ thống Chat Mạng dùng Fusion RPC.
/// Hỗ trợ Chat Kênh Chung và Chat Mật (/w).
/// </summary>
public class ChatManager : NetworkBehaviour
{
    public static ChatManager Instance { get; private set; }

    [Header("UI Settings")]
    public UIDocument uiDocument;
    
    private VisualElement _root;
    private ScrollView _chatLog;
    private TextField _chatInput;
    private VisualElement _chatContainer;

    private bool _isChatting = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void Spawned()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        _root = uiDocument.rootVisualElement;

        SetupUI();
    }

    private void SetupUI()
    {
        // 1. Tạo Container ở góc dưới bên trái
        _chatContainer = new VisualElement();
        _chatContainer.style.position = Position.Absolute;
        _chatContainer.style.bottom = 20;
        _chatContainer.style.left = 20;
        _chatContainer.style.width = 400;
        _chatContainer.style.height = 250;
        _chatContainer.style.backgroundColor = new Color(0, 0, 0, 0.4f);
        _chatContainer.style.borderTopLeftRadius = 5;
        _chatContainer.style.borderTopRightRadius = 5;
        _chatContainer.pickingMode = PickingMode.Ignore;
        _root.Add(_chatContainer);

        // 2. ScrollView để chứa log tin nhắn
        _chatLog = new ScrollView(ScrollViewMode.Vertical);
        _chatLog.style.flexGrow = 1;
        _chatLog.style.paddingLeft = 5;
        _chatLog.style.paddingRight = 5;
        _chatLog.pickingMode = PickingMode.Ignore;
        _chatContainer.Add(_chatLog);

        // 3. Ô nhập liệu (ẩn đi khi không dùng)
        _chatInput = new TextField();
        
        // Fix: Trong UI Toolkit, phải gọi trực tiếp class bên trong (unity-text-field__input) để đổi màu nền và chữ
        var textInput = _chatInput.Q(className: "unity-text-field__input");
        if (textInput != null)
        {
            textInput.style.backgroundColor = new Color(0, 0, 0, 0.8f); // Nền đen mờ
            textInput.style.color = Color.white; // Chữ trắng
            textInput.style.fontSize = 14;
        }
        else
        {
            // Dự phòng nếu chưa kịp render UI sub-elements (Unity cũ): 
            // In chữ màu Đen nổi bật trên nền Trắng mặc định của Unity
            _chatInput.style.color = Color.black;
        }

        // Fix: Không cho phép tự động bắt Focus khi chưa nhấn Enter
        _chatInput.focusable = false;

        _chatInput.RegisterCallback<KeyDownEvent>(OnInputKey);
        _chatContainer.Add(_chatInput);

        AddLocalMessage("<color=#73ACE5>[Hệ thống]</color> Chào mừng bạn! Gõ /w [ID] để nhắn tin mật.");
    }

    private void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        // Nhấn Enter để bắt đầu chat hoặc gửi tin
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            if (!_isChatting)
            {
                FocusChat();
            }
            else
            {
                SubmitChat();
            }
        }

        // Nhấn Esc để hủy chat
        if (kb.escapeKey.wasPressedThisFrame && _isChatting)
        {
            UnfocusChat();
        }
    }

    private void FocusChat()
    {
        _isChatting = true;
        _chatInput.focusable = true;
        _chatInput.Focus();
        _chatContainer.style.backgroundColor = new Color(0, 0, 0, 0.7f);
    }

    private void UnfocusChat()
    {
        _isChatting = false;
        _chatInput.value = "";
        _chatInput.Blur();
        _chatInput.focusable = false;
        _chatContainer.style.backgroundColor = new Color(0, 0, 0, 0.4f);
    }

    private void OnInputKey(KeyDownEvent evt)
    {
        // Chặn các phím di chuyển không cho lọt vào input
        evt.StopPropagation();
    }

    private void SubmitChat()
    {
        string msg = _chatInput.value.Trim();
        if (!string.IsNullOrEmpty(msg))
        {
            // Kiểm tra lệnh /w (Whisper)
            if (msg.StartsWith("/w "))
            {
                string[] parts = msg.Split(' ');
                if (parts.Length >= 3)
                {
                    int targetId;
                    if (int.TryParse(parts[1], out targetId))
                    {
                        string privateMsg = string.Join(" ", parts, 2, parts.Length - 2);
                        Rpc_SendPrivateMessage(targetId, privateMsg);
                    }
                }
            }
            else
            {
                // Gửi tin nhắn công khai
                Rpc_SendPublicMessage(msg);
            }
        }
        UnfocusChat();
    }

    private void AddLocalMessage(string text)
    {
        // Thêm tin nhắn vào log (hỗ trợ Rich Text của Unity như <color>, <b>)
        var label = new Label(text);
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.color = Color.white;
        label.style.fontSize = 14;
        label.style.paddingLeft = 2;
        label.style.marginBottom = 1;
        
        _chatLog.Add(label);
        
        // Tự động cuộn xuống cuối (Delayed để Layout kịp cập nhật)
        if (gameObject.activeInHierarchy)
            StartCoroutine(ScrollToBottom());
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SendPublicMessage(string message, RpcInfo info = default)
    {
        string senderName = $"<color=#73ACE5>Player {info.Source.PlayerId}</color>";
        AddLocalMessage($"<b>{senderName}:</b> {message}");
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SendPrivateMessage(int targetPlayerId, string message, RpcInfo info = default)
    {
        int myId = Runner.LocalPlayer.PlayerId;
        int senderId = info.Source.PlayerId;

        // Chỉ hiển thị nếu mình là người gửi HOẶC là người nhận
        if (myId == targetPlayerId)
        {
             AddLocalMessage($"<color=#FFD700>[THẦM THÌ] Player {senderId}:</color> {message}");
        }
        else if (myId == senderId)
        {
             AddLocalMessage($"<color=#AAAAAA>[Gửi tới {targetPlayerId}]:</color> {message}");
        }
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForSeconds(0.1f);
        _chatLog.verticalScroller.value = _chatLog.verticalScroller.highValue;
    }
}
