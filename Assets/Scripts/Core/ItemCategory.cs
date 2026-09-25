// Öğe türünün hamburger birleştirmedeki kategorisi (GDD 6.7.3). Sıra kuralı bu bilgiye dayanır:
// Alt ekmek → Protein → Garnitür → Sos → Üst ekmek. Ekmek tek kategoridir; alt mı üst mü olduğu
// yığının boş olup olmamasından çıkar (BurgerAssemblyStation). Yok: birleştirmeye girmeyen tür.
public enum ItemCategory
{
    Yok,
    Ekmek,
    Protein,
    Garnitur,
    Sos
}
