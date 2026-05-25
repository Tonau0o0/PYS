# PYS (Proje Yönetim Sistemi) - Geliştirici Yönergeleri ve Kabul Kriterleri

Sen kıdemli bir .NET Yazılım Mimarı ve AI Mühendisisin. Bu proje, "Minimalizm ve Maksimum Verim" ilkesiyle geliştirilen, .NET MAUI ve ASP.NET Core Minimal API kullanan katmanlı mimariye sahip bir proje yönetim sistemidir. Aşağıdaki kurallar KESİNDİR ve hiçbir koşulda ihlal edilemez (Aksi halde proje kabul edilmez).

## 🚨 KRİTİK KABUL KRİTERLERİ (FATAL RULES)
1. **Zorunlu Altyapı:** Frontend kesinlikle **.NET MAUI** ile, Backend kesinlikle **ASP.NET Core Minimal API** ile oluşturulacaktır.
2. **Çalışabilirlik:** Yazdığın kod daima derlenebilir ve çalışabilir olmalıdır. Bozuk veya test edilmemiş kod blokları bırakma.
3. **Veritabanı ve ORM:** Veritabanı olarak **MS SQL Server**, ORM olarak **Entity Framework Core (Code First)** yaklaşımı kullanılacaktır.
4. **Migration Yönetimi:** Veritabanı şemasındaki her değişiklik için EF Core Migration'ları eklenecek ve güncellenecektir.
5. **İsimlendirme:** Tüm proje genelinde C# isimlendirme standartlarına (PascalCase, camelCase vb.) sıkı sıkıya uyulacaktır.

## 🏗️ MİMARİ VE OOP PRENSİPLERİ
6. **Katmanlı Mimari:** Sunum, İş Mantığı (Service) ve Veri Erişim (Repository/EF) katmanları kesinlikle birbirinden ayrılacaktır.
7. **Arayüzler (Interfaces):** Tüm servis sınıfları (Service classes) bir arayüz (Interface) üzerinden tanımlanacak ve Dependency Injection ile projeye dahil edilecektir.
8. **OOP:** Kapsülleme, soyutlama ve kalıtım gibi Nesne Yönelimli Programlama prensipleri her katmanda uygulanacaktır.
9. **LINQ:** Veri erişim ve filtreleme işlemlerinin tamamında LINQ aktif ve optimize bir şekilde kullanılacaktır.

## 💾 VERİ YÖNETİMİ VE CRUD İŞLEMLERİ
10. **Temel İşlemler:** Veritabanından listeleme, ekleme, güncelleme ve silme (CRUD) işlemleri eksiksiz yapılacaktır.
11. **Gelişmiş Listeleme:** Listeleme işlemlerinde çeşitli kriterlere göre sorgulama ve filtreleme yapılabilecektir.
12. **Denetim İzi (Audit Logging):** Veri ekleme ve güncelleme işlemlerinde, işlemi hangi kullanıcının ve ne zaman yaptığını belirten veriler (Örn: `CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt`) kesinlikle tutulacaktır.

## 🔐 KİMLİK DOĞRULAMA VE GÜVENLİK
13. **Auth İşlemleri:** Kullanıcı girişi (Login), Kullanıcı çıkışı (Logout) ve Şifre Değiştirme sayfaları/endpoint'leri eksiksiz tasarlanacaktır.
14. **Veri Doğrulama (Validation):** Kaydetme, güncelleme ve giriş sayfalarındaki veriler hem frontend hem de backend tarafında doğrulanacaktır (Boş bırakılamaz kuralları, karakter sınırları vb.).

## 🎨 UI/UX VE MAUI KONTROLLERİ
Arayüz tasarımında aşağıdaki MAUI kontrolleri en az bir kez, amacına uygun olarak kullanılacaktır:
15. **CollectionView:** Proje veya görev listelemelerinde.
16. **Picker:** Durum veya öncelik seçimlerinde.
17. **DatePicker veya TimePicker:** Başlangıç/bitiş tarihi girişlerinde.
18. **CheckBox veya RadioButton:** Tekil/çoklu seçim gerektiren form alanlarında.
19. **Ekstra Kontrol:** Derste standart olarak anlatılmayan, MAUI kütüphanesine ait ek bir kontrol (Örn: `SwipeView`, `RefreshView`, `CarouselView` vb.) projeye yenilikçi bir şekilde entegre edilecektir.

## 🚀 İŞ MANTIĞI
20. **Minimum Gereksinimler:** Proje konusuna (PYS - Proje Yönetim Sistemi) uygun olarak; proje oluşturma, görev atama, durum takibi ve kullanıcı yönetimi işlemleri çalışır durumda olmalıdır.