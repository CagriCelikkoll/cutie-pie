# Cutie Pie – Proje Rehberi (Claude için)

Bu dosya, projede çalışan Claude'un her oturumda okuduğu proje özetidir. Kararlar değiştikçe güncelleyin.

## Oyun

Cutie Pie, yatay ekranlı (landscape), 2D, sevimli tarzda bir mobil pastane oyunudur. Hedef platform Android (Google Play), ileride iOS.
Hedef kitle çocuklar dahil karma kitledir; Google Play aile (Families) politikasına uyulur.

Oyunun 3 bölümü vardır:

1. **Salon (satış):** 3 dakikalık round'lar. Masalara oturan müşterilere pasta dilimi + içecek servisi yapılır.
   Müşterinin üstünde sabır barı vardır; biterse müşteri gider (kaçırma). 3 kaçırmada round biter.
   Müşteri geliş hızı round boyunca artar. Hızlı teslimde bahşiş, art arda doğru teslimde kombo çarpanı vardır.
   Round sonunda kazanç ve 1–3 yıldız gösterilir.
2. **Mutfak:** (a) Salon stoğunu üretir: tarif fırına konur, gerçek zamanlı 1–5 dk pişer (oyun kapalıyken de),
   fırın başına tek parti. (b) Özel sipariş: fişteki pastanın aynısı adım adım yapılır, benzerlik yüzdesi
   hesaplanır, paketlenir, onaylanır, altın kazanılır.
3. **Vitrin:** Temaya göre serbest pasta tasarımı; 1–5 yıldız puanlanır, pastane puanını yükseltir, dekor ödülü verir.

Kaynaklar: altın (harcanır), pastane puanı (kilit açar), elmas (premium).
Dil: Türkçe ve İngilizce, ayarlardan seçilir.

## Teknik kararlar

- Motor: Unity 2022.3 LTS, C#, 2D (URP 2D). Yatay ekran: Default Orientation = Auto Rotation, yalnızca Landscape Left ve Landscape Right açık.
  Unity 6'ya özel API veya özellik kullanma (geliştiricilerden birinin bilgisayarı Unity 6'yı desteklemiyor);
  önerdiğin her şeyin 2022.3'te çalıştığından emin ol.
- Salon düzeni: müşteri girişi sağ üstte. Ekranın solu hazırlama alanı (üstte pasta vitrini, ortada içecek makineleri,
  altta tepsi/tabak yığınları, hazırlık alanı ve çöp kutusu), sağı 8 masa yeri. Açık masa sayısı LevelData'dan gelir
  (başta 3), diğer masalar kilitli görünür ve geliştirmeyle açılır.
- Servis 5 sürüklemedir: tepsi → tabak → vitrinden tek dilim → içecek bardağı → tepsiyi masaya. Müşteriye dokununca
  siparişi hazırlık alanındaki fişe düşer. Vitrin ve içecekler 4'lü sabit yerdir, fazlası sayfalanır; round menüsü ilk sayfadadır.
- Android: Google Play'in güncel hedef API şartını karşılamak için Target API Level en az 36 olmalı.
- Klasör yapısı:
  - `Assets/_Project/Scripts/{Core, Salon, Kitchen, Showcase, Upgrades, Ads, UI}`
  - `Assets/_Project/{Data, Prefabs, Scenes, Art, Audio, Localization}`
