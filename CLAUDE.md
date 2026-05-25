# PYS — Proje Yönetim Sistemi (Session Handoff)

Bu dosya, projenin bu noktaya kadarki durumunu **bir sonraki Claude oturumu** veya
geliştirici için yazılmıştır. Her şeyden önce repo kökündeki **`SKILL.md`** kabul
kriterlerinin (20 madde) hepsi yüklenmelidir — bu dosya onların yerine geçmez.

> **Notu okuyan AI'a:** Daha önceki oturumlardaki kararlar `~/.claude/projects/.../memory/`
> içinde de var; ama bu dosya proje-içi gerçek durumdur. **Tutarsızlık varsa bu dosya
> doğrudur.** Mevcut bilinen sorunlar bölümünden başla.

---

## 🎯 Proje Hedefi

Trello/Jira benzeri, gerçek bir takım proje yönetim aracı:

- Kullanıcı kayıt olur, kendi rengini seçer.
- Proje oluşturan **otomatik owner** olur (Owner picker YOK).
- Takım arkadaşları **e-posta ile davet edilir**:
  - Email kayıtlıysa anında üye.
  - Değilse `Pending` davet — kişi kayıt olunca otomatik kabul.
- Görevler **Kanban tahtasında** 5 kolonda (Todo / InProgress / InReview / Done / Blocked).
- Görev kartı **atanan kişinin rengiyle** boyanır (sol şerit + avatar).
- Görev **karta tıklayarak düzenlenir**, kart **kolonlar arasında sürüklenebilir**.
- Görev assignee Picker'ı **yalnız o projenin üyelerini** gösterir.

---

## 🏗️ Mimari Özeti

```
PYS.slnx
└── src/
    ├── PYS.Core/        Entities, Enums, Abstractions (ICurrentUserService, IRepository)
    │                    ColorPalette (12 Material rengi + PickFor(email))
    ├── PYS.Data/        AppDbContext (audit override), Repositories, Configurations, Migrations
    ├── PYS.Service/     DTOs, IServices, AuthService/ProjectService/TaskService
    │                    JwtOptions, ServiceResult pattern, BCrypt password hashing
    └── PYS.API/         Minimal API endpoints, HttpCurrentUserService, JwtTokenService
                         JsonStringEnumConverter (kritik — enum'lar string olarak gidip-gelir)
```

Bağımlılık yönü: **API → Service → Data → Core** (tek yönlü, asla ihlal etme).

```
MAUI Client (PYS.Client)
└── net10.0-windows10.0.19041.0 (Windows-only)
    ├── App.xaml.cs                 Global UnhandledException → crash.log
    ├── AppShell.xaml               Login + Projects ShellContent + alt sayfa route'ları
    ├── MauiProgram.cs              DI (singleton HttpClient + AuthState + PysApi)
    ├── ViewModels/                 CommunityToolkit.Mvvm field-based ObservableProperty
    │                               (MVVMTK0045 NoWarn'lu — partial property desteği eksik 8.4)
    ├── Views/                      Login/Register/ChangePassword/Profile +
    │                               Projects/ProjectEdit + Members + Tasks/TaskEdit
    ├── Models/                     Client-side DTO record'lar (Color alanı + Role)
    ├── Services/                   AuthState (singleton), ApiClient, PysApi
    │                               ApiClient.EnsureSuccessAsync 3 hata formatı parse eder
    └── Converters/                 InvertedBool, StringNotEmpty, NullableHasValue,
                                    StringToColor, Initials
```

---

## 📦 Veritabanı

