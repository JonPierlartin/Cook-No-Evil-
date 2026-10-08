# Cook No Evil! — Karakter FBX paketi

Kadro L1: **Komi** (K1 Konik Tıkaç) · **Şef** (H1 Göz Bandı) · **Kasiyer** (R1 Fermuar). Modeller onaylanan toon önizlemelerden (4. tur + ince kaşlar) birebir çıkarıldı; `onizleme/01_Konsept_vs_FBX.png` ikisini yan yana gösteriyor.

## İçerik

| Dosya | Ne |
|---|---|
| `fbx/CNE_Komi.fbx`, `fbx/CNE_Sef.fbx`, `fbx/CNE_Kasiyer.fbx` | Binary FBX 7.4; mesh + iskelet + skin + blendshape + materyal, palet dokusu gömülü |
| `textures/CNE_Karakter_Palet_256.png` | Palet dokusu (256×256, 16 px hücre). Yeni iki hücre: **S9·K3 #C42E35** (Komi kırmızısı), **S9·K4 #2F2E3E** (ayakkabı siyahı). Ana palete eklenmeli. |
| `manifest.json` | Kemik hiyerarşisi, mesh/üçgen sayıları, blendshape adları, materyaller, kullanılan palet hücreleri, ölçüler, bakış limitleri |
| `onizleme/` | Doğrudan FBX'ten render: ortak göz hizası, konsept↔FBX, dönüş, topoloji, rig testi, yüz animasyonu |

## Ortak kurallar

