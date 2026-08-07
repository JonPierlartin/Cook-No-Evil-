// Varsayilan rol atama stratejisi: katilim sirasina gore sabit bir dagitim yapar.
// Ileride bir lobi rol-secim ekrani eklenirse, RoleManager degil sadece bu strateji
// (veya cagiran taraf) degistirilir.
public class SequentialRoleAssignmentStrategy : IRoleAssignmentStrategy
{
    private static readonly PlayerRole[] JoinOrder =
    {
        PlayerRole.Sef,
        PlayerRole.Yamak,
        PlayerRole.Kasiyer
    };

    public PlayerRole AssignRole(ulong clientId, int joinOrderIndex)
    {
        if (joinOrderIndex < 0 || joinOrderIndex >= JoinOrder.Length)
            return PlayerRole.None;

        return JoinOrder[joinOrderIndex];
    }
}
