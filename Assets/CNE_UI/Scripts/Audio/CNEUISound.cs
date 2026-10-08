namespace CookNoEvil.UI
{
    /// <summary>
    /// UI ses sozlugu. Sayilar sabittir (ses seti asset'inde int olarak saklanir); yenilerini sona ekleyin.
    /// </summary>
    public enum CNEUISound
    {
        None = 0,
        Hover = 1,         // krom tikirtisi
        Press = 2,         // tiknaz tus "tak"
        Release = 3,       // tus geri donusu
        Confirm = 4,       // servis zili "ding"
        Back = 5,          // yumusak "tok" (geri / vazgec)
        SwitchOn = 6,      // salter "klak" (acik)
        SwitchOff = 7,     // salter "klak" (kapali)
        KnobTick = 8,      // doner dugme kademesi
        SliderTick = 9,    // surgu adimi
        PanelOpen = 10,    // tabela iner
        PanelClose = 11,   // tabela kalkar
        FlagFlap = 12,     // bayrak dalgalanir (dil secimi)
        NeonOn = 13,       // neon yanar
        NeedleDrop = 14,   // plak ignesi (muzik parcasi degisir)
        Error = 15,        // diner buzzer'i
        BoardPress = 16,   // menu panosu harfleri tik tik oturur
        CashRegister = 17  // yazar kasa "ca-cing" (ileride sinyal carki)
    }
}
