using System;
using System.Text;

// Bağlantı onayı verisi (NGO ConnectionData) ve lobi şifresi. Veri: ilk 8 bayt oyuncunun kimliği (Steam yolunda
// SteamId, Local Debug'da süreç kimliği — RoleManager rejoin eşleştirmesi için), ardından varsa UTF-8 şifre.
// Şifreyi SUNUCU doğrular (RoleManager.HandleConnectionApproval): istemcinin "şifreyi bildim" demesi yetmez. Şifre
// Steam lobi verisine YAZILMAZ; lobi verisinde yalnızca "şifreli" işareti durur (liste kilit göstersin diye).
public static class LobbyAccess
{
    // Bu makine host ise kurduğu lobinin şifresi; boş = şifresiz. Lobiden çıkınca temizlenir.
    public static string HostPassword { get; set; }

    public static byte[] BuildPayload(ulong id, string password)
    {
        var idBytes = BitConverter.GetBytes(id);
        if (string.IsNullOrEmpty(password))
            return idBytes;

        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var payload = new byte[idBytes.Length + passwordBytes.Length];
        Buffer.BlockCopy(idBytes, 0, payload, 0, idBytes.Length);
        Buffer.BlockCopy(passwordBytes, 0, payload, idBytes.Length, passwordBytes.Length);
        return payload;
    }

    public static ulong ReadId(byte[] payload)
    {
        return payload == null || payload.Length < sizeof(ulong) ? 0 : BitConverter.ToUInt64(payload, 0);
    }

    // Sunucu: gelen bağlantının şifresi bu lobinin şifresiyle aynı mı (lobi şifresizse her bağlantı geçer).
    public static bool IsPasswordAccepted(byte[] payload)
    {
        if (string.IsNullOrEmpty(HostPassword))
            return true;

        if (payload == null || payload.Length <= sizeof(ulong))
            return false;

        string given = Encoding.UTF8.GetString(payload, sizeof(ulong), payload.Length - sizeof(ulong));
        return string.Equals(given, HostPassword, StringComparison.Ordinal);
    }
}
