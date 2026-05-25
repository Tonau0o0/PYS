# PYS — Proje Yönetim Sistemi (Session Handoff)

Bu dosya projenin güncel durumunu **bir sonraki Claude oturumu** veya geliştirici için
yazar. Repo kökündeki **`SKILL.md`** (20 kabul kriteri) ve **`developer.skill.md`**
(Clean Code/SOLID/threading kuralları) her şeyden önce geçerlidir; bu dosya onların
yerine geçmez. **Tutarsızlık varsa kod doğrudur.**

---

## 🎯 Proje Hedefi

Trello/Jira benzeri takım proje yönetim aracı:
- Kayıt olan kullanıcı kendi rengini alır (deterministic palette), avatar yükleyebilir.
- Proje oluşturan **otomatik owner** (Owner picker YOK).
- Takım **e-posta ile davet** edilir (kayıtlıysa anında üye, değilse Pending → kayıt olunca otomatik kabul).
- Görevler **Kanban tahtasında** 5 kolon (Todo/InProgress/InReview/Done/Blocked), sürükle-bırak.
- Görev **karta tıklayınca detay ekranı** açılır; oradan düzenle/sil, yorum yaz, bağlı dosyalar.
- Proje dosya sistemi: **iç içe klasörler**, dosya yükleme, YouTube linki + uygulama içi oynatma.

---

## 🏗️ Mimari

```
PYS.slnx
└── src/
    ├── PYS.Core/     Entities, Enums, Abstractions
    │                 (ICurrentUserService, IRepository, IFileStorage + FileContent),
    │                 ColorPalette
    ├── PYS.Data/     AppDbContext (audit override + soft-delete query filter),
    │                 Repository<T>, Configurations, Migrations
    ├── PYS.Service/  DTOs, IServices, Services, ServiceResult, BCrypt, JwtOptions
    └── PYS.API/      Minimal API endpoints, JwtTokenService, HttpCurrentUserService,
                      PhysicalFileStorage (wwwroot tabanlı), ClaimsPrincipalExtensions,
                      JsonStringEnumConverter, statik dosya servisi (UseStaticFiles)
```
Bağımlılık yönü: **API → Service → Data → Core** (tek yönlü, asla ihlal etme).

```
MAUI Client (PYS.Client) — net10.0-windows10.0.19041.0 (Windows-only)
    ├── Platforms/Windows/App.xaml.cs   ⚠️ WinUI UnhandledException → crash.log (XAML thread
    │                                      hataları AppDomain handler'a uğramaz; burada yakalanır)
    ├── Platforms/Windows/FolderPickerService.cs  Native klasör seçici (IFolderPicker impl)
    ├── MauiProgram.cs                  DI (singleton HttpClient/AuthState/PysApi/
    │                                   ResourceDownloadService; #if WINDOWS IFolderPicker)
    ├── ViewModels/                     CommunityToolkit.Mvvm (field-based, MVVMTK0045 NoWarn)
    ├── Views/                          Login/Register/ChangePassword/Profile/Settings/
    │                                   Projects/ProjectEdit/Members/Tasks/TaskEdit/
    │                                   TaskDetail/VideoPlayer
    ├── Services/                       AuthState, ApiClient (3 hata formatı + multipart +
    │                                   GetBytes), PysApi, IFolderPicker, ResourceDownloadService
    └── Converters/                     InvertedBool, StringNotEmpty, NullableHasValue,
                                        StringToColor, Initials, ApiUrl (göreli→mutlak URL)
```

---

## 📦 Veritabanı (MS SQL LocalDB `(localdb)\MSSQLLocalDB`, DB: `PYS_Db`)

Migration'lar (sırayla):
1. `Initial` — Users, Projects, Tasks
2. `TeamModel` — ProjectMembers + ProjectInvitations
3. `UserColor` — User.ColorHex
4. `UserAvatar` — User.AvatarUrl
5. `ProjectResources` — ProjectResource (File/YouTube)
6. `ResourceFoldersAndTaskLinks` — ProjectResource.ParentFolderId (nested folders) + TaskResource (çok-çok)
7. `TaskComments` — TaskComment

- Soft delete: tüm entity'ler `BaseEntity.IsDeleted` + `HasQueryFilter`.
- Audit: `AppDbContext.SaveChangesAsync` override + `ICurrentUserService` claims'ten doldurur.
- Self-ref (klasör) ve çok-çok FK'lerde SQL Server cascade kısıtı → `OnDelete(Restrict)`; klasör
  silme recursive olarak serviste yapılır.

