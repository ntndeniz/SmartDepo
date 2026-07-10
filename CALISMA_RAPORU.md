# Çalışma Raporu — Akıllı Depo Yönetimi

Bu rapor, "Akıllı Depo Yönetimi — Modül Geliştirme Testi" görev tanımının ("İş Bitiminde" bölümü)
istediği beş başlığa göre hazırlanmıştır.

---

## 1. Ne Yapıldığının Kısa Özeti

Çok kiracılı (multi-tenant, `CompanyId` ile şirket bazlı izole edilmiş) bir depo yönetim sistemi
geliştirildi. Uygulama şu uçtan uca akışı destekler:

**Marka/Ürün tanımlama → Mal Kabul (barkod okutarak depoya giriş) → Koli/Konum yönetimi (raf ataması) →
Mağaza Siparişi → Sevkiyat (toplama listesi, koli kapama, paletleme, sevk)** — ve bunların üzerine haftalık
otomatik CSV raporlama, rol tabanlı yetkilendirme (Admin/Personel) ve tam kullanıcı kimlik doğrulaması.

Görev tanımının istediği temel zorunlu senaryo (ürün tanımlama → depoya giriş → depodan çıkış) tamamlandıktan
sonra, "özgün tasarım ve problem çözme yaklaşımı" kriterine karşılık gelecek şekilde kapsam bilinçli olarak
genişletildi: gerçek bir depoda olması beklenen raf/konum takibi, çoklu mağaza sipariş yönetimi, palet bazlı
sevkiyat, kamera ile barkod okuma (`html5-qrcode`) ve rol bazlı erişim kontrolü eklendi.

Frontend tek sayfa (single page) olarak, altı sekme halinde tasarlandı: **Mal Kabul, Konum, Siparişler,
Sevkiyat, Raporlar, Ayarlar** (Ayarlar altında Depo Boyutu / Markalar / Mağazalar alt sekmeleri, ayrıca
"Kullanıcılar" yönetim penceresi). Her sekmede özet bilgi kartları, arama+filtre destekli server-side
sayfalanmış tablo, ekleme/düzenleme modalı (MUI Dialog) ve silme onay modalı bulunuyor.

---

## 2. Kullanılan Teknolojiler ve Versiyonları

| Katman | Teknoloji | Versiyon |
|---|---|---|
| Backend | .NET / ASP.NET Core Web API | 9.0 |
| ORM | Entity Framework Core | 9.0.* (`Microsoft.EntityFrameworkCore`, `.SqlServer`, `.Design`) |
| Kimlik doğrulama | `Microsoft.AspNetCore.Authentication.JwtBearer` | 9.0.0 |
| PDF ayrıştırma | PdfPig | 0.1.10 |
| Veritabanı | MS SQL Server | 2022 (Docker imajı, Azure SQL Edge ile geliştirme ortamında) |
| Frontend | React | 18.3.1 |
| Dil | TypeScript | 6.0.2 |
| UI kütüphanesi | Material-UI (MUI) | 9.2.0 |
| Build aracı | Vite | 8.1.1 |
| HTTP istemcisi | axios | 1.18.1 |
| Barkod okuma (kamera) | html5-qrcode | 2.3.8 |
| Barkod üretme/yazdırma | JsBarcode | 3.12.3 |

Katmanlı mimari: **Controller → Manager → Repository → Entity (DbContext)**.

---

## 3. Karşılaşılan Sorunlar ve Çözüm Yolları

### 3.1 Barkod yazdırma sessizce başarısız oluyordu
**Sorun:** Üretilen bazı barkodlar yazdırma penceresini hiç açmıyordu, hata da görünmüyordu.
**Kök neden:** Barkod metni büyütülürken `.ToUpper()` kullanılıyordu; sunucu Türkçe kültürde çalıştığında
`i` harfi `İ` (U+0130, noktalı büyük I) karakterine dönüşüyor, bu da CODE128 barkod standardında geçersiz
olduğu için `JsBarcode` sessizce istisna fırlatıp hiçbir şey çizmiyordu.
**Çözüm:** Barkod/kısaltma üreten tüm noktalarda `.ToUpper()` yerine kültürden bağımsız `.ToUpperInvariant()`
kullanıldı. Daha sonra, Türkçe'ye özgü diğer karakterlerin (`ı`, `ş`, `ğ`, `ö`, `ü`, `ç`) de
`.ToUpperInvariant()` ile ASCII'ye çevrilmediği ayrıca keşfedildi (örn. `"ı".ToUpperInvariant()` → değişmeden
kalıyor) — bunun için özel bir Türkçe karakter → ASCII eşleme tablosu (`BarcodeText.ToBarcodeSafeUpper`)
yazıldı ve mevcut hatalı barkodları düzelten bir migration eklendi.

