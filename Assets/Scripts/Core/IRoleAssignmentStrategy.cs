public interface IRoleAssignmentStrategy
{
    PlayerRole AssignRole(ulong clientId, int joinOrderIndex);
}
