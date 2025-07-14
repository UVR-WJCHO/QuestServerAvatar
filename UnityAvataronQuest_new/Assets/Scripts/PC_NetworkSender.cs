// PC_NetworkSender.cs
using UnityEngine;
using UnityEngine.UI; // For InputField and Text
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class PC_NetworkSender : MonoBehaviour
{
    public InputField messageInputField;
    public Text statusText; // Optional: For displaying server status
    public int port = 8080;

    private TcpListener tcpListener;
    private TcpClient connectedTcpClient;
    private NetworkStream networkStream;
    private bool isClientConnected = false;

    async void Start()
    {
        await StartServerAsync();
    }

    async Task StartServerAsync()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            UpdateStatus($"Server started on port {port}. Waiting for Quest 3...");
            Debug.Log($"Server started on port {port}. Waiting for connection...");

            connectedTcpClient = await tcpListener.AcceptTcpClientAsync();
            networkStream = connectedTcpClient.GetStream();
            isClientConnected = true;
            UpdateStatus("Quest 3 connected!");
            Debug.Log("Quest 3 connected.");
        }
        catch (Exception e)
        {
            UpdateStatus($"Error starting server: {e.Message}");
            Debug.LogError($"Server starting error: {e.Message}");
        }
    }

    public async void SendInputText()
    {
        if (!isClientConnected || networkStream == null || messageInputField == null)
        {
            UpdateStatus("Cannot send: Quest 3 not connected or InputField missing.");
            Debug.LogError("Client not connected, stream is null, or InputField not assigned.");
            return;
        }

        string message = messageInputField.text;
        if (string.IsNullOrEmpty(message))
        {
            UpdateStatus("Cannot send: Message is empty.");
            return;
        }

        try
        {
            byte[] dataToSend = Encoding.UTF8.GetBytes(message);
            await networkStream.WriteAsync(dataToSend, 0, dataToSend.Length);
            await networkStream.FlushAsync(); // Ensure data is sent
            UpdateStatus($"Sent: {message}");
            Debug.Log($"Sent message: {message}");
            messageInputField.text = ""; // Clear input field
        }
        catch (Exception e)
        {
            UpdateStatus($"Error sending data: {e.Message}");
            Debug.LogError($"Error sending data: {e.Message}");
            HandleDisconnection();
        }
    }

    void HandleDisconnection()
    {
        isClientConnected = false;
        networkStream?.Close();
        connectedTcpClient?.Close();
        networkStream = null;
        connectedTcpClient = null;
        UpdateStatus("Quest 3 disconnected. Attempting to listen for new connection...");
        Debug.LogWarning("Client disconnected. Restarting listener.");
        tcpListener?.Stop(); // Stop current listener before restarting
        _ = StartServerAsync(); // Restart listening for a new client
    }
    
    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            Debug.Log($"Status: {message}");
        }
    }

    void OnDestroy()
    {
        CleanUpNetworkResources();
    }

    void OnApplicationQuit()
    {
        CleanUpNetworkResources();
    }

    private async void CleanUpNetworkResources()
    {
        if (isClientConnected && networkStream != null)
        {
            try
            {
                // Attempt to send a "server shutting down" message
                byte[] shutdownMsg = Encoding.UTF8.GetBytes("SERVER_SHUTDOWN_Signal");
                await networkStream.WriteAsync(shutdownMsg, 0, shutdownMsg.Length);
                await networkStream.FlushAsync();
            }
            catch (Exception e) { Debug.LogWarning($"Could not send shutdown signal: {e.Message}"); }
        }

        networkStream?.Close();
        connectedTcpClient?.Close();
        tcpListener?.Stop();
        networkStream = null;
        connectedTcpClient = null;
        tcpListener = null;
        isClientConnected = false;
        Debug.Log("Network resources cleaned up.");
    }
}