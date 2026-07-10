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
cp .env.example .env   # gerekirse VITE_API_URL'i düzenleyin
npm run dev
```

Varsayılan giriş bilgileri (ilk migration ile seed edilir): `CompanyId=demo-sirket`, `Kullanıcı Adı=admin`,
`Şifre=123` — **production'a almadan önce mutlaka değiştirin.**

## Deploy

Bu bir tam-yığın (full-stack) uygulama olduğu için **frontend ve backend ayrı yerlerde barınır**:

### Frontend → Netlify
Repo kökünde `netlify.toml` hazır (base: `frontend`, build: `npm run build`, publish: `dist`, SPA
redirect kuralı dahil). Netlify'de yapmanız gereken tek şey: proje ayarlarından
**Environment variables** kısmına `VITE_API_URL` değişkenini, aşağıdaki backend'in gerçek HTTPS
adresiyle eklemek (örn. `https://sizin-backend-adresiniz.com/api`).

### Backend → Netlify'de ÇALIŞMAZ, ayrı bir sunucu gerekir
Netlify yalnızca statik dosya barındırır; .NET API'yi ve MS SQL Server'ı çalıştıramaz. Backend için
alternatifler: Azure App Service + Azure SQL, Railway, Render, Fly.io. Nereye alırsanız alın:
1. `ConnectionStrings__DefaultConnection` ve `Jwt__Key` ortam değişkeni olarak tanımlanmalı.
2. `Cors:AllowedOrigins` ortam değişkeni/appsettings'e Netlify domaininiz eklenmeli
   (örn. `Cors__AllowedOrigins__0=https://sizin-site.netlify.app`) — aksi halde tarayıcı CORS
   hatası verir.
3. Backend HTTPS üzerinden yayınlanmalı (Netlify sayfası HTTPS olduğundan, HTTP bir API'ye istek
   tarayıcı tarafından engellenir — "mixed content").

## Klasör Yapısı

```
AkilliDepo.Api/     .NET backend (Controllers, Managers, Repositories, Entities, DTOs, Migrations)
frontend/           React + TypeScript + MUI frontend
CALISMA_RAPORU.md   Geliştirme günlüğü — kararlar, mimari notlar, karşılaşılan hatalar ve çözümleri
```
