using UnityEngine;
using UnityEngine.UI;     // Button 사용을 위해 추가
using TMPro;              // TMP_InputField, TextMeshProUGUI 사용을 위해 추가
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

public class Quest3_NetworkReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    // pcIpAddress는 이제 UI를 통해 동적으로 설정됩니다.
    // Inspector에는 기본값 또는 마지막으로 사용된 IP를 표시할 수 있습니다.
    [SerializeField] private string pcIpAddress = "192.168.0.5"; // 기본값
    public int port = 52734;

    [Header("UI Elements - Inspector에서 할당")]
    public GameObject ipInputPanel;         // IP 주소 입력 UI 패널 (InputField와 Button 포함)
    public TMP_InputField ipAddressInputField; // IP 주소 입력 필드
    public Button connectButton;            // 연결 시도 버튼
    public TextMeshProUGUI displayText;     // 메시지 및 상태 표시용 TextMeshPro UI

    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private bool isConnected = false;
    
    private static readonly DateTime EpochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string PREF_KEY_IP_ADDRESS = "TargetPCIPAddress"; // PlayerPrefs 키

    void Start()
    {
        // UI 요소들이 Inspector에서 제대로 할당되었는지 확인
        if (ipInputPanel == null || ipAddressInputField == null || connectButton == null || displayText == null)
        {
            Debug.LogError("UI 요소 중 하나 이상이 Inspector에 할당되지 않았습니다! 정상 작동이 불가능합니다.");
            if (displayText != null) UpdateDisplay("UI 설정 오류!", false); // displayText라도 있으면 오류 표시
            this.enabled = false; // 컴포넌트 비활성화
            return;
        }

        // 초기 UI 상태 설정: IP 입력 패널 활성화, 메인 메시지 표시는 비활성화
        ipInputPanel.SetActive(true);
        displayText.gameObject.SetActive(false);

        // 이전에 저장된 IP 주소가 있으면 불러와서 InputField에 설정
        string savedIP = PlayerPrefs.GetString(PREF_KEY_IP_ADDRESS, pcIpAddress); // 저장된 값 없으면 기본값 사용
        ipAddressInputField.text = savedIP;
        this.pcIpAddress = savedIP; // 내부 변수도 업데이트

        // 연결 버튼에 리스너 추가
        connectButton.onClick.AddListener(OnConnectButtonPressed);
        
        // UnityMainThreadDispatcher 인스턴스 확인 (없으면 자동 생성 시도)
        if (UnityMainThreadDispatcher.Instance() == null) {
            Debug.LogWarning("UnityMainThreadDispatcher 인스턴스를 찾을 수 없어 새로 생성합니다.");
        }
    }

    // 연결 버튼 클릭 시 호출될 메소드
    public async void OnConnectButtonPressed()
    {
        string enteredIP = ipAddressInputField.text.Trim(); // 입력된 IP (앞뒤 공백 제거)

        if (string.IsNullOrWhiteSpace(enteredIP))
        {
            Debug.LogError("IP 주소가 비어있습니다.");
            // 필요하다면 ipInputPanel 내에 사용자에게 보여줄 오류 메시지 UI를 추가할 수 있습니다.
            UpdateDisplay("IP 주소를 입력하세요.", false); // 임시로 메인 디스플레이 사용
            // 또는 ipInputPanel 내의 상태 텍스트를 업데이트
            return;
        }

        this.pcIpAddress = enteredIP; // 현재 사용할 IP 주소 업데이트
        PlayerPrefs.SetString(PREF_KEY_IP_ADDRESS, enteredIP); // 다음 실행을 위해 IP 주소 저장
        PlayerPrefs.Save(); // 변경사항 즉시 저장

        // IP 입력 UI 비활성화, 메인 메시지 UI 활성화
        ipInputPanel.SetActive(false);
        displayText.gameObject.SetActive(true);
        UpdateDisplay($"PC ({this.pcIpAddress}:{port})에 연결 시도 중...", false);

        // 비동기적으로 서버 연결 시도
        await ConnectToServerAsync();
    }

    async Task ConnectToServerAsync()
    {
        // 이전 연결이 남아있다면 정리
        if (isConnected || tcpClient != null)
        {
            HandleDisconnection("새 연결 시도를 위해 이전 연결 정리 중.", false); // UI 업데이트 없이 내부적으로만 처리
            await Task.Delay(100); // 리소스 해제 시간 확보
        }

        try
        {
            tcpClient = new TcpClient();
            Debug.Log($"PC 서버 {pcIpAddress}:{port}에 연결을 시도합니다.");
            
            Task connectTask = tcpClient.ConnectAsync(pcIpAddress, port);
            // 연결 타임아웃 7초로 증가
            if (await Task.WhenAny(connectTask, Task.Delay(7000)) == connectTask && tcpClient.Connected)
            {
                networkStream = tcpClient.GetStream();
                isConnected = true;
                UpdateDisplay("PC에 연결되었습니다!", false);
                Debug.Log("PC 서버에 연결 성공.");
                _ = ListenForMessagesAsync(); // 메시지 수신 루프 시작
            }
            else
            {
                tcpClient?.Close(); // 연결 실패 또는 타임아웃 시 소켓 정리
                tcpClient = null;
                throw new TimeoutException($"서버 ({pcIpAddress}:{port}) 연결 시간 초과 또는 실패.");
            }
        }
        catch (Exception e)
        {
            UpdateDisplay($"연결 오류: {e.Message}", false);
            Debug.LogError($"서버 ({pcIpAddress}:{port}) 연결 오류: {e.Message}");
            isConnected = false;
            tcpClient = null; // 오류 발생 시 클라이언트 객체 null 처리

            // 연결 실패 시 IP 입력 패널 다시 활성화 (UI 스레드에서)
            UnityMainThreadDispatcher.Instance()?.Enqueue(() => {
                if (ipInputPanel != null) ipInputPanel.SetActive(true);
                if (displayText != null) displayText.gameObject.SetActive(false);
            });
        }
    }

    async Task ListenForMessagesAsync()
    {
        if (networkStream == null) {
            Debug.LogError("ListenForMessagesAsync 호출 시 networkStream이 null입니다.");
            HandleDisconnection("네트워크 스트림이 없어 리스닝을 시작할 수 없습니다.", true);
            return;
        }
        var receiveBuffer = new byte[4096]; 

        try
        {
            while (isConnected && tcpClient != null && tcpClient.Connected)
            {
                int bytesRead = await networkStream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                double clientReceiveTimestampEpochSeconds = (DateTime.UtcNow - EpochStart).TotalSeconds;

                if (bytesRead > 0)
                {
                    string receivedPayload = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
                    Debug.Log($"수신된 페이로드: {receivedPayload}");

                    if (receivedPayload == "SERVER_SHUTDOWN_Signal") 
                    {
                        HandleDisconnection("서버가 종료 신호를 보냈습니다.", true);
                        UnityMainThreadDispatcher.Instance()?.Enqueue(() => UpdateDisplay("PC 서버가 종료되었습니다.", false));
                        break;
                    }

                    string[] parts = receivedPayload.Split(new char[]{'|'}, 2); 
                    if (parts.Length == 2)
                    {
                        string serverTimestampStr = parts[0];
                        string actualMessage = parts[1];
                        
                        if (double.TryParse(serverTimestampStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double serverSendTimestampEpochSeconds))
                        {
                            double latencySeconds = clientReceiveTimestampEpochSeconds - serverSendTimestampEpochSeconds;
                            double latencyMilliseconds = latencySeconds * 1000.0;
                            string messageToShow = $"{actualMessage} ({latencyMilliseconds:F2} ms)";
                            UnityMainThreadDispatcher.Instance()?.Enqueue(() => UpdateDisplay(messageToShow, true));
                        }
                        else
                        {
                            Debug.LogError($"서버 타임스탬프 파싱 오류: {serverTimestampStr}. 전체 페이로드: {receivedPayload}");
                            string errorMessage = $"[TS파싱오류] {actualMessage}";
                            UnityMainThreadDispatcher.Instance()?.Enqueue(() => UpdateDisplay(errorMessage, false));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"형식이 맞지 않는 메시지 수신: {receivedPayload}");
                        string unformattedMessage = $"[형식오류] {receivedPayload}";
                        UnityMainThreadDispatcher.Instance()?.Enqueue(() => UpdateDisplay(unformattedMessage, false));
                    }
                }
                else
                {
                    HandleDisconnection("서버 연결이 끊어졌습니다 (0 바이트 수신).", true);
                    break;
                }
            }
        }
        catch (ObjectDisposedException) { HandleDisconnection("연결이 손실되었습니다 (ObjectDisposed).", true); }
        catch (System.IO.IOException ex) { HandleDisconnection($"연결 오류 (IO): {ex.Message}", true); }
        catch (Exception e)
        {
            if (isConnected) 
            {
                Debug.LogError($"데이터 수신 중 오류 발생: {e.Message}");
                HandleDisconnection($"데이터 수신 중 오류: {e.Message}", true);
            }
        }
    }
    
    // isFormattedForLatency 인자는 메시지 내용에 이미 (XX.XX ms) 같은 지연시간 정보가 포함되어 포맷팅 되었는지를 나타냄
    // 여기서는 isLatencyMessage 로 이름을 변경하여, 해당 메시지가 지연시간 정보를 포함하는 메시지인지를 나타내도록 함.
    void UpdateDisplay(string message, bool isLatencyMessage)
    {
        if (displayText != null)
        {
            displayText.text = message; 
        }
        Debug.Log($"UI 표시: {message}");
    }

    // showIpInputOnDisconnect 파라미터 추가: 연결 해제 시 IP 입력 패널을 다시 표시할지 여부
    void HandleDisconnection(string reason, bool showIpInputOnDisconnect)
    {
        if (!isConnected && tcpClient == null && networkStream == null) return; 

        isConnected = false; 
        
        networkStream?.Close(); 
        networkStream = null;
        
        tcpClient?.Close(); 
        tcpClient = null;
        
        Debug.LogWarning($"서버로부터 연결 해제됨: {reason}");
        
        UnityMainThreadDispatcher.Instance()?.Enqueue(() => {
            UpdateDisplay($"연결 끊김: {reason}. IP를 확인하고 다시 시도하세요.", false);
            if (showIpInputOnDisconnect && Application.isPlaying && this.enabled) {
                 if (ipInputPanel != null) ipInputPanel.SetActive(true);
                 if (displayText != null) displayText.gameObject.SetActive(false);
            }
        });
    }

    void OnDestroy()
    {
        // 리스너 제거는 Null-conditional 연산자를 사용하거나, connectButton이 null이 아닐 때만 호출
        connectButton?.onClick.RemoveListener(OnConnectButtonPressed);
        HandleDisconnection("클라이언트(Quest3_NetworkReceiver)가 제거됩니다 (OnDestroy).", false);
    }
    
    void OnApplicationQuit()
    {
        HandleDisconnection("클라이언트 애플리케이션이 종료됩니다 (OnApplicationQuit).", false);
    }
}