---

## 🔐 Auth

- **JWT Bearer** (HmacSha256). Claims: NameIdentifier, Name(=UserName), Email, Role.
- `Jwt:SecretKey` **user-secrets**'ta. Yoksa `Program.cs` start'ta hata fırlatır.
- ⚠️ **Login kullanıcı ADI ile yapılır, e-posta ile DEĞİL** (`LoginDto.UserName`).
- `RegisterAsync`/`LoginAsync` → `AcceptPendingInvitationsAsync` (email bekleyen davetler otomatik üye).
- `AuthResponseDto`: token, userId, userName, **email**, fullName, role, **color**, **avatarUrl**.

---

## ✨ Özellikler (bu repo'da çalışır durumda)

**Profil:** isim düzenleme (`PUT /me/profile`), e-posta değiştirme (`PUT /me/email`, benzersizlik),
avatar yükleme (`POST /me/avatar`, multipart, 25MB, sadece resim → `wwwroot/avatars`, statik servis).
Avatar görev kartlarında ve ana menü başlığında initials üstüne biner.

**Ayarlar sekmesi:** Projeler toolbar'ında **⚙ Ayarlar** → e-posta/şifre değiştir + çıkış.

**Proje Kaynakları (dosya sistemi):** Görevler ekranı altındaki panel.
- İç içe klasörler (breadcrumb + ⬆), klasör oluşturma, dosya/YouTube ekleme.
- Sürükle-bırak: kaynağı **klasöre taşı** (move), **görev kartına bağla** (link, çok-çok).
- YouTube **uygulama içi oynatma** (`VideoPlayerPage` WebView — ⚠️ `watch?v=` URL kullanılır,
  `/embed/` "Hata 153" verir).
- İndirme: **`IFolderPicker` ile hedef klasör seçtirilir**, sonra `GET …/resources/{id}/download`
  (auth) ile iner. Dosya olduğu gibi; **klasör içeriğiyle ZIP** olarak (yapı korunur).

**Görev Detayı (`TaskDetailPage`):** Karta tıklayınca açılır (3-nokta menü KALDIRILDI).
- Sol: başlık/durum/öncelik/tarih/atanan(avatar)/açıklama. Sağ üst toolbar: **Düzenle + Sil**.
- Sağ: **bağlı dosyalar** — klasöre tıklayınca **girintili ağaç** açılır (`ResourceNode`),
  her satırda ⬇ indir / ✕ kaldır.
- Alt: **yorumlar** (`TaskComment`) — yazar adı + tarih/saat + avatar; kendi yorumunu silebilir.

---

## 🐛 Bilinen Sorunlar / Notlar