- Sahneler: Boot, Shop (ana ekran), Salon, Kitchen, Showcase.
- Akış: Boot → Shop (açılış ekranı: Vitrin, Kasa, Mutfak butonları; Faz 1'de sadece Kasa çalışır) → Kasa → Salon.
  Round sonundan "Pastaneye dön" ile Shop'a dönülür.
- Tüm denge değerleri (fiyat, süre, müşteri hızı, ödüller) ScriptableObject'lerde tutulur; kodda sihirli sayı olmaz.
- Sistemler birbirine C# event'leriyle bağlanır. Singleton yöneticiler yalnızca Core'da
  (GameManager, EconomyManager, SaveSystem, AudioManager).
- Salon ile Mutfak doğrudan konuşmaz; ortak stok StockManager üzerinden yönetilir.
- Kayıt: JSON, yerel. Gerçek zamanlı süreler başlangıç zaman damgasıyla hesaplanır; cihaz saati geri alınırsa
  süre ilerlemez, ileri alınırsa en fazla bir parti hazır olur.
- Reklamlar `IAdsService` arayüzü arkasındadır. Geliştirmede `FakeAdsService`, yayında `AdMobAdsService`.
  Aile politikası: çocuklara ve yaşı bilinmeyenlere yalnızca çocuk uyumlu, kişiselleştirilmemiş reklam;
  açılışta tarafsız yaş sorusu (doğum yılı).
- Görseller şimdilik basit renkli sprite/kutu yer tutucularıdır.

## Ekip, sahiplik ve Git

İki geliştirici var, ikisi de Claude Code ile çalışıyor. Her kişi yalnızca kendi alanındaki dosyaları değiştirir.

| Alan | Oynanış geliştiricisi | Altyapı geliştiricisi |
| --- | --- | --- |
| Kod | `Scripts/Salon`, `Scripts/UI`, `Scripts/Showcase` | `Scripts/Core`, `Scripts/Upgrades`, `Scripts/Ads`, `Scripts/Kitchen` |
| Sahneler | `Salon.unity`, `Showcase.unity` | `Boot.unity`, `Shop.unity`, `Kitchen.unity` |

- Dal adları `rol/gorev-no-kisa-ad` biçimindedir: `oynanis/...` oynanış geliştiricisi, `altyapi/...` altyapı geliştiricisi,
  `contract/...` ortak sözleşme değişikliği, `fix/...` acil düzeltme. Görev başında dal adından alanı anla; belli değilse kullanıcıya sor.
- Ortak sözleşme (GameEvents olayları, Data sınıfları, sahne/prefab kuralları) "Cutie Pie – Faz 1 Görev Planı" dokümanında tanımlıdır;
  sözleşmenin dışına çıkan bir değişiklik gerekiyorsa kodlamadan önce kullanıcıya söyle.
- Altyapının Salon'da durması gereken parçaları (RoundManager vb.) prefab olarak hazırlanır; `Salon.unity`'ye sahneyi yalnızca oynanış geliştiricisi ekler.
- Diğer kişinin klasörüne veya sahnesine dokunman gerekiyorsa kodlamadan önce söyle; gerekiyorsa değişikliği prefab olarak hazırla.
- Bölümler arası iletişim `Core/GameEvents` üzerinden olur. GameEvents'e yeni olay eklemek ortak karardır; ekleme önerisini kullanıcıya bildir.
- Ortak dosyalar (`CLAUDE.md`, `ProjectSettings/`, `Packages/manifest.json`) konuşulmadan değiştirilmez.
- Her görev ayrı dalda yapılır, `main`'e doğrudan commit atılmaz; iş pull request ile alınır.
- `.meta` dosyaları her zaman commit edilir. `Library/`, `Temp/`, `Logs/`, `UserSettings/` ve build dosyaları depoya girmez.
- Görsel, ses ve font dosyaları Git LFS ile takip edilir.
- Sahne veya prefab dosyasında Git çakışması olursa elle birleştirme önerme; kullanıcıya bir sürümü seçmesini söyle.

## Claude'dan beklenenler

- Her görevin sonunda: oluşturulan/değiştirilen dosyaların listesi ve Unity editöründe yapılması gerekenler
  (sahneye ne eklenecek, hangi alan atanacak) adım adım yazılsın.
- Kod yorumları Türkçe olsun; sınıf, metot ve değişken adları İngilizce olsun.
- Oyuncuya görünen tüm metinler yerelleştirme dosyasından gelsin, koda yazılmasın.
- Büyük görevlerde önce kısa bir plan çıkarılsın, onaydan sonra kodlansın.
- Yeni paket eklemeden önce sorulsun.

## Sözleşme ve Git notları

- Ortak sözleşmenin güncel kaynağı koddur: `Scripts/Core/GameEvents.cs` ve `Scripts/Core/Data`. Faz 1 Görev Planı ile çelişirse kod esas alınır; farkı kullanıcıya bildir.
- Proje dokümanları (PDF'ler) depoda tutulmaz; ekip içinde ayrıca paylaşılır. Yerelde `docs/` klasöründe olabilir.
- Reklama yalnızca `GameManager.Instance.Ads` üzerinden erişilir; `FakeAdsService` veya `AdMobAdsService` doğrudan kullanılmaz.
- Dal açma, `git add`, commit, push ve merge işlemlerini kullanıcı yapar. Görev sonunda yalnızca önerilen dal adını, commit summary ve description metnini, PR başlığı ve açıklamasını ver.
- Commit mesajları Türkçe olur; commit'lere ve PR açıklamalarına `Co-Authored-By` satırı veya Claude/Claude Code ibaresi eklenmez.
