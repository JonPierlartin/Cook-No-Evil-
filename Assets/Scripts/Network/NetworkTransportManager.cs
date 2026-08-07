using System;
using System.Linq;
using Netcode.Transports.Facepunch;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

// Steam SDR (FacepunchTransport) ve Local UDP (UnityTransport) arasinda gecis yapan wrapper.
// GDD 2.1: transport, NetworkManager.Start*() cagrilmadan ONCE atanmis olmalidir; ag
// baslatildiktan sonra transport degistirilemez, bu yuzden ConfigureTransport bunu kontrol eder.
public class NetworkTransportManager : MonoBehaviour
{
    public static NetworkTransportManager Instance { get; private set; }

    [SerializeField] private TransportMode defaultMode = TransportMode.Steam;
    [SerializeField] private FacepunchTransport steamTransport;
    [SerializeField] private UnityTransport localUdpTransport;

    public TransportMode CurrentMode { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (steamTransport == null)
            steamTransport = GetComponent<FacepunchTransport>();
        if (localUdpTransport == null)
            localUdpTransport = GetComponent<UnityTransport>();
    }

    private void Start()
    {
        // NetworkManager.Singleton kendi Awake'inde atanir; sira garantisi olmadigi icin
        // ilk transport secimini Start'a erteliyoruz (butun Awake'ler tamamlandiktan sonra calisir).
        var mode = Environment.GetCommandLineArgs().Contains("-localudp") ? TransportMode.LocalUdp : defaultMode;
        ConfigureTransport(mode);
    }

    public void ConfigureTransport(TransportMode mode)
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("[NetworkTransportManager] NetworkManager.Singleton bulunamadi.");
            return;
        }

        if (networkManager.IsListening)
        {
            Debug.LogError("[NetworkTransportManager] Transport, ag baslatildiktan SONRA degistirilemez.");
            return;
        }

        CurrentMode = mode;

        if (mode == TransportMode.Steam)
        {
            networkManager.NetworkConfig.NetworkTransport = steamTransport;
        }
        else
        {
            localUdpTransport.SetConnectionData("127.0.0.1", 7777);
            networkManager.NetworkConfig.NetworkTransport = localUdpTransport;
        }

        Debug.Log($"[NetworkTransportManager] Transport ayarlandi: {mode}");
    }

    public void SetSteamHostTarget(ulong hostSteamId)
    {
        if (steamTransport == null)
        {
            Debug.LogError("[NetworkTransportManager] Steam transport atanmamis.");
            return;
        }

        steamTransport.targetSteamId = hostSteamId;
    }
}
