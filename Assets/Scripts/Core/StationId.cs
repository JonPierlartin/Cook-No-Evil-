using UnityEngine;

// Haritadaki bir makinenin / malzeme kabının asset tabanlı kimliği. ScriptableObject sahne nesnesine referans
// veremediği için LevelConfig "aktif olanlar"ı bu kimliklerle tutar; sahnedeki nesne kimliğini StationIdentity ile
// taşır (GDD 11.9: oda yerleşimi sabit, makineler hep sahnede; LevelConfig hangilerinin aktif olduğunu belirler).
[CreateAssetMenu(fileName = "StationId", menuName = "Cook No Evil/Station Id")]
public class StationId : ScriptableObject
{
}