- **MS SQL LocalDB** instance: `(localdb)\MSSQLLocalDB`
- DB: `PYS_Db`
- Migrations (sırayla):
  1. `Initial` — Users, Projects, Tasks
  2. `TeamModel` — ProjectMembers + ProjectInvitations + backfill SQL (mevcut owner'ları member yapar)
  3. `UserColor` — User.ColorHex + backfill SQL (deterministic palette)
- Soft delete: tüm entity'ler `BaseEntity.IsDeleted` + `HasQueryFilter`
- Audit: `AppDbContext.SaveChangesAsync` override + `ICurrentUserService` claims'ten doldurur

---

## 🔐 Auth

- **JWT Bearer** (HmacSha256). Claims: NameIdentifier, Name, Email, Role.
- `JwtOptions.SecretKey` **user-secrets**'ta (kod tabanında değil):
  ```powershell
  dotnet user-secrets set "Jwt:SecretKey" "<48 byte base64>" --project src/PYS.API
  ```
  Eğer secret yoksa `Program.cs` start'ta açıklayıcı hata fırlatır.
- BCrypt.Net-Next ile şifre hash.
- `RegisterAsync`/`LoginAsync` her ikisi de `AcceptPendingInvitationsAsync` çağırır →
  email bekleyen tüm davetler otomatik ProjectMember'a dönüşür.

---

## 🎨 Renk Sistemi

- `User.ColorHex` nvarchar(9). Default kayıt sırasında `ColorPalette.PickFor(email)`.
- 12 Material rengi: Blue, Red, Green, Orange, Purple, Pink, Cyan, Amber, Indigo, Teal, DeepOrange, Brown.
- `AuthResponseDto`, `ProjectMemberDto`, `TaskDto.AssigneeColor` hepsinde renk taşınır.
- MAUI client `StringToColorConverter` ile `#RRGGBB` → `Microsoft.Maui.Graphics.Color`.
- Self-update: `PUT /api/auth/me/color` body `{color}`. `ProfilePage` palette grid'i.

---

## ✅ Çalıştığı Doğrulanan Davranışlar

Bu senaryoları **PowerShell + Invoke-RestMethod** ile **canlı doğruladım**:

1. `register` (anonim) → `auth-response` döner, varsayılan renk atanmış.
2. `login` → JWT döner (1 saat ömür), `Authorization: Bearer <token>` header'ı kalan tüm istekleri açar.
3. `change-password` (yanlış mevcut / aynı / doğru senaryoları) — 400/204 doğru.
4. `POST /api/projects` (auto-owner) — current user owner + ProjectMember(Owner) satırı oluşur.
5. `GET /api/projects` — sadece owner OR member olduğum projeler döner.
6. `GET /api/projects/{id}/members` — Color alanı dolu, herkes listelenir.
7. `POST /api/projects/{id}/invitations` email body — kayıtlıysa anında member, değilse Pending.
8. Yeni kullanıcı register olunca pending davetler accepted'a döner + ProjectMember satırı.
9. `POST /api/tasks` `assigneeId=<member>` → 201. Non-member assignee → 400 "must be a member".
10. `PUT /api/tasks/{id}` status değişimi (drag-drop simülasyonu) → 200, completedAt set/clear.
11. `JsonStringEnumConverter` backend'de aktif — payload hem `"status":"Todo"` hem `"status":0` kabul edilir.
12. MAUI build temiz: 0 uyarı, 0 hata. `MVVMTK0045` NoWarn'lu (AOT advisory).

---

## 🐛 BİLİNEN SORUNLAR (Sonraki Session Buradan Başlasın)

### 🔴 P1 — Kanban kartına tıklayınca uygulama crash oluyor

**Belirti:** `TasksPage`'de bir kart tıklayınca uygulama kapanıyor.
**Stack trace (crash.log'tan):**
```
COMException 0x8001010E — RPC_E_WRONG_THREAD
"The application called an interface that was marshalled for a different thread."
   at Microsoft.UI.Xaml.Controls.ItemsControl.set_ItemsSource
   at Microsoft.Maui.Handlers.PickerHandler.Reload
   at ObservableCollection.OnCollectionChanged
   at PYS.Client.ViewModels.TaskEditViewModel.LoadAsync()  ← line 97
```

**Şüphem:** `TaskEditViewModel.LoadAsync` içinde `ProjectMembers.Clear()` veya `Add()`
çağrısı **UI thread DIŞINDA** çalışıyor → Picker handler thread-marshalling hatası.

**Denenenler:**
1. `FireAndForget`'tan `Task.Run` kaldırıldı, `_ = SafeAsync(work)` yapıldı —
   teoride UI thread context'i korumalı. **Yine de crash devam ediyor.**
2. `ProjectMembers` `List<>` → `ObservableCollection<>` yapıldı. Yetmedi.

**Sonraki adım için stratejiler:**
- a) `ProjectMembers.Clear/Add` çağrılarını **explicit `MainThread.BeginInvokeOnMainThread`** ile sar.
- b) Veya: `ProjectMembers`'i `ObservableCollection` yerine **`List` + explicit OnPropertyChanged**'e geri al, ama listeyi yeni instance ile değiştir (Picker.ItemsSource yeniden bağlanır).
- c) Veya: TaskEditViewModel'i `SemaphoreSlim` ile koru, **paralel iki LoadAsync** çağrısının race yaratmadığından emin ol. Şu anda `OnTaskIdChanged` + `OnProjectIdChanged` her ikisi de `FireAndForget(LoadAsync)` tetikliyor (Shell QueryProperty atamaları ardışık).
- d) **DEBUG ipucu:** `LoadAsync`'in başına `if (!MainThread.IsMainThread) System.Diagnostics.Debugger.Break();` koy → gerçekten UI thread'de olup olmadığını doğrula.

