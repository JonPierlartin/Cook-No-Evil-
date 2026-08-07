using System;
using Unity.Netcode;
using UnityEngine;

// Server-authoritative rol atamasi. Atama mantigi IRoleAssignmentStrategy arkasina
// soyutlanmistir (GDD 3, Bilesen 1) — ileride bir lobi rol-secim ekrani eklenirse
// RoleManager'in kendisi degil sadece bu strateji degistirilir.
[RequireComponent(typeof(NetworkObject))]
public class RoleManager : NetworkBehaviour
{
    public static RoleManager Instance { get; private set; }

    public event Action<PlayerRole> OnLocalRoleAssigned;

    private readonly NetworkList<ClientRoleEntry> _assignedRoles = new();
    private IRoleAssignmentStrategy _strategy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _strategy = new SequentialRoleAssignmentStrategy();
    }

    public override void OnNetworkSpawn()
    {
        _assignedRoles.OnListChanged += HandleAssignedRolesChanged;

        if (IsServer)
            NetworkManager.OnClientConnectedCallback += HandleClientConnected;

        // Gec katilan client icin: liste zaten dolu geldiyse kendi rolumuzu hemen bildir.
        var existing = GetRole(NetworkManager.LocalClientId);
        if (existing != PlayerRole.None)
            OnLocalRoleAssigned?.Invoke(existing);
    }

    public override void OnNetworkDespawn()
    {
        _assignedRoles.OnListChanged -= HandleAssignedRolesChanged;

        if (IsServer && NetworkManager != null)
            NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        int joinOrderIndex = _assignedRoles.Count;
        var role = _strategy.AssignRole(clientId, joinOrderIndex);
        _assignedRoles.Add(new ClientRoleEntry(clientId, role));

        Debug.Log($"[RoleManager] Client {clientId} -> {role}");
    }

    private void HandleAssignedRolesChanged(NetworkListEvent<ClientRoleEntry> change)
    {
        if (change.Value.ClientId != NetworkManager.LocalClientId)
            return;

        OnLocalRoleAssigned?.Invoke(change.Value.Role);
    }

    public PlayerRole GetRole(ulong clientId)
    {
        foreach (var entry in _assignedRoles)
        {
            if (entry.ClientId == clientId)
                return entry.Role;
        }

        return PlayerRole.None;
    }

    public PlayerRole LocalRole => NetworkManager == null ? PlayerRole.None : GetRole(NetworkManager.LocalClientId);
}

public readonly struct ClientRoleEntry : IEquatable<ClientRoleEntry>, INetworkSerializeByMemcpy
{
    public readonly ulong ClientId;
    public readonly PlayerRole Role;

    public ClientRoleEntry(ulong clientId, PlayerRole role)
    {
        ClientId = clientId;
        Role = role;
    }

    public bool Equals(ClientRoleEntry other) => ClientId == other.ClientId && Role == other.Role;
    public override bool Equals(object obj) => obj is ClientRoleEntry other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(ClientId, Role);
}
