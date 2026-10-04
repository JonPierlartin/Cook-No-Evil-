using System;
using UnityEngine;
using UnityEngine.UI;

// Lobi listesindeki tek satır: lobi adı, oyuncu sayısı, şifreli işareti ve "Katıl" düğmesi. Yalnızca gösterir;
// katılma kararı ve şifre sorma LobbyBrowserUI'dadır.
public class LobbyListRow : MonoBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private Text countText;
    [Tooltip("Lobi şifreliyse görünen işaret.")]
    [SerializeField] private GameObject lockMark;
    [SerializeField] private Button joinButton;

    public void Bind(SteamLobbyManager.LobbyInfo info, Action<SteamLobbyManager.LobbyInfo> onJoin)
    {
        nameText.text = info.Name;
        countText.text = $"{info.Members}/{info.MaxMembers}";
        lockMark.SetActive(info.Locked);
        joinButton.interactable = info.Members < info.MaxMembers;
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => onJoin(info));
    }
}