### 3.2 Soft-delete + `.Include()` birlikte kullanılınca veri kayboluyordu
**Sorun:** Bir ürün soft-delete edildiğinde, o ürüne bağlı koliler bazı listeleme ekranlarında sessizce
kayboluyordu ("kayıp stok" hissi veriyordu).
**Kök neden:** EF Core, soft-delete için tanımlı global query filter'ı `.Include()` ile çekilen ilişkili
tabloya da uyguluyor; foreign key non-nullable olduğunda bu bir INNER JOIN'e denk geliyor — filtrelenen
taraf (silinmiş ürün) boş döndüğünde, join'in diğer tarafı (koli, fiziksel olarak silinmemiş olsa bile)
de sonuçtan düşüyor.
**Çözüm:** İlgili sorgularda gerekli yerlerde `IgnoreQueryFilters()` bilinçli olarak uygulandı ve bu
davranışın nerede/neden ortaya çıkabileceği kod içinde belgelendi.

### 3.3 Aynı sipariş barkodu birden fazla okutulunca mükerrer kayıt oluşuyordu
**Sorun:** Kullanıcı aynı sipariş barkodunu art arda birden fazla kez okutursa, her seferinde yeni bir
toplama (dispatch) emri oluşuyordu.
**Çözüm:** İlgili oluşturma işlemine, aynı sipariş için zaten açık bir emir olup olmadığını kontrol eden
idempotent bir denetim eklendi — varsa yeni kayıt açılmadan mevcut emir döndürülüyor.

### 3.4 Palet tek-mağaza kısıtı gerçekte çalışmıyordu
**Sorun:** "Bir palete yalnızca aynı mağazanın kolileri eklenebilir" kuralı koddaydı ama pratikte
çalışmıyordu.
**Kök neden:** Doğrulama yapılırken ilgili ilişkinin (`.Include()`) eksik olması nedeniyle mağaza bilgisi
sorguya hiç gelmiyor, kontrol her zaman geçiyordu.
**Çözüm:** Eksik `.Include()` eklendi, kural gerçekten uygulanır hale getirildi.

### 3.5 Server-side pagination bazı ekranlarda sabit bir üst sınırla (1000) sessizce veri kesiyordu
**Çözüm:** Sabit `pageSize` sınırı kaldırılıp gerçek `Skip`/`Take` + `TotalCount` tabanlı sayfalamaya
geçirildi.

### 3.6 Ortam sorunu: geliştirme veritabanı konteyneri kapanınca giriş başarısız oluyordu
**Sorun:** Yerel geliştirme ortamında SQL Server, Docker (Colima) üzerinde çalışıyordu; bilgisayar yeniden
başlatıldığında Colima otomatik açılmadığı için backend veritabanına bağlanamıyor, giriş ekranı "sunucu
hatası" veriyordu.
**Çözüm:** Kök neden zincir halinde izlendi (önce bağlantı reddi, sonra gerçek SQL bağlantı hatası, en
son kimlik doğrulama reddi) ve ortam adım adım yeniden ayağa kaldırıldı; bu durum ileride tekrar
yaşanabileceği için giriş sorunlarında önce ortam/servis durumunun kontrol edilmesi gerektiği not edildi.

---

## 4. Mimari Kararlar ve Nedenleri

- **Controller → Manager → Repository → Entity katmanlı mimarisi**, görev tanımının zorunlu kıldığı
  yapı. Her katmanın tek sorumluluğu var: Controller yalnızca HTTP'yi bilir; Manager iş kurallarını
  (validasyon, tenant izolasyonu, iş akışı durum geçişleri) taşır; Repository yalnızca EF Core LINQ
  sorgusu içerir. Bu ayrım, bir iş kuralının birden fazla yerden (örn. hem API'den hem gelecekte bir
  arka plan görevinden) tutarlı şekilde çağrılabilmesini ve katmanların birbirinden bağımsız test
  edilebilmesini sağlar.
- **Yalnızca GET/POST HTTP metotları.** Güncelleme ve silme işlemleri de `POST /update`, `POST /delete`
  şeklinde tasarlandı; PUT/DELETE hiç kullanılmadı (görev tanımının zorunlu kıldığı, production ortamında
  bu metotların desteklenmediği varsayımına dayalı kısıtlama).
  ```csharp
  [HttpPost("update")]
  public async Task<IActionResult> Update([FromBody] UpdateProductRequest request) { ... }
  ```
- **Multi-tenant güvenlik: `CompanyId` her zaman JWT token'dan okunur, client'tan gelen değere asla
  güvenilmez.** `BaseApiController`, her istekte token'daki `CompanyId` claim'ini `CurrentCompanyId` olarak
  sağlar; her Controller action'ı, DTO üzerindeki `CompanyId` alanını bu değerle ezer. Manager katmanında
  ayrıca, bulunan kaydın `CompanyId`'si mevcut kullanıcının şirketiyle eşleşmiyorsa `Forbid` (403) döner —
  bu, bir kullanıcının başka bir şirketin verisine ID tahmin ederek erişmesini (IDOR) engeller.
