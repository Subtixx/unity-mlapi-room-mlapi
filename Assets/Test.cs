using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using DefaultNamespace;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Puncher.Client;
using MLAPI.Transports;
using MLAPI.Transports.UNET;
using Open.Nat;
using Debug = UnityEngine.Debug;

public class Test : MonoBehaviour
{
    private static string _ipAdress = "";
    private static int _port;
    private static string _roomName = "";

    private static Test _instance;

    void Awake()
    {
        // Not pretty but should work.
        _instance = this;
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            StartButtons();
        }
        else
        {
            _roomName = GUILayout.TextField(_roomName, 32);
            if (GUILayout.Button("Send Room Name") && _roomName != "")
                SendRoomName();
            StatusLabels();

            SubmitNewPosition();
        }

        GUILayout.EndArea();
    }

    static void StartButtons()
    {
    NetworkManager.Singleton.NetworkConfig.ConnectionData
        NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApprovalCallback;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnOnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnOnClientDisconnectCallback;
        NetworkManager.Singleton.NetworkConfig.NetworkTransport.OnTransportEvent += NetworkTransportOnOnTransportEvent;
        CustomMessagingManager.OnUnnamedMessage += CustomMessagingManagerOnOnUnnamedMessage;

        if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();

        GUILayout.Space(10.0f);
        GUILayout.Label("---------------------------- CLIENT ----------------------------");

        if (GUILayout.Button("Client (OpenNAT)/No Nat Punch"))
        {
            NetworkManager.Singleton.StartClient();
        }

        if (GUILayout.Button("Client (MLApiNatPuncher)"))
        {
            DoMLApiNatPunchClient();
        }

        GUILayout.Space(10.0f);
        GUILayout.Label("---------------------------- SERVER ----------------------------");

        if (GUILayout.Button("Server"))
        {
            _port = NetworkManager.Singleton.GetComponent<UNetTransport>().ServerListenPort;

            NetworkManager.Singleton.StartServer();
        }
        if (GUILayout.Button("Server (OpenNAT)"))
        {
            _port = NetworkManager.Singleton.GetComponent<UNetTransport>().ServerListenPort;

            NetworkManager.Singleton.StartServer();

            DoNatPunch();
        }

        if (GUILayout.Button("Server (MLApiNatPuncher)"))
        {
            _port = NetworkManager.Singleton.GetComponent<UNetTransport>().ServerListenPort;
            var natPunchListener = new Thread(DoMLApiNatPunchServer);
            natPunchListener.Start();

            NetworkManager.Singleton.StartServer();
        }
    }

    private static void CustomMessagingManagerOnOnUnnamedMessage(ulong clientid, Stream stream)
    {
        using (var br = new BinaryReader(stream))
        {
            br.ReadInt16(); // Unknown!?
            var type = br.ReadInt16();
            Debug.Log("GOT TYPE: " + type);
            if (type == 1)
            {
                Debug.Log("TYPE: " + type);

                var result = br.ReadBoolean();
                Debug.Log("RESULT: " + result);
            }
        }
    }

    private static void OnConnectionApprovalCallback(byte[] arg1, ulong arg2, NetworkManager.ConnectionApprovedDelegate arg3)
    {
        Debug.Log("OnConnectionApprovalCallback");
    }

    private static void OnServerStarted()
    {
        //Instance.StartCoroutine(SendRoomName());
    }

    private static void NetworkTransportOnOnTransportEvent(NetworkEvent type, ulong clientid, NetworkChannel networkchannel, ArraySegment<byte> payload, float receivetime)
    {
        Debug.Log("YES");
    }

    private static void JoinRoom(string roomNameToJoin)
    {
        var transport = NetworkManager.Singleton.GetComponent<UNetTransport>();
        
        var payloadBuffer = new byte[46];

        using (var ms = new MemoryStream(payloadBuffer))
        {
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(int.Parse("FF03FFFF", System.Globalization.NumberStyles.HexNumber));
                
                bw.Write(roomNameToJoin);
            }
        }
            
        //var payload = new ArraySegment<byte>(payloadBuffer, 0, 4);
        var payload = new ArraySegment<byte>(payloadBuffer, 0, 37);
        transport.Send(0, payload, NetworkChannel.Internal);
    }

    private static void SendRoomName()
    {
        var transport = NetworkManager.Singleton.GetComponent<UNetTransport>();
        
        // This has to be actual length + 10 since RelayTransport will copy connectionId into the last bytes
        var payloadBuffer = new byte[46];

        using (var ms = new MemoryStream(payloadBuffer))
        {
            using (var bw = new BinaryWriter(ms))
            {
                // First 4 bytes are the packet type.
                
                // This is actually FF FF 01 FF but because of endianess it has to be written as FF 01 FF FF
                bw.Write(int.Parse("FF01FFFF", System.Globalization.NumberStyles.HexNumber));
                
                bw.Write(_roomName);
            }
        }
            
        //var payload = new ArraySegment<byte>(payloadBuffer, 0, 4);
        var payload = new ArraySegment<byte>(payloadBuffer, 0, 37);
        transport.Send(0, payload, NetworkChannel.Internal);
    }

    #region MLApiNatPuncher

    // This doesn't work for some reason. Could be because the server doesn't even respond to telnet. Not sure whats wrong.
    static void DoMLApiNatPunchServer()
    {
        using (var listener = new PuncherClient("173.249.46.57", 6776))
        {
            // 1234 is the port where the other peer will connect and punch through.
            // That would be the port where your program is going to be listening after the punch is done.
            listener.ListenForPunches(new IPEndPoint(IPAddress.Any, _port));
        }
    }

    // This doesn't work for some reason. Could be because the server doesn't even respond to telnet. Not sure whats wrong.
    static void DoMLApiNatPunchClient()
    {
        // Get listener public IP address by means of a matchmaker or otherwise.
        var listenerAddress = NetworkManager.Singleton.GetComponent<UNetTransport>().ConnectAddress;

        // Creates the connector with the address and port to the server.
        // Disposal stops everything and closes the connection.
        using (var connector = new PuncherClient("173.249.46.57", 6776))
        {
            // Punches and returns the result
            if (connector.TryPunch(IPAddress.Parse(listenerAddress), out var remoteEndPoint))
            {
                NetworkManager.Singleton.GetComponent<UNetTransport>().ConnectAddress =
                    remoteEndPoint.Address.ToString();
                NetworkManager.Singleton.GetComponent<UNetTransport>().ConnectPort =
                    remoteEndPoint.Port;
                NetworkManager.Singleton.StartClient();
            }
            else
            {
                Debug.LogError("NAT Punching failed.");
                // NAT Punchthrough failed.
            }
        }
    }

    #endregion

    #region OpenNAT
    /// <summary>
    /// Does Upnp over OpenNAT
    /// </summary>
    static async void DoNatPunch()
    {
        NatDiscoverer.TraceSource.Switch.Level = SourceLevels.Verbose;
        NatDiscoverer.TraceSource.Listeners.Add(new UnityTraceListener());
        try
        {
            var discoverer = new NatDiscoverer();
            var cts = new CancellationTokenSource(10000);
            var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            var ip = await device.GetExternalIPAsync();
            _ipAdress = ip.ToString();
            Debug.Log("The external IP Address is: " + _ipAdress);

            await device.CreatePortMapAsync(new Mapping(Protocol.Tcp,
                NetworkManager.Singleton.GetComponent<UNetTransport>().ServerListenPort,
                NetworkManager.Singleton.GetComponent<UNetTransport>().ServerListenPort, "Unity Test"));
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }
    #endregion

    private static void OnOnClientDisconnectCallback(ulong obj)
    {
        Debug.Log("OnOnClientDisconnectCallback");
    }

    private static void OnOnClientConnectedCallback(ulong obj)
    {
        Debug.Log("OnOnClientConnectedCallback");
    }

    static void StatusLabels()
    {
        var mode = NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
                        NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
        GUILayout.Label("IP: " + _ipAdress);
    }

    static void SubmitNewPosition()
    {
        if (GUILayout.Button(NetworkManager.Singleton.IsServer ? "Move" : "Request Position Change"))
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId,
                out var networkedClient))
            {
                var player = networkedClient.PlayerObject.GetComponent<Player>();
                if (player)
                {
                    player.Move();
                }
            }
        }
    }
}