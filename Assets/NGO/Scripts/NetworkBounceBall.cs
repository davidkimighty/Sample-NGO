using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkBounceBall : NetworkBehaviour
{
    [SerializeField] private InputActionProperty _fireAction;
    [SerializeField] private GameObject _ballPrefab = null;
    [SerializeField] private Transform _ballSpawnPoint = null;

    private void OnEnable()
    {
        _fireAction.action.performed += BounceBall;
    }

    private void OnDisable()
    {
        _fireAction.action.performed -= BounceBall;
    }

    #region Subscribers
    private void BounceBall(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        SpawnBall();
        RequestBounceBallServerRpc();
    }

    #endregion

    #region Network
    [ServerRpc]
    private void RequestBounceBallServerRpc()
    {
        ExecuteBounceBallClientRpc();
    }

    [ClientRpc]
    private void ExecuteBounceBallClientRpc()
    {
        if (IsOwner) return;
        SpawnBall();
    }

    #endregion

    private void SpawnBall()
    {
        GameObject ball = Instantiate(_ballPrefab, _ballSpawnPoint);
        ball.transform.localPosition = Vector3.zero;
    }
}
