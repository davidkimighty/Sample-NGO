using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ParrelSync;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class MatchMakingTest : MonoBehaviour
{
    private const string JoinCodeKey = "h";

    [SerializeField] private Camera _cam = null;
    [SerializeField] private GameObject _matchMakerUi = null;

    private Lobby _connectedLobby = null;
    private QueryResponse _lobbies = null;
    private UnityTransport _transport = null;
    private string _playerId = null;

    private void Awake()
    {
        _transport = FindAnyObjectByType<UnityTransport>();
    }

    private void OnDestroy()
    {
        try
        {
            StopAllCoroutines();
            if (_connectedLobby != null)
            {
                if (_connectedLobby.HostId == _playerId)
                    Lobbies.Instance.DeleteLobbyAsync(_connectedLobby.Id);
                else
                    Lobbies.Instance.RemovePlayerAsync(_connectedLobby.Id, _playerId);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Shutting down lobby: {e}");
        }
    }

    public async void CreateOrJoinLobby()
    {
        await Authenticate();

        _connectedLobby = await QuickJoinLobby() ?? await CreateLobby();
        if (_connectedLobby != null)
        {
            _matchMakerUi.SetActive(false);
            _cam.enabled = false;
        }
    }

    private async Task Authenticate()
    {
        var options = new InitializationOptions();
#if UNITY_EDITOR
        options.SetProfile(ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
#endif
        await UnityServices.InitializeAsync(options);

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        _playerId = AuthenticationService.Instance.PlayerId;
    }

    private async Task<Lobby> QuickJoinLobby()
    {
        try
        {
            var lobby = await Lobbies.Instance.QuickJoinLobbyAsync();
            var joinAlloc = await RelayService.Instance.JoinAllocationAsync(lobby.Data[JoinCodeKey].Value);

            SetTransformAsClient(joinAlloc);

            NetworkManager.Singleton.StartClient();
            return lobby;
        }
        catch (Exception e)
        {
            Debug.Log("No lobbies");
            return null;
        }

        void SetTransformAsClient(JoinAllocation joinAlloc)
        {
            _transport.SetClientRelayData(joinAlloc.RelayServer.IpV4, (ushort)joinAlloc.RelayServer.Port,
                joinAlloc.AllocationIdBytes, joinAlloc.Key, joinAlloc.ConnectionData, joinAlloc.HostConnectionData);
        }
    }

    private async Task<Lobby> CreateLobby()
    {
        try
        {
            const int maxPlayers = 100;
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> { { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, joinCode) } }
            };
            Lobby lobby = await Lobbies.Instance.CreateLobbyAsync("Name", maxPlayers, options);

            StartCoroutine(LobbyHeartbeatCoroutine(lobby.Id, 15));

            _transport.SetHostRelayData(alloc.RelayServer.IpV4, (ushort)alloc.RelayServer.Port,
                alloc.AllocationIdBytes, alloc.Key, alloc.ConnectionData);

            NetworkManager.Singleton.StartHost();
            return lobby;
        }
        catch (Exception e)
        {
            Debug.Log("Failed creating a lobby");
            return null;
        }
    }

    private static IEnumerator LobbyHeartbeatCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }
}
