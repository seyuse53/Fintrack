# FinTrack

Bu dosya, FinTrack projesinde geliştirme yaparken uyulması gereken kuralları ve standartları tanımlar.

## AI Çalışma Prensipleri

Bu kurallar, aşağıdaki proje kurallarını tamamlar ve tüm geliştirmelerde uygulanmalıdır.

### Çalışma Yaklaşımı

- Sadece istenen görevi gerçekleştir.
- Görev kapsamı dışına çıkma.
- Gereksiz refactor yapma.
- Çalışan kodu yalnızca daha modern veya daha güzel göründüğü için değiştirme.
- Mevcut mimariye, kod stiline ve proje yapısına uy.
- "Bozuk değilse düzeltme." prensibini uygula.

### Token Verimliliği

- Sadece gerekli dosyaları oku.
- Tüm projeyi analiz etme.
- Aynı dosyayı gereksiz yere tekrar okuma.
- Küçük değişiklikler için tüm dosyayı yeniden yazma.
- Gereksiz açıklama ve rapor üretme.
- Aynı analizi tekrar etme.

### Kod Kalitesi

- KISS prensibini uygula.
- YAGNI prensibini uygula.
- Gereksiz helper, sınıf veya dosya oluşturma.
- Yeni bağımlılık eklemeden önce mevcut çözümleri değerlendir.

### Varsayım Yapma

- Emin olmadığın konularda varsayım yapma.
- Eksik bilgi varsa kullanıcıya sor.
- Kullanıcının istemediği davranış değişiklikleri yapma.

### UI Kuralları

Bu proje bir Windows masaüstü uygulamasıdır.

UI tasarım hedefleri:

- Kompakt ve profesyonel görünüm.
- İşlevsellik önceliklidir.
- Görsel sadelik yerine verimli alan kullanımı tercih edilir.
- Visual Studio, JetBrains IDE'leri ve VS Code yoğunluğu referans alınmalıdır.
- Web uygulaması tarzı geniş boşluklardan kaçınılmalıdır.

Yerleşim Kuralları

- Gereksiz padding kullanma.
- Gereksiz margin kullanma.
- Gereksiz spacing kullanma.
- Gereksiz beyaz boşluk bırakma.
- Kontroller hizalı olmalıdır.
- İlgili kontroller birlikte gruplanmalıdır.
- İçeriğe göre boyutlanan yerleşimler tercih edilmelidir.
- Gereksiz sabit Width ve Height değerlerinden kaçınılmalıdır.

Kontroller

- Butonlar standart masaüstü boyutunda olmalıdır.
- Input alanları yalnızca gerektiği kadar geniş olmalıdır.
- Gereksiz büyük ikon veya başlık kullanma.
- Büyük kartlar ve geniş boş alanlar oluşturma.

Pencereler

- Pencereler içerik kadar büyümelidir.
- Gereksiz büyük pencere oluşturma.
- Diyaloglar mümkün olduğunca Auto Size / Fit Content kullanmalıdır.
- Responsive davranış korunmalıdır.

Davranış

- UI düzenlemeleri mevcut işlevleri değiştirmemelidir.
- Kullanıcı akışı korunmalıdır.
- Mevcut davranışlar bozulmamalıdır.

UI İncelemesi

Bir UI değişikliği tamamlanmadan önce mutlaka doğrula:

- Gereğinden büyük kontroller küçültüldü mü?
- Gereksiz boşluklar kaldırıldı mı?
- Pencere içerikten büyük değil mi?
- Responsive davranış korunuyor mu?
- Masaüstü uygulaması yoğunluğu sağlandı mı?

### Teslim Öncesi

Teslim etmeden önce doğrula:

- Sadece gerekli dosyalar değiştirildi.
- Gereksiz refactor yapılmadı.
- Gereksiz kod eklenmedi.
- Geçici debug kodları kaldırıldı.
- Kod mevcut proje standartlarına uyuyor.

### Mevcut Kodu Koruma

- Kullanıcı açıkça istemediği sürece mevcut davranışı değiştirme.
- Yeni geliştirmeler mevcut özellikleri bozmamalıdır.
- Geriye dönük uyumluluk korunmalıdır.