- **Ölçek ve eksen:** 1 birim = 1 m, Y yukarı, karakter +Z'ye bakar, zemin y = 0. Gövdeler y = 0,45 m'den başlar; eller ve ayakkabılar gövdeden ayrık (yüzen eller).
- **Göz hizası:** Komi ve Kasiyer'in göz merkezi 1,395 m. Şef'in bandı 1,17–1,40 m.
- **UV:** Her yüzey tek bir palet hücresinin ortasında; boş hücre (#FF00FF) kullanan yüz yok.
- **Vertex color:** R = AO (G = B = R, A = 1) → CNE/Toon `_VertexAOStrength`. Normal map yok, normaller geometrik.
- **Mesh'ler (karakter başına):** `*_Body`, `*_Face` (Komi, Kasiyer), `*_Glove_L`, `*_Glove_R`, `*_Shoes`. Eldivenler her el için ayrı renderer: Kasiyer'in el sinyali tek eldivene `_BaseColor` override'ı ile verilebilir (eldiven hücresi beyaz olduğu için renk birebir çıkar).
- **Poligon:** Komi 27,1 bin, Şef 43,4 bin, Kasiyer 43,8 bin üçgen; quad ağırlıklı. Eldivenlerde bir vertex en fazla 3 kemikten etkilenir (eklem geçişleri); diğer bütün parçalar tek kemiğe sert bağlı.

### Materyaller

| Materyal | Kullanım | CNE/Toon ayarı |
|---|---|---|
| `MI_Char_Palette` | Gövdeler, yüz, ayakkabılar | Rim açık. Ayakkabı siyahı S9·K4 için `_PropMap` R'de küçük specular. |
| `MI_Char_Glove` | Eldivenler | Renderer'da Receive Shadows kapalı; `_BaseColor` sinyal rengi için. |
| `MI_Char_Chrome` | Kasiyer: ekran çerçevesi, tuş sapları, ray, kol, fermuar sürgüsü/çekeceği | Matcap; `_PropMap` R = S5·K0 |
| `MI_Char_Steel` | Kasiyer: fermuar bandı | Specular; `_PropMap` R = S5·K3 |

Karakterleri **Outline** rendering layer'ına koyun (CNEOutlineFeature).

## Unity import ayarları

**Model:** Scale Factor 1, Convert Units açık · Import BlendShapes açık · Mesh Compression Off · Optimize Mesh açık · Normals: **Import** · Blend Shape Normals: **Import** · Legacy Blend Shape Normals kapalı · Tangents: varsayılan (CNE/Toon normal map kullanmadığı için None da olur) · Generate Lightmap UVs kapalı (karakterler light probe ile aydınlanır).

**Rig:** Animation Type **Generic** · Avatar: Create From This Model · Root node: **Root** · Skin Weights: Standard (4 Bones) · Strip Bones kapalı · Optimize Game Objects kapalı (göz, kaş, kol ve tuş kemiklerine koddan erişilebilsin).

**Animation:** Import Animation kapalı (dosyalarda klip yok).

**Materials:** Projede dört materyali (yukarıdaki adlarla, shader CNE/Toon) oluşturup *On Demand Remap → Search and Remap* ile eşleyin ya da *Extract Materials* ile çıkarıp shader'ı değiştirin. Doku olarak projedeki ana paleti kullanın (mipmap kapalı, Bilinear, sıkıştırma yok, Clamp, sRGB açık); gömülü kopya yalnızca yedek.

## İskelet

Ortak: `Root` (zemin, root motion) → `Body` (gövdenin alt yüzü, y 0,45) · `Hand_L/R` → `Index/Middle/Ring/Pinky/Thumb_1..3_L/R` · `Foot_L/R`. Eller ve ayaklar Root'a bağlı; gövde squash'ı (Body ölçeği) elleri bozmaz.

| Karakter | Gövde altı kemikler |
|---|---|
| Komi | `Cap` (kapak + uç) · `Eye_L/R` · `Brow_L/R` · `Mouth` · `Ear_L/R` (tıkaçlar) |
| Şef | `Stack_1` (alt köfte + peynir) → `Stack_2` (üst köfte + peynir) → `Stack_3` (domates + marul) → `Head` (üst ekmek, bant, düğüm) → `Tail_L/R` (bant uçları). Katmanlara küçük dönüş/kaydırma = sallanan burger. |
| Kasiyer | `Head` (kafa kutusu + ekran; kapak gibi kalkar) → `Eye_L/R`, `Brow_L/R` · `Crank` (yan kol) · `Key_1..7` (tuşlar) |

Eksen notları:
- **Parmaklar:** Y parmak boyunca; kıvırma local X etrafında (+).
- **Eye_L/R:** Y = bakış yönü (rest'te bebekler göz akının ortasında). Yatay bakış local Z, dikey bakış local X etrafında.
- **Crank:** Y = mil; çevirme local Y etrafında. **Key_n:** Y = tuş sapı; basma local X etrafında (+25° civarı).

## Yüz animasyonu

Göz kapağı yok; kırpma **ezilerek kapanma**: göz akı yatay bir çizgiye ezilir, bebek ve parıltı içine küçülür. Ara değerler yarı kapalı göz verir.

| Mesh | Blendshape'ler |
|---|---|
| `Komi_Face` (17) | `Blink_L`, `Blink_R` · `Brow_L_*`, `Brow_R_*` (Angry, Sad, Flat, Arch) · `Mouth_Open`, `Mouth_O`, `Mouth_Frown`, `Mouth_Flat`, `Mouth_Wide`, `Mouth_Smirk_L`, `Mouth_Smirk_R` |
| `Kasiyer_Face` (10) | `Blink_L`, `Blink_R` · `Brow_L_*`, `Brow_R_*` (Angry, Sad, Flat, Arch) — ağız (fermuar) sabit |
| Şef | Yok (bant sabit, uçları Tail kemikleriyle) |

- Kaşlar blendshape (şekil) + kemik (kaldır/indir/eğ) birlikte kullanılabilir.
- Ağız şekilleri dinlenme gülümsemesinden morph eder; `Mouth_Open` ile `Mouth_Smirk_L/R` karışabilir, `Mouth_O` ve `Mouth_Frown` tek başına daha temiz.
- **Bakış limitleri** (göz bebeği tamamen görünür): Komi yatay ±16°, dikey ±19° (ötesinde bebek iç köşede şişenin arkasına kısmen girer); Kasiyer ±25°.
- Konseptteki yan bakış rest pozda değil; Eye kemikleriyle verilir (Kasiyer: karakterin soluna 8°, aşağı 8°).

## Blender'da açmak (animatör)

File › Import › FBX, Scale 1.0, *Automatic Bone Orientation* kapalı, Primary/Secondary Bone Axis Y/X (varsayılan). Kök Null (`CNE_Komi` vb.) armatür olur; yüz mesh'leri shape key'lerle gelir. Unity'ye geri dönüşte ekibin mevcut Blender → Unity export ayarını kullanın ve ilk klipte karakterin aynı yöne baktığını kontrol edin.

## Doğrulama

- Üç dosya bağımsız bir FBX okuyucusuyla (ufbx, strict mod) açıldı: uyarı yok; bind pose ile rest pozu aynı; rest'te skinning mesh'i birebir veriyor; ağırlık toplamları 1; blendshape'ler normal farklarıyla.
- `onizleme/` görselleri dışa aktarılan dosyalardan okunup render edildi.

## Açık konular

- Komi'nin gözü (r 0,07 m) Kasiyer'inkinden (0,057 m) büyük; konseptteki gibi bırakıldı.
- Şef'in bandının merkezi (1,285 m) ortak göz hizasının ~11 cm altında; konseptteki gibi.
- Ayakkabı ve eldiven üç karakterde aynı mesh; yalnızca konumları farklı.
