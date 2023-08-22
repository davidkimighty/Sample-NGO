using Broccollie.Core;
using Broccollie.Game;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private Rigidbody _playerBody = null;
    [SerializeField] private PlayerControllerPhysics _playerController = null;
    [SerializeField] private GameObject _cameraHolder = null;

    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private Vector3 _rayDir = Vector3.down;
    [SerializeField] private float _floatHeight = 1.3f;
    [SerializeField] private float _rayLength = 1.5f;
    [SerializeField] private float _rayStartPointOffset = 0.5f;
    [SerializeField] private float _springStrength = 500f;
    [SerializeField] private float _springDamper = 50f;

    [SerializeField] private float _maxSpeed = 6f;
    [SerializeField] private float _acceleration = 150f;
    [SerializeField] private float _maxAcceleration = 200f;

    [SerializeField] private float _rotationStrength = 100f;
    [SerializeField] private float _rotationDamper = 10f;

    private bool _initialized = false;
    private NetworkVariable<PlayerNetworkData> _playerNetworkData = new(writePerm: NetworkVariableWritePermission.Owner);
    private bool _grounded = true;
    private Vector3 _targetVel = Vector3.zero;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Destroy(_playerController);
            Destroy(_cameraHolder);
        }
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;

        if (IsOwner)
        {
            _playerNetworkData.Value = new PlayerNetworkData()
            {
                Position = _playerBody.position,
                Rotation = _playerBody.rotation
            };
        }
    }

    private void FixedUpdate()
    {
        if (IsOwner) return;

        (bool isHit, RaycastHit rayHit) = CastRay();
        Floating(isHit, rayHit);
        Movement();
        Rotation();
    }

    private (bool, RaycastHit) CastRay()
    {
        Vector3 startPoint = _playerBody.position;
        startPoint.y += _rayStartPointOffset;
        Ray ray = new(startPoint, Vector3.down);
        bool isHit = Physics.Raycast(ray, out RaycastHit rayHit, _rayLength + _rayStartPointOffset, ~_playerLayer);
        return (isHit, rayHit);
    }

    private void Floating(bool isHit, RaycastHit rayHit)
    {
        if (!isHit)
        {
            if (_grounded)
                _grounded = false;
            return;
        }

        _grounded = rayHit.distance <= _floatHeight + _rayStartPointOffset;
        if (_grounded)
        {
            Vector3 vel = _playerBody.velocity;
            Vector3 hitVel = rayHit.rigidbody != null ? rayHit.rigidbody.velocity : Vector3.zero;

            float rayDirVel = Vector3.Dot(_rayDir, vel);
            float hitRayDirVel = Vector3.Dot(_rayDir, hitVel);
            float relVel = rayDirVel - hitRayDirVel;
            float offset = rayHit.distance - _floatHeight - _rayStartPointOffset;
            float springForce = (offset * _springStrength) - (relVel * _springDamper);
            _playerBody.AddForce(_rayDir * springForce);
        }
    }

    private void Movement()
    {
        _playerBody.position = Vector3.Lerp(_playerBody.position, _playerNetworkData.Value.Position, 0.1f);
    }

    private void Rotation()
    {
        Quaternion currentRotation = _playerBody.rotation;
        Quaternion targetRotation = Helper.ShortestRotation(_playerNetworkData.Value.Rotation, currentRotation);

        targetRotation.ToAngleAxis(out float angle, out Vector3 axis);
        float rotationRadians = angle * Mathf.Deg2Rad;
        _playerBody.AddTorque((axis.normalized * (rotationRadians * _rotationStrength)) - (_playerBody.angularVelocity * _rotationDamper));
    }

    private struct PlayerNetworkData : INetworkSerializable
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
        }
    }
}
