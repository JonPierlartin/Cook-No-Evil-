// Round'un tek otoriteli yasam dongusu (bkz. GameLoopManager). RoleManager.IsRoundActive
// ve GameLoopManager.IsGamePaused'un birbirinden habersiz iki ayri bayrak olmasindan
// kaynaklanan riskleri (disconnect sirasinda sipariş süresinin işlemeye devam etmesi,
// pause sirasinda round bitişinin tetiklenebilmesi) onlemek icin tek NetworkVariable
// uzerinden senkronize edilir. RoundEnded'a gecis mantigi Bilesen 2 ile gelecek —
// simdilik hicbir kod bu state'e gecmiyor, sadece makine burada tanimli.
public enum RoundState
{
    Lobby,
    RoundActive,
    RoundEnded
}
