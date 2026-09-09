# Tezgah

Üretim yapan bir işletmenin sipariş–reçete–stok–üretim–fatura akışını tek yerden
yürüten bir ERP uygulaması. Bir siparişin neye ihtiyaç duyduğunu sistem kendisi
çıkarır, üretim kaydı reçetedeki bileşenleri stoktan düşer ve mamulü depoya alır.

Uygulama iki türlü kullanılabilir: **normal kullanıcı girişi** ile ve **herkese açık
demo** olarak. Demoda her ziyaretçiye kendi izole veritabanı verilir, oturum boyunca
kayıt ekleyip silebilir, oturum bitince alan bir sonraki ziyaretçi için sıfırlanır.

## 🚀 Teknolojiler

**Sunucu** — .NET 8 Web API, Clean Architecture, CQRS (MediatR), Entity Framework Core,
SQL Server, ASP.NET Identity + JwtBearer, Mapster, FluentValidation, TS.Result,
GenericRepository, Swagger/OpenAPI, rate limiting.

**İstemci** — Angular 18 (standalone bileşenler + signals), tembel yüklenen rotalar,
kendi tasarım sistemi ve satır içi SVG ikon takımı. Üçüncü parti UI kitaplığı yok.

## 🏛️ Mimari

```
ERPServer
├── ERPServer.Domain          entity'ler, enum'lar, repository sözleşmeleri, demo seçenekleri
├── ERPServer.Application     CQRS handler'ları, doğrulama, servis arayüzleri, eşleme
├── ERPServer.Infrastructure  EF Core context, repository'ler, JWT, demo sandbox havuzu
└── ERPServer.WebAPI          controller'lar, demo modülü, ara katmanlar

ERPClient/src/app
├── core       servisler (http, auth, demo, tema, bildirim), biçimlendiriciler, menü
├── ui         tasarım sisteminin bileşenleri (ikon, kip, panel, tablo yüzeyleri)
├── layout     kabuk: sol ray, üst çubuk, demo şeridi
└── pages      ekranlar (panel, tanımlar, siparişler, üretim, faturalar)
```

Uygulamada **tek bir `ApplicationDbContext`** var. Çok kiracılı bir yapı kurmak yerine,
isteğin bağlanacağı veritabanı `ConnectionStringResolver` tarafından çalışma zamanında
çözülüyor: demo jetonu taşıyan istek kendi sandbox'ına, diğer her istek ana
veritabanına gidiyor.

## 🔄 İş akışı

| Adım | Ne olur |
| --- | --- |
| **Sipariş** | Müşteri siparişi kalemleriyle açılır; belge numarası yıl bazlı üretilir. |
| **İhtiyaç planı** | Stoğu yetmeyen kalemler üretilecek adet kadar reçeteleriyle patlatılır, bileşenlerin brüt ihtiyacı toplanır ve stok bir kez düşülerek eksik liste çıkarılır. Sipariş "planlandı"ya geçer. |
| **Üretim** | Reçetedeki bileşenler üretim adediyle çarpılıp depolardan düşülür, mamul hedef depoya girer. Maliyeti tüketilen bileşenlerin ağırlıklı ortalamasıdır. |
| **Fatura** | Alış faturası seçilen depoya stok girer, satış faturası çıkarır. Siparişe bağlı kesilirse sipariş "tamamlandı"ya döner. |

Stok hiçbir yerde alan olarak tutulmuyor; her zaman `StockMovement` kayıtlarının
giriş–çıkış farkından hesaplanıyor.

## 👤 Kullanıcı yönetimi

`/kullanicilar` ekranından hesap açılır, düzenlenir, silinir ve parola sıfırlanır
(`Users/GetAll`, `Create`, `Update`, `DeleteById`, `ChangePassword`). Rol kavramı yok;
giriş yapabilen herkes aynı yetkide.

İki koruma var: kişi **kendi hesabını silemez** (oturumu anında geçersizleşirdi) ve
**sistemdeki son kullanıcı silinemez** (kimse giremez hâle gelirdi). Yeni hesaplar
`EmailConfirmed = true` ile açılır; kurulumda e-posta onayı zorunlu olduğu için aksi
hâlde hesap oluşur ama giriş yapılamazdı.

