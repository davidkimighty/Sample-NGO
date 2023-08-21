using Unity.Netcode;
using UnityEngine;

public class RPCTest : NetworkBehaviour
{
    // RPC stands for Remote Procedural Calls
    public override void OnNetworkSpawn()
    {
        //Only send an RPC to the server on the client that owns the NetworkObject that owns this NetworkBehaviour instance
        if (!IsServer && IsOwner) 
        {
            TestServerRpc(0, NetworkObjectId);
        }
    }

    [ServerRpc]
    void TestServerRpc(int value, ulong sourceNetworkObjectId)
    {
        Debug.Log($"Server Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
        TestClientRpc(value, sourceNetworkObjectId);
    }

    [ClientRpc]
    void TestClientRpc(int value, ulong sourceNetworkObjectId)
    {
        Debug.Log($"Client Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
        //Only send an RPC to the server on the client that owns the NetworkObject that owns this NetworkBehaviour instance
        if (IsOwner) 
        {
            TestServerRpc(value + 1, sourceNetworkObjectId);
        }
    }
}