### 🔴 P2 — Drag-drop visual delay (görev başka pencereye geçilince yeni yerinde görünüyor)

**Belirti:** Görev kartı sürüklenip başka kolona bırakılınca **anında refresh olmuyor**.
Başka uygulamaya alt-tab yapıp dönülünce yeni durumu görüyor. Backend tarafı **doğru
güncelleniyor** (smoke test 200 OK döner) — UI yenilenmiyor.

**Şüphem:** P1 ile aynı kök neden. `DropAsync` → `await LoadAsync` → ObservableCollection
mutate UI thread dışında → CollectionView redraw etmiyor, WinUI invalidate olunca anca
yeni state'i render ediyor.

**Sonraki adım:** P1 düzeltilince muhtemelen otomatik çözülür. Eğer çözülmezse:
- `DropAsync`'in sonunda `await Task.Yield()` veya explicit `MainThread.InvokeAsync(LoadAsync)`.
- Veya in-place mutation: `LoadAsync` çağırmadan **kartı eski koleksiyondan kaldır + yeni koleksiyona ekle** (network güncellemesi ayrı).

### 🟡 P3 — `crash.log` çift konumda

`App.xaml.cs` log dosyasını şuraya yazıyor:
- `%LOCALAPPDATA%\PYS.Client\crash.log` (asıl)

Ama proje kökünde de eski bir `crash.log` var (kullanıcı manuel kopyaladı). Karışıklığa
yol açabilir; sonraki session başında **iki konumu da temizle**:
```powershell
Remove-Item "$env:LOCALAPPDATA\PYS.Client\crash.log","C:\Users\tunah\source\repos\PYS\crash.log" -ErrorAction SilentlyContinue
```

### 🟢 P4 — `MVVMTK0045` uyarıları NoWarn'lu

CommunityToolkit.Mvvm 8.4.0 partial-property source-generator desteği eksik. Şu an
field-based pattern + `<NoWarn>$(NoWarn);MVVMTK0045</NoWarn>` ile geçici çözüm.
8.5+ stable çıkınca partial property'ye geçilebilir, NoWarn kaldırılır.

---

## 🧰 Geliştirme Komutları

### Klonlama sonrası tek seferlik

```powershell
cd c:\Users\tunah\source\repos\PYS
sqllocaldb start MSSQLLocalDB
dotnet ef database update --project src/PYS.Data/PYS.Data.csproj --startup-project src/PYS.API/PYS.API.csproj

# JWT secret kur
$bytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
dotnet user-secrets set "Jwt:SecretKey" ([Convert]::ToBase64String($bytes)) --project src/PYS.API/PYS.API.csproj

dotnet build PYS.slnx
```

### Her oturum (2 terminal)

```powershell
# Terminal 1 — Backend
dotnet run --project src/PYS.API/PYS.API.csproj --launch-profile http
# http://localhost:5014

# Terminal 2 — MAUI
dotnet run --project src/PYS.Client/PYS.Client.csproj --launch-profile "Windows Machine"
```

### Smoke test (backend doğrudan)

`src/PYS.API` boyunca PowerShell `Invoke-RestMethod` ile end-to-end test yapılabiliyor.
Önceki oturumda string-enum payload + drag-drop + invite + register-accept akışı bu
şekilde tamamen doğrulandı.

### Migration ekleme

```powershell
dotnet ef migrations add <AdiBuyukHarfBaslar> --project src/PYS.Data/PYS.Data.csproj --startup-project src/PYS.API/PYS.API.csproj --output-dir Migrations
dotnet ef database update --project src/PYS.Data/PYS.Data.csproj --startup-project src/PYS.API/PYS.API.csproj
```

---

## 🚫 Asla Yapma

- **`CreateProjectDto`'ya `OwnerId` ekleme.** Owner her zaman current user'dır (claims).
- **Global "Yeni Kullanıcı" UI ekranı koyma.** Kullanıcı yönetimi yalnız:
  - `auth/register` (kişi kendi kaydolur), veya
  - `projects/{id}/invitations` (proje sahibi email ile davet eder)
- **Görevde assignee'yi proje üyesi olmayan bir kullanıcıdan seçilebilir hale getirme.**
  `GET /api/projects/{id}/members` her zaman tek geçerli kaynak.