- **Soft delete + global query filter.** Her entity `BaseEntity`'den türeyip `IsDeleted` alanını taşır;
  `AppDbContext.OnModelCreating` içinde her entity için `!x.IsDeleted` global query filter'ı tanımlanır —
  böylece her sorguya elle bu koşulu eklemeyi unutma riski ortadan kalkar.
  ```csharp
  public abstract class BaseEntity
  {
      public int Id { get; set; }
      public string CompanyId { get; set; } = default!;
      public bool IsDeleted { get; set; }
  }
  ```
- **Server-side pagination, tüm listeleme uç noktalarında zorunlu.** Ortak bir `PagedRequest`/
  `PagedResponse<T>` DTO çifti tüm modüllerde tekrar kullanıldı (`page`, `pageSize`, `search`, opsiyonel
  `fromDate`/`toDate`); Repository katmanında `Skip((page-1)*pageSize).Take(pageSize)` + ayrı bir
  `CountAsync()` ile toplam sayı hesaplanır. Frontend tüm veriyi çekip istemci tarafında kesmez.
- **PascalCase (backend) ↔ camelCase (frontend) dönüşümü, tek bir noktada (axios interceptor).**
  Backend JSON çıktısını bilinçli olarak PascalCase bırakır (`PropertyNamingPolicy = null`); frontend'deki
  `api/client.ts` içindeki request/response interceptor'lar giden gövdeyi PascalCase'e, gelen yanıtı
  camelCase'e otomatik çevirir — böylece backend C# convention'ını, frontend TypeScript convention'ını
  korur, aradaki dönüşüm hiçbir component'in bilmesi gerekmeyen tek bir merkezi noktada yaşar.
- **Depo içi konum modeli: `Location.CurrentBoxId` ile doğrudan raf–koli ilişkisi.** İlk tasarımda ayrı
  bir `Pallet`/`PalletBox`/`PalletLocationHistory` tablo seti vardı; bu, depo-içi raf ataması ile
  sevkiyat paleti kavramlarını karıştırıyordu. Basitleştirilerek her raf konumunun en fazla bir koli
  tutabildiği, doğrudan referanslı bir model benimsendi; sevkiyat paleti (`DispatchPallet`) tamamen ayrı,
  bağımsız bir kavram olarak yeniden tasarlandı.
- **Toplama (picking) mantığı FIFO ve önce rafta olanı önceliklendirir.** `DispatchManager` bir sipariş
  kalemini karşılarken önce rafta (`OnShelf`) olan kolilerden, sonra stoktaki kolilerden, her ikisinde de
  en eski `CreatedAt` tarihli olandan başlayarak toplar; bir koli yetmezse birden fazla koliden bölerek
  toplamayı destekler ve stok yetersizse kısmi karşılama uyarısı döner.
- **Rol tabanlı yetkilendirme (RBAC), hem backend hem frontend'de, iki katmanlı.** `User.Role`
  (`Admin`/`Staff`) JWT claim'i olarak taşınır; yönetimsel işlemler (marka/mağaza/ürün oluşturma-
  düzenleme-silme, kullanıcı yönetimi) backend'de `[Authorize(Roles = "Admin")]` ile korunur — bu,
  frontend'deki buton gizlemenin yalnızca bir kullanılabilirlik (UX) önlemi olduğunu, gerçek güvenlik
  sınırının API seviyesinde olduğunu garanti eder.

---

## 5. Yapay Zeka Kullanımı

Geliştirme sürecinde Claude (Anthropic) tabanlı bir AI kod asistanı (Claude Code) kullanıldı.

Mimari yapı (Controller/Manager/Repository/Entity katmanları), veritabanı/domain modeli (hangi entity'ler,
hangi ilişkiler), iş akışı tasarımı (mal kabul → konum → sipariş → toplama → paletleme → sevkiyat) ve
frontend'in ekran/sekme yapısı bana ait kararlardı. AI'dan asıl olarak bu kararları koda dökme, katmanlar
arasında tekrarlayan kod kalıplarını (CRUD, DTO, validasyon) hızlı yazma, EF Core migration/kurulum
adımlarını çalıştırma ve React component'lerinin implementasyonunda destek almak için yararlandım.
Geliştirme sırasında ortaya çıkan hataların (örn. barkod üretiminde Türkçe karakter sorunu, soft-delete
ile `.Include()` birlikte kullanıldığında oluşan veri kaybı) kök nedenini bulmakta da yardım aldım, ancak
her düzeltmeyi canlı ortamda kendim test edip doğruladım.
