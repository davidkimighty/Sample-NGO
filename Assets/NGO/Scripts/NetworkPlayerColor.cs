using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerColor : NetworkBehaviour
{
    [SerializeField] private MeshRenderer _renderer = null;

    private readonly NetworkVariable<Color> _networkColor = new();
    private readonly Color[] _colors = { Color.yellow, Color.green, Color.cyan, Color.blue };
    private int _index = 0;

    private void Awake()
    {
        _networkColor.OnValueChanged += ChangeColor;
    }

    public override void OnDestroy()
    {
        _networkColor.OnValueChanged -= ChangeColor;
    }

    #region Subscribers
    private void ChangeColor(Color prev, Color next) => _renderer.material.color = next;

    #endregion

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _index = (int)OwnerClientId;
            ChangeColorServerRpc(GetNextColor());
        }
        else
            _renderer.material.color = _networkColor.Value;
    }

    #region Network
    [ServerRpc]
    private void ChangeColorServerRpc(Color color) => _networkColor.Value = color;

    #endregion

    private Color GetNextColor() => _colors[_index++ % _colors.Length];
}