### Mimari Tutarlılık

- Yeni kod mevcut proje mimarisine uygun olmalıdır.
- Aynı problemi çözen ikinci bir yapı oluşturma.
- Mevcut servisler, ViewModel'ler ve yardımcı sınıflar kullanılabiliyorsa yeniden kullanılmalıdır.

### Kod Teslimi

Kod teslim edilirken:

- Kullanılmayan kod bırakma.
- TODO bırakma.
- Debug kodu bırakma.
- Geçici workaround bırakma.

## Proje Kuralları

### 1. Kuralların Sürekli Kontrolü (MUTLAK KURAL)
* **Kural:** Asistan, gelen **HER promptta (kullanıcı isteğinde)** bu dosyadaki (`AGENTS.md`) tüm kuralları mutlak suretle gözden geçirmek ve kararlarını/eylemlerini mutlaka bu kurallara göre şekillendirmek zorundadır. Bu dosyadaki kurallar sadece yeni bir konuşmanın başında değil, her bir adımda ve her bir cevapta geçerlidir.

### 2. İşlem Geçmişi ve Sürümleme (Changelog)

* Her geliştirme tamamlandıktan sonra yapılan değişiklikler **mutlaka** [changelog.md](file:///c:/VSRepos/FinTrack/changelog.md) dosyasına işlenmelidir.
* Changelog yalnızca büyük geliştirmeler için değil, küçük hata düzeltmeleri ve ara geliştirmeler için de güncellenmelidir.
* Changelog bu proje için geliştirme günlüğü (Development Journal) olarak kullanılmaktadır.
* Changelog bu projenin güvenilir geliştirme geçmişidir.

#### Format

* Değişiklikler aşağıdaki standart formatta eklenmelidir:
  ```markdown
  ## [Sürüm_Numarası] - YYYY-MM-DD — Kısa Geliştirme Başlığı

  ### Added
  - Yeni eklenen özelliklerin açıklaması

  ### Fixed
  - Düzeltilen hataların açıklaması

  ### Changed
  - Mevcut özelliklerde yapılan değişiklikler
  ```

#### Versiyonlama

* Yeni sürüm numarası belirlenmeden önce [changelog.md](file:///c:/VSRepos/FinTrack/changelog.md) dosyasındaki **en son sürüm mutlaka okunmalıdır.**
* Sürüm numarası hiçbir zaman varsayılmamalıdır.
* Yeni sürüm, mevcut son sürüm üzerinden **Semantic Versioning (Major.Minor.Patch)** kurallarına göre artırılmalıdır.
* Aynı sürüm numarası ikinci kez kullanılmamalıdır.

### 3. Mükerrer Geliştirme Kontrolü (Changelog Analizi)
* **Kural:** Her geliştirme veya hata düzeltme talebi öncesinde, talep edilen özelliğin veya benzer bir çözümün daha önce yapılıp yapılmadığını doğrulamak için mutlaka [changelog.md](file:///c:/VSRepos/FinTrack/changelog.md) dosyası incelenmelidir.
* **Uyumlu Tasarım:** Düzeltme önerileri ve eklenecek yeni kodlar, geçmiş sürümlerdeki çözümlerle çelişmeyecek ve onları tamamlayacak şekilde tasarlanmalıdır.

### 4. Derleme ve Çalıştırma Süreci (Build & Run) - KESİN KURAL
* **Kural:** KESİNLİKLE VE HİÇBİR KOŞULDA Asistan kendi başına `dotnet build`, `dotnet run` veya benzeri herhangi bir derleme/çalıştırma komutunu tetiklememelidir! Asistan sadece kod yazar ve mantığı kurar. Derleme işlemi, Hata ayıklama (Debug) ve test etme süreci SADECE kullanıcı tarafından Visual Studio üzerinden yürütülecektir. Kullanıcı açıkça "şu komutu çalıştır" demediği sürece, test amacıyla dahi olsa arka planda `run_command` aracıyla derleme komutu KULLANILAMAZ.

### 5. Veri Güvenliği İlkeleri (KRİTİK)
* Kod değişikliklerinde mevcut yorum satırları, mimari yapı ve veritabanı şifreleme/güvenlik standartları korunmalıdır.
* **Log Güvenliği:** Hassas veriler (DEK, şifreler, kurtarma kodları, IBAN, API anahtarları) `Debug.WriteLine`, `AppLogger` veya herhangi bir log/konsol çıktısına **kesinlikle yazılmamalıdır**. Hata loglarında yalnızca exception mesajı ve stack trace bilgisi yer almalıdır.
* **Veritabanı Yapısı Değişiklik Kontrolü:** Yeni bir özellik veritabanı yapısını değiştiriyorsa (ALTER TABLE, yeni sütun, migration), eklenen alanın hassas veri içerip içermediği ve uygun şifreleme stratejisi (alan bazlı AES mi, yoksa SQLCipher disk şifrelemesi yeterli mi) kullanıcıyla birlikte değerlendirilmelidir.
* **Güvenlik-Kritik Sınıf Müdahale Analizi:** `CryptoProvider`, `SettingsManager`, `EncryptionService` ve `AppDbContext.OnConfiguring` gibi güvenlik kritik sınıflara yapılacak her değişiklik, yan etkileri (anahtar bozulması, veri erişim kaybı, şifreleme uyumsuzluğu) açısından analiz edilmeli ve kullanıcıya olası riskler bildirilmelidir.

### 6. Manuel Veri Koruma ve Onay Mekanizması
* **Kural:** Sistem, bir dokümandan veya harici bir kaynaktan (örn: ekstre aktarımı, Excel import vb.) otomatik veri işlerken, kullanıcının daha önceden **elle girdiği (manuel) veriler asla silinmemeli veya üzerine yazılmamalıdır**. Manuel veriyle çakışan veya değiştirilmesi gereken bir durum tespit edilirse, işlem yapılmadan önce mutlaka kullanıcının görüşü ve onayı alınmalıdır.

### 7. Şifreleme (Encryption) ve Ayar Dosyası (settings.json) Güvenliği (KRİTİK)
* **Kural:** `settings.json` dosyasını (özellikle `EncryptedDataKey` anahtarını) değiştiren, üzerine yazan veya şifreleme algoritmasına (`CryptoProvider`, AES altyapısı, SQLCipher DB kilitleri) müdahale eden herhangi bir işlem yapmadan önce; asistana verilen talimat ne kadar basit görünürse görünsün (örneğin sadece bir tema ayarı değiştiriliyor olsa bile), **MUTLAKA** kullanıcıya olası riskler açıklanacak ve işleme devam etmek için açıkça "Emin misin?" şeklinde **3 FARKLI ONAY AŞAMASI** (veya belirgin, tekrarlı uyarılar) sunulacaktır. Kullanıcı riskleri tamamen kabul edene kadar bu dosyalara dokunulmayacaktır.

### 8. Karakter Kodlaması ve Dosya Düzenleme (ZORUNLU - Mojibake Koruması)
* **Kural:** Proje, yapısı gereği her aşamada ve her kod dosyasında Türkçe karakterler ve emojiler barındırmaktadır. Bu nedenle, markdown ve kod dosyalarında değişiklik yaparken (okuma/yazma/düzenleme) **MUTLAKA UTF-8 kodlaması (encoding)** kullanılması bizim için **vazgeçilmez bir zorunluluktur (MUST)**. Varsayılan sistem karakter seti ne olursa olsun, dosyalar asla ANSI, Windows-1254 veya farklı bir formatta okunup tekrar UTF-8 olarak kaydedilmemelidir. Aksi halde "Mojibake" (çift kodlama) sorunu oluşur ve tüm Türkçe veri kalıcı olarak bozulur.

### 9. Çoklu Dil (i18n) Uyumluluğu ve Yeni Geliştirmeler
* **Kural:** Uygulamanın İngilizce/Türkçe çift dil desteği tamamen kurulmuştur. Bundan sonra yapılacak **tüm yeni geliştirmelerde, eklenecek yeni ekranlarda veya bileşenlerde**, arayüzün (UI) hem Türkçe hem de İngilizce dil desteğine (i18n) sahip olması gerektiği **kesinlikle unutulmamalıdır**. Sabit/statik metinler kod içine veya XAML/AXAML dosyalarına doğrudan yazılmamalı, mutlaka `Strings.resx` ve `Strings.en.resx` dosyalarına eklenerek `LocalizationService` veya `loc:Translate` üzerinden çağrılmalıdır.

## Knowledge Base

### Şifreleme ve Altyapı Keşifleri
Asistanın geçmiş geliştirmelerde FinTrack mimarisi hakkında keşfettiği kritik bilgiler şunlardır:
1. **Anahtar Bağımlılığı**: `settings.json` içindeki `EncryptedDataKey`, uygulamanın veritabanı (SQLCipher) ve alan bazlı (AES) şifrelemelerinin kilit taşıdır. Bu anahtar bozulduğunda tüm veri erişimi kopar.
2. **Kalıntı Şifreli Veriler**: Geçmişte veritabanına şifrelenerek atılan `BankName` ve `IBAN` gibi alanlar iki formatta bulunabilir: Ön eksiz düz Base64 veya `G:base64...` şeklinde "G:" ön ekine sahip metinler. Bu veriler `TryDecryptValue` gibi akıllı metotlarla çözümlenmelidir.
3. **ValueConverter Çökmesi**: Entity Framework `AppDbContext` içerisinde `ValueConverter` kullanılarak on-the-fly (anında) şifre çözme işlemleri eklemek, LINQ sorgularını (özellikle `OrderBy` ve `Contains`) bozarak `InvalidOperationException` fırlatır. Bu sebeple UI'da gösterilecek metin verileri (Banka İsimleri vb.) için `ValueConverter` **kullanılmamalıdır**. Yerine, uygulama açılışında verileri kalıcı olarak çözen Migration metotları tercih edilmelidir.
4. **BankName ve IBAN Alanlarına Şifreleme YASAĞI (KRİTİK)**: `BankAccount.BankName`, `BankAccount.IBAN` ve `CreditCardAccount.BankName` alanlarına Entity Framework `ValueConverter` ile veya doğrudan `CryptoProvider.Encrypt()` çağrısıyla **alan bazlı şifreleme UYGULANMAZ**. Bu alanlar veritabanında **düz metin** olarak saklanır. Veritabanının kendisi zaten SQLCipher ile tam disk şifrelemesine sahip olduğundan ek alan şifrelemeye gerek yoktur. Geçmişte (beta.23) bu yaklaşım denenmiş ve anahtar bozulma krizi (beta.45) sonrasında bazı kayıtların **kalıcı olarak çözülemez** hale gelmesine yol açmıştır. `ManageAccountsViewModel` ve `ManageCardsWindow` bu alanları düz metin olarak kaydeder; bu davranış **korunmalıdır**.
5. **Boş Veritabanı (Yeni Profil) ve EF Core Migrate() Çakışması**: SQLCipher kullanırken Entity Framework Core'un standart `context.Database.Migrate()` fonksiyonu yepyeni/boş bir veritabanı dosyasında çalıştırıldığında (bağlantı durumlarını yanlış yönettiği için) `PRAGMA key` şifresini kaybedip çöker ve hiçbir tabloyu oluşturamaz. Bu yüzden tamamen boş veritabanlarında `Migrate()` yerine mutlaka `context.Database.EnsureCreated()` kullanılmalıdır. `EnsureCreated()`, önceden açık olan ve şifresi girilmiş SQLite bağlantısını bozmadan (SQLCipher'ı zedelemeden) tüm tabloları şifreli kasanın içine kusursuzca kurar. `LoginViewModel` içindeki bu fallback mekanizması **kesinlikle değiştirilmemelidir.**

### Performans Keşifleri

### UI Keşifleri

### EF Core Keşifleri

### Avalonia Keşifleri

### SQLite Keşifleri

### Bilinen Tuzaklar
