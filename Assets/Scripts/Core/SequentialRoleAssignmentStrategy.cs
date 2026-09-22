// Varsayilan rol atama stratejisi: katilim sirasina gore sabit bir dagitim yapar.
// Ileride bir lobi rol-secim ekrani eklenirse, RoleManager degil sadece bu strateji
// (veya cagiran taraf) degistirilir.
//
// Sira artik CAGIRANDAN (RoleManager.testRoleJoinOrder, Inspector) gelir — bu BIR LOBI ROL
// SECIMI DEGILDIR (oyuncuya sunulan bir UI yok); tek amaci, MPPM'de host'un HER ZAMAN Sef
// olmasi yuzunden Sef'e ozel istasyonlarin ISTEMCI yolunun hic test edilememesidir (bkz.
// CLAUDE.md Temizlik Borcu "Test bosluğu"). Dizi bos/null gelirse (savunma amacli) bugunku
// sabit sira kullanilir.
public class SequentialRoleAssignmentStrategy : IRoleAssignmentStrategy
{
    private static readonly PlayerRole[] DefaultJoinOrder =
    {
        PlayerRole.Sef,
        PlayerRole.Komi,
        PlayerRole.Kasiyer
    };

    private readonly PlayerRole[] _joinOrder;

    public SequentialRoleAssignmentStrategy(PlayerRole[] joinOrder = null)
    {
        _joinOrder = joinOrder != null && joinOrder.Length > 0 ? joinOrder : DefaultJoinOrder;
    }

    public PlayerRole AssignRole(ulong clientId, int joinOrderIndex)
    {
        if (joinOrderIndex < 0 || joinOrderIndex >= _joinOrder.Length)
            return PlayerRole.None;

        return _joinOrder[joinOrderIndex];
    }
}
