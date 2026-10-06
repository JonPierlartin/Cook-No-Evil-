using UnityEditor;
using UnityEngine;

// CNE/Toon materyal inspector'ı: özellikleri gruplar ve her alana Türkçe ipucu koyar. Aç/kapa kutucukları shader'daki
// [Toggle(...)] özellikleridir (anahtar kelimeyi Unity yönetir); burada yalnızca kapalı grubun alanları gizlenir.
public class CNEToonShaderGUI : ShaderGUI
{
    private static readonly GUIContent Content = new();

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        Header("Renk");
        Draw(editor, properties, "_BaseMap", "Palet dokusu", "Renklerin okunduğu palet dokusu (sRGB). Normal map değildir; yüzey ayrıntısı taşımaz.");
        Draw(editor, properties, "_BaseColor", "Renk", "Palet rengiyle çarpılır. Oyun kodu bunu renderer başına değiştirir (pişmişlik, sinyal rengi).");
        Draw(editor, properties, "_PropMap", "Özellik maskesi", "Paletle aynı düzende, linear doku. R = parlama, G = emission, B = desen. Atanmazsa hiçbiri uygulanmaz.");
        if (Toggle(editor, properties, "_AlphaClip", "Alpha clip", "Palet dokusunun alfasına göre pikselleri atar. Varsayılan kapalı."))
            Draw(editor, properties, "_Cutoff", "Alpha eşiği", "Alfası bunun altındaki pikseller çizilmez.");

        Header("Işık");
        Draw(editor, properties, "_Wrap", "Işık sarması", "0,5 = half-Lambert. Büyüdükçe ışık yüzeyin arkasına doğru sarar, gölge alanı küçülür.");
        Draw(editor, properties, "_ShadowThreshold", "Gölge eşiği", "Işık ile gölge bandının ayrıldığı yer. Büyüdükçe gölge alanı büyür.");
        Draw(editor, properties, "_ShadowSoftness", "Geçiş yumuşaklığı", "Bantlar arasındaki geçişin genişliği. Küçük = keskin çizgi film kenarı.");
        if (Toggle(editor, properties, "_MidBand", "Ara bant", "Işık ile gölge arasına üçüncü bir ton ekler."))
        {
            Draw(editor, properties, "_MidThreshold", "Ara bant eşiği", "Ara tonun tam ışığa döndüğü yer. Gölge eşiğinden büyük olmalı.");
            Draw(editor, properties, "_MidStrength", "Ara bant koyuluğu", "Ara tonun tam ışığa göre ne kadar koyu olduğu.");
        }
        Draw(editor, properties, "_ShadowTint", "Gölge rengi", "Gölgedeki yüzey bu renge doğru çarpılır. Siyah değil, soğuk bir renk olmalı.");
        Draw(editor, properties, "_ShadowStrength", "Gölge gücü", "0 = gölge yok, 1 = yüzey tamamen gölge rengiyle çarpılır.");
        Draw(editor, properties, "_GIStrength", "Dolaylı ışık gücü", "Lightmap, light probe ve ortam ışığının katkısı. Basamaksız eklenir.");
        Draw(editor, properties, "_LightClamp", "Işık üst sınırı", "Toplam ışık bunu geçemez; lambaların yanında palet renkleri patlamaz.");

        Header("Parlama");
        if (Toggle(editor, properties, "_Specular", "Parıltı", "Keskin kenarlı küçük parıltı (paslanmaz çelik). Yalnızca aydınlık bantta ve maskenin R kanalında."))
        {
            Draw(editor, properties, "_SpecColor", "Parıltı rengi", "Parıltının rengi; ışığın rengiyle çarpılır.");
            Draw(editor, properties, "_SpecSize", "Parıltı boyutu", "Büyüdükçe parıltı lekesi büyür.");
            Draw(editor, properties, "_SpecSoftness", "Parıltı kenarı", "Lekenin kenar yumuşaklığı. Küçük = keskin.");
        }
        if (Toggle(editor, properties, "_MatCap", "Matcap", "Kameraya göre sabit bir yansıma dokusu (krom). Maskenin R kanalında."))
        {
            Draw(editor, properties, "_MatCapTex", "Matcap dokusu", "Küre biçiminde çizilmiş yansıma dokusu.");
            Draw(editor, properties, "_MatCapStrength", "Matcap gücü", "1 = maskeli yüzey tamamen matcap rengini alır.");
        }

        Header("Rim (kenar ışığı)");
        if (Toggle(editor, properties, "_Rim", "Kenar ışığı", "Siluete yakın yüzeyde basamaklı parlama. Karakterler için."))
        {
            Draw(editor, properties, "_RimColor", "Renk", "Kenar ışığının rengi.");
            Draw(editor, properties, "_RimThreshold", "Eşik", "Büyüdükçe kenar ışığı incelir.");
            Draw(editor, properties, "_RimSoftness", "Yumuşaklık", "Kenar ışığının iç kenarının yumuşaklığı.");
            Draw(editor, properties, "_RimStrength", "Güç", "Kenar ışığının parlaklığı.");
            Draw(editor, properties, "_RimUpBias", "Yukarı ağırlığı", "1 = yalnızca yukarı bakan yüzeylerde güçlü, 0 = her yönde aynı.");
        }

        Header("Emission");
        if (Toggle(editor, properties, "_Emission", "Emission", "Kendi ışığını veren yüzey (neon). Maskenin G kanalında; bloom alır."))
            Draw(editor, properties, "_EmissionColor", "Emission rengi", "HDR renk. Şiddeti 1'in üstüne çıkınca bloom parlatır.");

        Header("Desen (tarama)");
        if (Toggle(editor, properties, "_Hatch", "Tarama deseni", "Gölge bandına düşük kontrastlı tarama çizgileri. Maskenin B kanalında. Varsayılan kapalı."))
        {
            Draw(editor, properties, "_HatchTex", "Tarama dokusu", "Döşenebilir gri tonlu desen (R kanalı okunur).");
            Draw(editor, properties, "_HatchStrength", "Güç", "Desenin kontrastı. Düşük tutulmalı.");
            Draw(editor, properties, "_HatchScale", "Ölçek", "Desenin metre başına tekrar sayısı (obje uzayında).");
        }

        Header("AO");
        Draw(editor, properties, "_VertexAOStrength", "Vertex AO gücü", "Vertex renginin R kanalındaki (Blender'da pişirilmiş) AO'nun etkisi. Vertex rengi yoksa etkisizdir.");

        Header("Gelişmiş");
        editor.EnableInstancingField();
        editor.RenderQueueField();
    }

    private static void Header(string title)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    private static void Draw(MaterialEditor editor, MaterialProperty[] properties, string name, string label, string tooltip)
    {
        var property = FindProperty(name, properties);
        Content.text = label;
        Content.tooltip = tooltip;

        if (property.propertyType == UnityEngine.Rendering.ShaderPropertyType.Texture)
            editor.TexturePropertySingleLine(Content, property);
        else
            editor.ShaderProperty(property, Content);
    }

    private static bool Toggle(MaterialEditor editor, MaterialProperty[] properties, string name, string label, string tooltip)
    {
        Draw(editor, properties, name, label, tooltip);
        return FindProperty(name, properties).floatValue > 0.5f;
    }
}