Demo oturumunda kullanıcı yönetimi **salt okunurdur** — sandbox izole olsa da kullanıcı
kayıtları iş verisiyle birlikte temizlenmediği için bir ziyaretçinin girdiği ad ve
e-posta sonrakine görünür kalırdı. Menüde de gizlenir.

## 🧪 Demo modu

`Demo:Enabled` açıkken uygulama başlangıçta sabit sayıda **sandbox veritabanı** hazırlar
(migrate eder ve örnek veriyle doldurur). Ziyaretçi `POST /api/demo/start` çağırdığında
bunlardan biri o oturuma kiralanır ve oturuma özel, sandbox adını taşıyan bir JWT üretilir.

- Sandbox kiralanma anında sıfırlanır; her ziyaretçi aynı başlangıç verisiyle açar.
- Yazma işlemleri (`Create`, `Update`, `DeleteById`, `RequirementsPlanningByOrderId`)
  sayılır ve `Demo:WriteLimit` aşıldığında reddedilir.
- Boşta kalma, mutlak süre ve **çalışma kümesi eşiği** için bir arka plan servisi
  oturumları geri alır.
- Havuz dolduğunda en uzun süredir dokunulmayan oturum geri alınır, yani eşzamanlı
  ziyaretçi sayısı `Demo:SlotCount` ile sınırlıdır.

API, demoya özel retleri gövdede bir `demoCode` ile bildirir (`session_ended`,
`write_limit`). İstemci bunu yakalayıp oturum penceresini gösterir.

| Endpoint | Açıklama |
| --- | --- |
| `GET /api/demo/config` | Anonim. Demo açık mı; giriş ekranı düğmeyi buna göre gösterir. |
| `POST /api/demo/start` | Anonim. Sandbox kiralar, token döner. |
| `GET /api/demo/status` | Kalan işlem hakkı ve süre. |
| `POST /api/demo/reset` | Sandbox'ı sıfırlar, yeni oturum açar. |
| `POST /api/demo/end` | Oturumu kapatır, sandbox'ı iade eder. |

Başlangıç verisi bilinçli olarak eksik bir sipariş içerir: açık siparişin ihtiyaç
planlaması boş bir liste değil, tedarik edilecek üç bileşen döndürür.

## ⚙️ Yapılandırma

Gizli değerler depoda tutulmaz, ortam değişkeniyle geçilir (`__` iç içe anahtarları ayırır):

| Anahtar | Açıklama |
| --- | --- |
| `ConnectionStrings__SqlServer` | Ana veritabanı bağlantısı. Sandbox'lar da aynı sunucu ve kimlik bilgileriyle açılır. |
| `Jwt__SecretKey` | İmzalama anahtarı. **Production'da zorunlu**, yoksa uygulama açılmaz. |
| `Database__MigrateOnStartup` | Açılışta ana veritabanı migration'larını uygular. |
| `Cors__AllowedOrigins__0` | İzin verilen origin listesi. Boşsa kimlik bilgisi olmadan tüm origin'lere izin verilir. |
| `Seed__AdminPassword` | İlk `admin` kullanıcısının parolası. |
| `Demo__*` | `DemoOptions` alanları: `Enabled`, `SlotCount`, `WriteLimit`, `NudgeAfterWrites`, `IdleTimeoutMinutes`, `AbsoluteTimeoutMinutes`, `MemoryThresholdMegabytes`, `ContactUrl`, `WorkspaceName`. |

İstemcinin API adresi çalışma zamanında `assets/config.json` dosyasından okunur, yani
adresi değiştirmek için yeniden derleme gerekmez:

```json
{ "apiUrl": "https://api.ornek.com/api" }
```

Dosya boş bırakılırsa derleme sırasındaki adres (`/api`) kullanılır.

## 🐳 Docker ile çalıştırma

```bash
cp .env.example .env   # degerleri doldurun
docker compose up -d --build
```

