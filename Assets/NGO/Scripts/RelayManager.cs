using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    [SerializeField] private int _maxPlayerNum = 3;
    [SerializeField] private Camera _uiCam = null;
    [SerializeField] private TextMeshProUGUI _joinCodeText = null;
    [SerializeField] private TMP_InputField _inputField = null;
    [SerializeField] private GameObject _multiUi = null;

    private UnityTransport _transport = null;

    private async void Awake()
    {
        _multiUi.SetActive(false);
        _transport = FindAnyObjectByType<UnityTransport>();

        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (AuthenticationService.Instance.IsSignedIn)
            _multiUi.SetActive(true);
    }

    public async void CreateGame()
    {
        _multiUi.SetActive(false);

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(_maxPlayerNum);
        _joinCodeText.text = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        _transport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

        NetworkManager.Singleton.StartHost();
        _uiCam.enabled = false;
    }

    public async void JoinGame()
    {
        _multiUi.SetActive(false);

        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(_inputField.text);

        _transport.SetClientRelayData(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData);

        NetworkManager.Singleton.StartClient();
        _uiCam.enabled = false;
    }
}
