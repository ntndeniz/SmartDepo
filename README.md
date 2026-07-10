# Akıllı Depo Yönetimi

Çok kiracılı (multi-tenant) bir depo yönetim sistemi: ürün/marka tanımlama, mal kabul, raf/konum takibi,
mağaza siparişi toplama (picking), paletleme ve sevkiyat, haftalık otomatik raporlama.

## Teknoloji Yığını

| Katman | Teknoloji |
|---|---|
| Backend | .NET 9.0 (ASP.NET Core Web API) |
| Veritabanı | MS SQL Server |
| ORM | Entity Framework Core |
| Frontend | React 18 + TypeScript + Material-UI (MUI) |
| Kimlik doğrulama | JWT |

Mimari: **Controller → Manager → Repository → Entity (DbContext)**. Detaylı mimari anlatımı için proje
kökündeki `CALISMA_RAPORU.md` dosyasına bakın.

## Yerel Kurulum

### Gereksinimler
- .NET 9 SDK
- Node.js 20+
- MS SQL Server (yerelde Docker ile: `docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<güçlü-bir-şifre>" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`)

### Backend

```bash
cd AkilliDepo.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1,1433;Database=AkilliDepoDb;User Id=sa;Password=<sizin-sifreniz>;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "<en az 32 karakterlik rastgele bir anahtar>"
dotnet ef database update
dotnet run --urls http://localhost:5200
```

> **Önemli:** Gerçek bağlantı dizesi ve JWT anahtarı `appsettings.json`'a **asla** yazılmaz —
> `dotnet user-secrets` kullanıcının kendi makinesinde, repo dışında tutulur. Production'da bu
> değerler ortam değişkeni (`ConnectionStrings__DefaultConnection`, `Jwt__Key`) olarak verilir.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

Varsayılan giriş bilgileri (ilk migration ile seed edilir): `CompanyId=demo-sirket`, `Kullanıcı Adı=admin`,
`Şifre=123` — **production'a almadan önce mutlaka değiştirin.**

## Ekran Görüntüleri

### Giriş
Çok kiracılı (multi-tenant) sisteme `CompanyId`, kullanıcı adı ve şifre ile giriş. Kimlik JWT ile doğrulanır,
şirket kimliği token'dan çıkarılır — client'tan gelen değere hiçbir zaman güvenilmez.

![Giriş ekranı](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.29.16.png)

### Mal Kabul
Depoya gelen ürünlerin barkod okutularak kayıt altına alındığı ekran. Liste varsayılan olarak **sadece
bugünün** kayıtlarını gösterir, tarih aralığı genişletilebilir.

![Mal Kabul listesi](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.29.51.png)

Barkod okutuldukça (kamera veya harici okuyucu ile) aynı ürünün kalem sayısı otomatik artan sihirbaz akışı:

![Mal Kabul Sihirbazı](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.30.09.png)

### Konum ve Stok
Depo, koridor → bölge → raf hiyerarşisiyle görsel olarak haritalanır; dolu/boş raf durumu renkle ayırt edilir.
Aynı ekranda stoktaki tüm koliler (rafa yerleştirilmiş veya henüz stokta bekleyen) listelenir.

![Görsel depo haritası](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.30.21.png)

![Stoktaki koliler tablosu](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.30.35.png)

### Mağaza Siparişleri
Mağazalardan gelen siparişler; elle girilebilir ya da mağazanın gönderdiği PDF sipariş formu otomatik
ayrıştırılarak (barkod/miktar satırları okunarak) oluşturulabilir.

![Siparişler listesi](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.30.46.png)

![Yeni mağaza siparişi formu](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.30.57.png)

### Sevkiyat ve Paletleme
Aktif toplama (picking) işleri burada yönetilir — FIFO mantığıyla önce raftaki, sonra stoktaki kolilerden
toplanır. Kapatılan koliler "Paletlenmemiş Koliler" listesine düşer; aynı mağazaya ait koliler seçilip tek
palette birleştirilir ve palet durumu (Hazırlanıyor → Sevke Hazır → Sevk Edildi) takip edilir.

![Sevkiyat ve paletleme ekranı](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.31.06.png)

![Oluşturulan paletler listesi](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.31.15.png)

### Raporlama
Mal kabul ve sevkiyat hareketleri için haftalık CSV raporları arka planda otomatik üretilir, buradan
indirilebilir.

![Raporlar listesi](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.31.23.png)

### Firma Ayarları
Depo boyutu (koridor/bölge/raf sayısı), markalar ve mağazalar tek bir "Ayarlar" sekmesi altında yönetilir.

![Depo boyutu ayarı](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.31.32.png)

![Markalar yönetimi](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.31.42.png)

![Mağazalar yönetimi](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.31.50.png)

### Kullanıcı Yönetimi (RBAC)
Admin, Personel (Staff) rolündeki kullanıcıları ekleyip yönetebilir. Personel rolü Markalar/Mağazalar gibi
yönetimsel ekranları sadece görüntüleyebilir, değişiklik yapamaz — bu kısıtlama hem frontend'de (butonlar
gizlenir) hem backend'de (`[Authorize(Roles = "Admin")]`) uygulanır.

![Kullanıcı yönetimi](SmartDepoImage/Ekran%20Resmi%202026-07-10%2017.32.13.png)

## Klasör Yapısı

```
AkilliDepo.Api/     .NET backend (Controllers, Managers, Repositories, Entities, DTOs, Migrations)
frontend/           React + TypeScript + MUI frontend
CALISMA_RAPORU.md   Geliştirme günlüğü — kararlar, mimari notlar, karşılaşılan hatalar ve çözümleri
```