- **`Task.Run` ile async başlatma**, eğer içinde ObservableCollection mutate ediyorsa.
  WinUI'da `RPC_E_WRONG_THREAD` (0x8001010E) garantilidir.
- **`MauiXamlInflator=SourceGen` ile compiled binding'lerde** olmayan property'lere bind etme.
  Build başarılı olsa bile runtime'da `NotFound` crash atar.
- **Yeni `User.cs` alanı eklediğinde** AuthService.RegisterAsync ve mapping'leri unutma.

---

## 🗂️ Dosya Haritası (en kritikler)

```
src/PYS.API/Program.cs                              Composition root, JsonStringEnumConverter
src/PYS.API/Endpoints/AuthEndpoints.cs              register/login/logout/change-pwd/me/me/color
src/PYS.API/Endpoints/ProjectEndpoints.cs           CRUD + members + invitations
src/PYS.API/Endpoints/TaskEndpoints.cs              CRUD (filtered by project membership)

src/PYS.Service/Services/AuthService.cs             register + login + change-pwd + update-color
src/PYS.Service/Services/ProjectService.cs          owner=current, member-scoped, invite
src/PYS.Service/Services/TaskService.cs             access scoped, assignee=member validated

src/PYS.Client/MauiProgram.cs                       DI registrations
src/PYS.Client/AppShell.xaml(.cs)                   routes
src/PYS.Client/Services/ApiClient.cs                HttpClient wrapper, parses 3 error formats
src/PYS.Client/Services/PysApi.cs                   Typed endpoint methods
src/PYS.Client/ViewModels/BaseViewModel.cs          ⚠️ FireAndForget burada (P1 ile ilgili)
src/PYS.Client/ViewModels/TaskEditViewModel.cs      ⚠️ LoadAsync — P1 crash'ın gerçekleştiği yer
src/PYS.Client/ViewModels/TasksViewModel.cs        ⚠️ DragStarted/DropAsync — P2 lag ile ilgili
src/PYS.Client/Views/TasksPage.xaml                 Kanban 5 kolon, drag/drop gesture'ları
src/PYS.Client/Views/TaskEditPage.xaml              Picker(ProjectMembers) — crash burada tetikleniyor
```

---

## 🧪 Sonraki Session İçin İlk Aksiyon Listesi

1. **`crash.log`'u oku** — şu anki bilinen hatadan farklı bir stack varsa rapor et.
2. **P1'i çöz** — `TaskEditViewModel.LoadAsync`'te `ProjectMembers.Clear/Add`'i
   `MainThread.BeginInvokeOnMainThread` ile sar. Test: kart tıkla, crash yok mu?
3. **P2'yi doğrula** — drag-drop sonrası anında refresh oluyor mu? Olmuyorsa
   `DropAsync` sonunda `await Task.Yield()` + `MainThread.InvokeAsync(LoadAsync)`.
4. **SemaphoreSlim guard** — `LoadAsync` paralel çağrıldığında ikinciyi skip et:
   ```csharp
   private readonly SemaphoreSlim _gate = new(1, 1);
   if (!await _gate.WaitAsync(0)) return;
   try { ... } finally { _gate.Release(); }
   ```
5. **Çalıştığını kanıtla** — uygulamayı aç, alice/newSecret456 ile login, **alice'in
   şifresi sıfırlanmış olabilir** (5+ sefer testten geçti). Eğer login olmazsa
   `dotnet ef database drop --force` + `update` ile sıfırla; sonra register'dan kendi
   hesabınla başla.

---

## 👤 Test Kullanıcıları (eğer DB sıfırlanmadıysa)

| Username | Email              | Şifre (en son set) | Renk    |
|----------|--------------------|--------------------|---------|
| alice    | alice@pys.dev      | `newSecret456`     | Pink    |
| bob      | bob@pys.dev        | `secret123`        | Green   |
| charlie  | charlie@pys.dev    | `charlie123`       | (palette)|
| newcomer | new@team.dev       | `comer123`         | Brown   |
| Tunahan  | tunahan@gmail.com  | (sen biliyorsun)   | Amber   |

---

## 📝 Commit Notu

Bu noktaya kadar commit atılmamış. Sonraki session ilk yaptığı şeylerden biri olarak
P1 + P2 çözüldüğünde commit atılması önerilir — şu ana kadar ki tüm refactor tek
büyük commit olarak gider, sonraki çalışmalar daha küçük commit'lere bölünür.

---

**Son güncelleme:** 2026-05-24 oturumu sonu. Kullanıcı mola istedi; backend kusursuz
çalışıyor, MAUI frontend Kanban'da P1+P2 sorunları açık.
