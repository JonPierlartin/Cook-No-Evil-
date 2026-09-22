using System.Collections.Generic;
using UnityEngine;

// K5/GDD 5.2.1: bir ServerProgress'in gectigi fazlarin SIRALI listesi. Veri odakli — kac faz
// oldugunu ve her fazin suresini kod degil bu asset belirler. Ayni tip (pisirme: Cig->Pismis->
// Yanmis) farkli urunler (et, patates, ekstra) icin farkli sureli AYRI profil asset'leri olarak
// var olabilir; tek-fazli bir profil (orn. "Dolu" tek fazi) icecek/dondurma dolumunu da temsil
// edebilir (bkz. ServerProgress dosya basi notu).
[CreateAssetMenu(fileName = "ProgressProfile", menuName = "Cook No Evil/Progress Profile")]
public class ProgressProfile : ScriptableObject
{
    [Tooltip("Sirali faz listesi. En az 1 faz olmali. Son faz TERMINALDIR — ilerleme oraya ulasinca durur (GDD: 'Yanmis son fazdir, ilerleme orada durur'; ayni kural icecek dolumunun 'tepe noktada otomatik durur' kuralini da karsilar).")]
    [SerializeField] private ProgressPhase[] phases;

    public IReadOnlyList<ProgressPhase> Phases => phases;
}
