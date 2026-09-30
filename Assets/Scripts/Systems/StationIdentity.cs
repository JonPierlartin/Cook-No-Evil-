using UnityEngine;

// Sahnedeki makine / malzeme kabı nesnesinin asset kimliği (bkz. StationId). Açma/kapama davranışı yok (Faz 0'da
// hepsi aktif); yalnızca LevelConfig'in "aktif makineler" listesinin sahneyle eşleşebilmesi için.
[DisallowMultipleComponent]
public class StationIdentity : MonoBehaviour
{
    [SerializeField] private StationId id;

    public StationId Id => id;
}
