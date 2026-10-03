// Round'un tek otoriteli yaşam döngüsü (bkz. GameLoopManager). Tek NetworkVariable üzerinden senkronize edilir.
public enum RoundState
{
    Lobby,
    RoundActive,
    // Bölüm bitti (kazanıldı ya da kaybedildi — bkz. RoundOutcome); sonuç ekranı gösterilir.
    RoundEnded
}

// Biten bölümün sonucu (GDD 3.4). Yalnızca sunucu yazar.
public enum RoundOutcome : byte
{
    None,
    Won,
    Lost
}
