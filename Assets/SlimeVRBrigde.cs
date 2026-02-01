using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using OscJack;

public class SlimeVRBridge : MonoBehaviour
{
    [Header("Network Settings")]
    public int oscPort = 9001;

    [Header("Tracking Targets")]
    public Transform headTarget;
    public Transform leftController;
    public Transform leftHand;
    public Transform rightController;
    public Transform rightHand;
    
    private OscClient _client;
    private string _detectedIp = "";
    private int _detectedPort = 0;
    private UdpClient _discoveryClient;
    private Thread _discoveryThread;
    private volatile bool _isSearching = true;
    
    private const int SLIMEVR_TRACKER_PORT = 6969;
    private AndroidJavaObject _multicastLock;

    void Start()
    {
        Debug.Log("SlimeVR Bridge: Inicializace...");
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        EnableMulticastOnAndroid();
        StartNetworkDiscovery();
    }

    void EnableMulticastOnAndroid()
    {
    #if UNITY_ANDROID && !UNITY_EDITOR
        try {
            using (AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = playerClass.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject wifiManager = currentActivity.Call<AndroidJavaObject>("getSystemService", "wifi"))
            {
                _multicastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "SlimeVRDiscoveryLock");
                _multicastLock.Call("acquire");
                Debug.Log("SlimeVR Bridge: Android Multicast Lock získán.");
            }
        } catch (System.Exception e) {
            Debug.LogError($"SlimeVR Bridge: Chyba při získávání Multicast Locku: {e.Message}");
        }
    #endif
    }

    string GetLocalIPAddress()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString();
            }
        }
        catch { return null; }
    }

    void StartNetworkDiscovery()
    {
        _discoveryThread = new Thread(() =>
        {
            try
            {
                string localIp = GetLocalIPAddress();
                if (string.IsNullOrEmpty(localIp)) return;

                string[] parts = localIp.Split('.');
                if (parts.Length != 4) return;
                string subnet = $"{parts[0]}.{parts[1]}.{parts[2]}.";
                
                Debug.Log($"SlimeVR Bridge: Skenuju subnet {subnet}0/24...");

                _discoveryClient = new UdpClient();
                _discoveryClient.Client.ReceiveTimeout = 100;
                _discoveryClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                byte[] handshakePacket = new byte[] {
                    0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
                };

                while (_isSearching)
                {
                    for (int i = 1; i <= 254 && _isSearching; i++)
                    {
                        string targetIp = subnet + i;
                        if (targetIp == localIp) continue;
                        try
                        {
                            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse(targetIp), SLIMEVR_TRACKER_PORT);
                            _discoveryClient.Send(handshakePacket, handshakePacket.Length, serverEndpoint);
                        }
                        catch { }
                    }

                    System.DateTime waitStart = System.DateTime.Now;
                    while ((System.DateTime.Now - waitStart).TotalMilliseconds < 2000 && _isSearching)
                    {
                        try
                        {
                            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                            byte[] response = _discoveryClient.Receive(ref remoteEndPoint);
                            
                            if (response.Length > 0)
                            {
                                string responseStr = Encoding.ASCII.GetString(response);
                                bool isSlimeVR = responseStr.Contains("Hey OVR") || 
                                                 (response.Length >= 4 && response[3] == 3);

                                if (isSlimeVR)
                                {
                                    _detectedIp = remoteEndPoint.Address.ToString();
                                    _detectedPort = oscPort;
                                    Debug.Log($"<color=green>SlimeVR Bridge: Server nalezen na {_detectedIp}</color>");
                                    _isSearching = false;
                                    break;
                                }
                            }
                        }
                        catch (SocketException) { }
                    }

                    if (_isSearching)
                    {
                        Debug.Log("SlimeVR Bridge: Server nenalezen, opakuji...");
                        Thread.Sleep(3000);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Discovery Error: " + e.Message);
            }
        });
        _discoveryThread.IsBackground = true;
        _discoveryThread.Start();
    }

    void Update()
    {
        if (!_isSearching && _client == null && !string.IsNullOrEmpty(_detectedIp))
        {
            int portToUse = _detectedPort > 0 ? _detectedPort : oscPort;
            _client = new OscClient(_detectedIp, portToUse);
            Debug.Log($"<color=green>SlimeVR Bridge: Připojeno na {_detectedIp}:{portToUse}</color>");
        }

        if (_client == null) return;

        // Hlava
        if (headTarget != null)
        {
            SendVRSystemPose("/tracking/vrsystem/head/pose", headTarget.position, headTarget.rotation, true);
        }

        // Levá ruka
        Transform activeLeft = GetActiveTarget(leftHand, leftController);
        if (activeLeft != null)
        {
            SendVRSystemPose("/tracking/vrsystem/leftwrist/pose", activeLeft.position, activeLeft.rotation, false);
        }

        // Pravá ruka
        Transform activeRight = GetActiveTarget(rightHand, rightController);
        if (activeRight != null)
        {
            SendVRSystemPose("/tracking/vrsystem/rightwrist/pose", activeRight.position, activeRight.rotation, false);
        }
    }

    void SendVRSystemPose(string address, Vector3 pos, Quaternion rot, bool isHead)
    {
        Vector3 euler = rot.eulerAngles;
        float rotX = isHead ? euler.x : -euler.x;
        float rotY = euler.y;
        float rotZ = isHead ? euler.z : -euler.z;
        _client.Send(address, pos.x, pos.y, pos.z, rotX, rotY, rotZ);
    }

    Transform GetActiveTarget(Transform hand, Transform controller)
    {
        if (hand != null && hand.gameObject.activeInHierarchy) return hand;
        if (controller != null && controller.gameObject.activeInHierarchy) return controller;
        return null;
    }

    void OnDestroy()
    {
        _isSearching = false;
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (_multicastLock != null) {
            _multicastLock.Call("release");
            _multicastLock.Dispose();
        }
        #endif
        _discoveryClient?.Close();
        if (_discoveryThread != null && _discoveryThread.IsAlive) _discoveryThread.Abort();
        _client?.Dispose();
    }
}