- **P1 & P2 ÇÖZÜLDÜ.** P1'in gerçek kök nedeni `[QueryProperty(nameof(TaskId),"id")]`
  hedefinin `int?` olmasıydı (string→Nullable<int> cast → `InvalidCastException`, navigasyonda).
  Çözüm: `id` string `…Query` property'si ile alınıp parse edilir. Threading (ObservableCollection
  mutasyonları `MainThread.InvokeOnMainThreadAsync` + `SemaphoreSlim`) de düzeltildi (SKILL #4).
- 🟡 **Sürükle-bırak (tap+drag+drop aynı kartta)** WinUI'de nazik olabilir; sorun çıkarsa
  TaskEdit/TaskDetail'deki buton fallback'leri zaten var.
- 🟢 `MVVMTK0045` NoWarn'lu (CommunityToolkit.Mvvm 8.4 partial-property eksik).
- `IFolderPicker` yalnız `#if WINDOWS` kayıtlı (uygulama Windows-only).

---

## 🧰 Geliştirme Komutları

```powershell
# Tek seferlik
sqllocaldb start MSSQLLocalDB
dotnet ef database update --project src/PYS.Data/PYS.Data.csproj --startup-project src/PYS.API/PYS.API.csproj
$b=New-Object byte[] 48; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
dotnet user-secrets set "Jwt:SecretKey" ([Convert]::ToBase64String($b)) --project src/PYS.API/PYS.API.csproj

# Her oturum (2 terminal)
dotnet run --project src/PYS.API/PYS.API.csproj --launch-profile http          # http://localhost:5014
dotnet run --project src/PYS.Client/PYS.Client.csproj --launch-profile "Windows Machine"

# Migration ekleme
dotnet ef migrations add <Ad> --project src/PYS.Data/PYS.Data.csproj --startup-project src/PYS.API/PYS.API.csproj --output-dir Migrations
```

Smoke test: `Invoke-RestMethod` ile uçtan uca yapılabiliyor. ⚠️ PowerShell 5.1 quirk'leri:
`$pid` salt-okunur (kullanma); `Invoke-RestMethod` Türkçe karakterli JSON'u UTF-8 göndermez
(testte ASCII kullan); multipart için `curl.exe -F` kullan.

Build temizliği: derlemeden önce çalışan process'leri kapat —
`Get-Process PYS.API,PYS.Client | Stop-Process -Force` (exe kilidi build'i bozar).

---

## 🚫 Asla Yapma

- `CreateProjectDto`'ya `OwnerId` ekleme (owner her zaman current user).
- Global "Yeni Kullanıcı" UI'ı koyma (sadece register + project invitation).
- Görev assignee'sini proje üyesi olmayandan seçtirme (`GET /projects/{id}/members` tek kaynak).
- `[QueryProperty]` hedefini `int?` yapma → string al, parse et (P1 crash'ın kök nedeni).
- ObservableCollection mutasyonunu UI thread dışında yapma → `MainThread.InvokeOnMainThreadAsync`.
- `MauiXamlInflator=SourceGen` ile farklı `BindingContext`'li panelde `x:DataType` koymayı unutma
  (compiled binding yanlış tipe bağlanır).

---

## 🗂️ Kritik Dosya Haritası

```
API
  Program.cs                         composition root, UseStaticFiles, IFileStorage kaydı
  Endpoints/AuthEndpoints.cs         register/login/change-pwd/me/color/profile/email/avatar
  Endpoints/ProjectEndpoints.cs      proje CRUD + members + invitations
  Endpoints/TaskEndpoints.cs         görev CRUD + comments (GET/POST/DELETE)
  Endpoints/ResourceEndpoints.cs     resources (folder/file/link/move/download/delete) + task-link
  Common/PhysicalFileStorage.cs      IFileStorage impl (wwwroot/{category}/{guid})
Service
  Services/AuthService.cs            register/login/pwd/color/profile/email/avatar
  Services/TaskService.cs            erişim scope'lu; ToDto AssigneeAvatarUrl içerir
  Services/ResourceService.cs        klasör CRUD, move (döngü koruması), recursive delete,
                                     task-link, download (dosya + klasör→zip)
  Services/CommentService.cs         yorum CRUD (yazar join)
Client
  Services/ApiClient.cs              HttpClient wrapper (Get/Post/Put/Delete/PostFile/GetBytes)
  Services/PysApi.cs                 tipli endpoint metotları
  Services/ResourceDownloadService.cs  klasör seç + indir + yaz (paylaşılan)
  ViewModels/TasksViewModel.cs       Kanban + drag-drop; Resources alt-VM'i barındırır
  ViewModels/ResourcesViewModel.cs   dosya sistemi paneli (klasör nav + drag move/link)
  ViewModels/TaskDetailViewModel.cs  detay + yorum + bağlı kaynak ağacı (ResourceNode)
  Views/TasksPage.xaml               Kanban 5 kolon + alt kaynak paneli
  Views/TaskDetailPage.xaml          bilgi + bağlı dosyalar (ağaç) + yorumlar
```

---

## 👤 Test Kullanıcısı

| Kullanıcı adı | Şifre      | Not                          |
|---------------|------------|------------------------------|
| `demo`        | `demo1234` | login **kullanıcı adı** ile  |

(DB sıfırlanırsa register'dan yeni hesap aç. Demo projesi: "DEMO Kanban".)

---

## 📝 Commit Durumu

P1/P2 fix → Profil → Ayarlar → Kaynaklar → klasörler+task-link → görev detayı+yorumlar +
indirme/klasör-seçici sırasıyla commit'lendi. `.gitignore` eklendi; `bin/obj/.vs` ve
`wwwroot/avatars|uploads` takip dışı.

**Son güncelleme:** 2026-05-25. Tüm epikler çalışır + build 0 hata/0 uyarı.