Uygulama `http://localhost:8080` adresinde açılır. Caddy hem statik dosyaları sunar hem
de `/api` isteklerini API konteynerine yönlendirir, böylece tarayıcı tarafında
cross-origin çağrı olmaz. SQL Server yığının içinde gelir ve yalnızca sunucunun kendi
localhost'una bağlanır.

**Sunucuya kurarken** `.env` içinde şunları değiştirin:

```
SITE_ADDRESS=demo.ornek.com   # alan adi yazilirsa Caddy Let's Encrypt sertifikasi alir
HTTP_PORT=80
HTTPS_PORT=443
```

Alan adının A kaydı sunucunun IP'sine bakıyorsa Caddy sertifikayı ilk açılışta kendi
alır ve süresi dolmadan yeniler; 80 ve 443 portları doğrulama için dışarı açık olmalıdır.

## ☁️ Coolify ile dağıtım

`docker-compose.coolify.yml` Coolify için hazırlanmış varyanttır: hiçbir servis host
portu yayınlamaz, TLS'i Coolify'ın ters vekili sonlandırır.

**İmajlar sunucuda derlenmez.** `.github/workflows/images.yml` her push'ta Angular ve
.NET imajlarını GitHub'ın koşucusunda derleyip `ghcr.io/<kullanıcı>/tezgah-api` ve
`tezgah-client` olarak GHCR'ye iter; sunucu yalnızca hazır imajı çeker. Sebebi
somut: yayın sunucusu 2 çekirdek / 4 GB ve swap'sızdı, `npm install` ile
`dotnet publish` birlikte 2 GB'ın üzerinde bellek istiyor ve sunucuda
çalıştıklarında yanı başındaki canlı servisleri düşürüyorlardı. Çekilecek etiketi
`IMAGE_TAG` belirler.

> GHCR paketleri ilk oluşturulduğunda private gelir. Sunucunun kimlik doğrulamadan
> çekebilmesi için GitHub → Packages → ilgili paket → *Package settings* →
> *Change visibility* → **Public** yapılmalıdır (paket başına bir kez).

1. Coolify'da yeni bir **Docker Compose** kaynağı oluşturun, bu depoyu bağlayın.
2. Compose dosyası olarak `docker-compose.coolify.yml` seçin.
3. `.env.example` içindeki değişkenleri panelin ortam değişkenleri bölümüne girin
   (`MSSQL_SA_PASSWORD`, `JWT_SECRET`, `ADMIN_PASSWORD` zorunlu).
4. `client` servisine alan adını atayın; Coolify `SERVICE_FQDN_CLIENT_80` değişkenini
   kendisi doldurur.
5. Dağıtın. API'nin `/health` ucu hazır olduğunda Coolify yeni konteynere geçer.

Sunucudaki SQL Server'a kendi bilgisayarınızdan bağlanmak için SSH tüneli kurun:

```bash
ssh -N -L 1433:localhost:1433 root@<sunucu>
```

## 💻 Lokal geliştirme

Sunucu (`appsettings.Development.json` yerel SQL Server'ı gösterir, demo modu açıktır):

```bash
dotnet run --project ERPServer/ERPServer.WebAPI --launch-profile https
```

İstemci:

```bash
cd ERPClient && npm install && npm start
```

API `https://localhost:7054` (Swagger arayüzü `/swagger`), istemci
`http://localhost:4200` adresinde çalışır. İlk kullanıcı `admin` / `1` olarak oluşturulur.

Migration eklemek için:

```bash
dotnet ef migrations add <ad> --project ERPServer/ERPServer.Infrastructure --startup-project ERPServer/ERPServer.WebAPI
```

## 📚 Kaynak

Projenin çekirdek eğitimi:
📺 _[Taner Saydam'ın Udemy profili](https://www.udemy.com/user/taner-saydam/?kw=taner+saydam&src=sac)_ ⭐⭐⭐⭐⭐ <br>
🐙 _[ERP.Udemy](https://github.com/TanerSaydam/ERP.Udemy)_

## 📬 İletişim

[ataberkkaya.com](https://ataberkkaya.com)
