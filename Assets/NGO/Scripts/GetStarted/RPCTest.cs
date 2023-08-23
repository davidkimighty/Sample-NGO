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

    // ServerRpc is a code that can only run on the server that is triggered by clients.
    [ServerRpc]
    void TestServerRpc(int value, ulong sourceNetworkObjectId)
    {
        Debug.Log($"Server Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
        TestClientRpc(value, sourceNetworkObjectId);
    }

    // ClientRpc can only be called on the server but, the code will be executed on all the clients.
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
