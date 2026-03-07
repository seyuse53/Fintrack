# Changelog

All notable changes to this project will be documented in this file.

## [0.9.4] - 2026-03-02

### Added
- **Küresel Tarih Filtresi (Global Date Filter)**: Ana ekranın tepe menüsüne modern görünümlü bir tarih (Ay/Yıl) filtresi eklendi. Artık geçmiş aylara ait gelir/gider detaylarını ve toplam harcamaları tek tıkla görebilirsiniz.
- **Rakam Biçimlendirme (Binlik Ayracı)**: İşlem Ekleme, Kredi Kartı Ödemesi, Yatırım Alım/Satım vb. dahil uygulama genelindeki tüm tutar giriş kutularına anlık binlik ayraç (örn: 1.250,50) düzeltmesi eklendi. Girdi deneyimi çok daha finansal standartlara uygun hale getirildi.
- **Akıllı Profil İçe Aktarımı**: `.db` dosyasından mevcut bir bağlantı kurulurken veritabanı dosyasının ismi otomatik olarak profil adı olarak doldurulacak şekilde iyileştirildi.

### Fixed
- **Modern Bilgilendirme ve Onay Pencereleri**: Ayarlar ve veri onay süreçlerinde karşınıza çıkan eski nesil Windows mesaj eklentileri (`MessageBox`) yerine uygulamanın koyu/açık temasına uygun modern `InfoDialogWindow` ve `GeneralConfirmWindow` tasarımlarına geçildi.

## [0.9.3] - 2026-03-01

### Added
- **Modern Şifre Değiştirme UI**: `Ayarlar > Güvenlik` sekmesi modern, kart tabanlı bir tasarıma ve 3D gölge efektlerine ![alt text](image.png)geçirildi.
- **Premium Mesaj Kutuları**: Ayarlar menüsündeki tüm eski Windows `MessageBox` uyarıları, uygulamanın genel temasına uygun `GeneralConfirmWindow` ile değiştirildi.
- **Ana Başlık Versiyon Gösterimi**: Uygulama versiyon numarası (Örn: v0.9.3) artık ana pencerenin sağ alt köşesindeki durum çubuğunda (StatusBar) dinamik olarak görünüyor.
- **Görsel Güncelleme İlerleyişi**: Güncelleme penceresine (Update Window) gerçek zamanlı yüzde (%) ve MB bazlı indirme ilerleme çubuğu eklendi. Alt alta çakışan yazı sorunları giderildi.
- **Şifre Kutusu Düzenlemesi**: Ayarlar ekranındaki şifre giriş kutularının bazı ekranlarda daralması (collapse) engellendi ve yerleşim optimize edildi.

### Fixed
- **Uygulama Simgesi (App Icon) Şeffaflığı ve Görünürlük Sorunu**: 
  - Görev çubuğunda ve pencere başlıklarında simgenin etrafında oluşan beyaz çerçeve (damalı arka plan) özel bir işleme algoritması ile temizlenerek **gerçek şeffaflık** sağlandı.
  - Simgelerin bazı pencerelerde (Örn: Giriş Ekranı) kaybolmasına sebep olan kaynak bağlama (Resource) hataları giderildi.
  - Simgelerin görünürlüğünü tüm pencerelerde standartlaştırmak için `App.xaml` üzerine global bir stil kuralı eklendi. Hem kaliteyi artırmak hem de sorunları aşmak için başlık çubuklarında yüksek çözünürlüklü **PNG** formatına, genel Windows kullanımı için ise **ICO** formatına geçildi.
- **Otomatik .keys Onarımı**: Eski profillerde "DatabasePath" boş olduğu için `.keys` dosyasının oluşmasını engelleyen kritik mantık hatası giderildi. Artık her girişte dosya otomatik doğrulanır/oluşturulur.
- **Gelişmiş Güncelleyici (Updater)**: İndirme işlemlerinde `User-Agent` eksikliği nedeniyle oluşan hatalar giderildi ve güncelleme betiği (bat), uygulama kapanmadan dosya değişimini zorlamayacak şekilde güçlendirildi.
- **GitHub API Erişimi**: Gizli repository nedeniyle güncellemelerin 404 vermesi sorunu için genel erişim altyapısı optimize edildi.

## [0.9.1] - 2026-03-01
- **Otomatik Kilitleme (Auto-Lock)**:
  - Uzun süreli hareketsizlik (mouse/klavye kullanılmaması) durumunda uygulamanın otomatik olarak bir şifre kalkanı ile kilitlenmesi sağlandı.
  - Sizi verilerinizden uzaklaştığınızda güvenlik altına alan bu ekran, sadece uygulamanın ana şifresi ile açılabiliyor.
  - Otomatik kilitleme süresi varsayılan olarak **3 dakika** ayarlı olarak gelir. İstenirse `Ayarlar > Güvenlik` menüsünden süre değiştirilebilir veya tamamen kapatılabilir.
- **Uygulama İçi Otomatik Güncelleme (Auto-Updater)**:
  - FinTrack'in yeni sürümlerinin otomatik olarak tespit edilmesi ve tek tuşla indirilip kurulması için altyapı geliştirildi.
  - Uygulama sadece her başlangıçta GitHub üzerindeki kararlı (Release) sürümleri denetler ve "Yeni Sürüm Var" uyarısı ile beraber o sürüme ait yenilik notlarını ekranda gösterir.
  - Arka planda çalışan güncelleyici sistemi (updater), eski dosyayı silip yeni kurulan `.exe` dosyası ile uygulamayı saniyeler içinde baştan başlatır. Tamamen ücretsiz ve güvenli GitHub altyapısı üzerine inşa edildi.
- **Güvenli Otomatik Yedekleme (Auto-Backup)**: Uygulama kapanırken (veya profil değiştirirken) veritabanınızın şifreli bir kopyasını istediğiniz bir klasöre (örn. OneDrive, Google Drive) otomatik olarak yedekleyen gelişmiş veri koruma sistemi eklendi.
    - **Akıllı Temizlik**: "Sadece son 5 yedeği tut" veya "30 günden eski yedekleri sil" gibi kurallarla disk alanınızın dolması engellenir.
    - **İlk Kurulum Asistanı**: Kullanıcı yedekleme klasörü seçmemişse, sisteme ilk girişinde otomatik uyarı/öneri penceresi çıkarak "Veri güvenliğiniz için yedek klasörü seçin" şeklinde yönlendirme yapar.
- **Kredi Kartı Yönetimi İyileştirmeleri**:
    - Kart ekleme ekranı, kafa karışıklığını önlemek için açıklayıcı başlıklar ve daha düzenli bir yerleşimle yenilendi.
    - Bağlı (ana) kart seçim listesinde banka ve kart adı beraber gösterilecek şekilde (Örn: *Yapı Kredi - Master*) güncellendi.
- **Veritabanı Taşınabilirliği (Sidecar Keys)**:
    - Veritabanı dosyası başka bir bilgisayara taşındığında şifreleme bilgilerinin de taşınabilmesi için otomatik `.keys` dosyası altyapısı kuruldu.
    - Mevcut bir veritabanı dosyası seçildiğinde sistem anahtarları otomatik algılar ve yeni şifre oluşturmak yerine mevcut şifreyle giriş yapılmasını sağlar.
- **Giriş Ekranı Güncellemesi**: Uygulama versiyon numarası artık giriş ekranının sağ alt köşesinde dinamik olarak görünüyor.

- **Güçlendirilmiş Şifreleme (PBKDF2 & Salt)**: Veri gizliliği ve güvenliği "Askeri Düzey" (Military-Grade) standartlarına yükseltildi. 
    - Uygulama şifreleri (`HashedPassword`) artık düz SHA-256 yerine, brute-force (kaba kuvvet) saldırılarını imkansız kılan **PBKDF2** algoritması ve rastgele **Salt** kullanılarak şifreleniyor. 
    - Eski sürümlerde oluşturulan şifreler, kullanıcı uygulamaya ilk giriş yaptığında *otomatik* ve kesintisiz olarak yeni yüksek güvenlikli PBKDF2 altyapısına yükseltiliyor (Geriye dönük tam uyumluluk).

## [0.9.0] - 2026-02-28
- **Gelişmiş Raporlama Merkezi (`ReportsView`)**: Uygulamaya görsel istatistikler ve analiz sekmeleri eklendi.
    - **Harcama Dağılımı**: Kategorilere göre harcamaları görsel barlar ve yüzdelerle özetler.
    - **Aylık Trendler**: Son 6 ayın Gelir vs Gider karşılaştırmasını grafiksel olarak gösterir.
    - **Yatırım Analizi**: Portföy dağılımını, toplam varlık değerini ve kâr/zarar durumunu analiz eder.
- **Modern Profil Yönetimi (`AddProfileWindow`)**: Çoklu profil desteği modern bir arayüzle yenilendi.
    - **Veritabanı İçe Aktarma**: Yeni profil oluştururken bilgisayardaki mevcut bir `.db` dosyasını seçip bağlama opsiyonu eklendi.
    - **İzole Veritabanı**: Her profilin kendi `fintrack_ProfilAdı.db` dosyası ile tam izolasyon sağlandı.
- **Güvenli Profil Silme Akışı**: Profil silme işlemi artık hata payını sıfıra indiren ve güvenliği artıran 3 aşamalı bir akışa sahip:
    - **Aşama 1 (Onay)**: İşlemin ciddiyetini belirten ilk onay diyaloğu.
    - **Aşama 2 (Şifre)**: Sadece bu işlem için açılan profesyonel şifre doğrulama penceresi.
    - **Aşama 3 (Veri Seçimi)**: Ayarların mı yoksa veritabanının mı silineceğine dair son karar ekranı.
- **Modern Tasarımlı Onay Pencereleri**: Standart Windows mesaj kutuları yerine projenin premium tasarımına uygun özel pencereler eklendi:
    - `ConfirmPasswordWindow`: İşlem onayları için şık şifre giriş kutusu.
    - `DeleteDataConfirmWindow`: Veri silme tercihleri için yüksek kontrastlı ve net seçenekli onay ekranı.
    - `GeneralConfirmWindow`: Uygulama geneli için modernize edilmiş esnek onay diyaloğu.
- **Profil Bağlantısını Kesme ('-' Butonu)**: Giriş ekranındaki `-` butonu, profili tamamen silmek yerine sadece uygulamadan "ayırmak" (unlink) işleviyle geri getirildi. Veritabanı dosyasına dokunulmadan profil listeden kaldırılır.
- **Standart Veritabanı Konumu**: Yeni oluşturulan profillerin veritabanı dosyaları artık daha güvenli ve silinmeye karşı dirençli olan `Belgelerim\FinTrack` klasörüne otomatik olarak kaydediliyor.
    - Eski profillerin bulunduğu klasörler muhafaza edildi. "Sıfırla" veya "Sil" işlemleri her iki klasörü de temizleyecek şekilde güçlendirildi.
    - Profil oluşturma ekranında veritabanı yolunun nereye oluşacağı dinamik olarak gösterildi.
- **Onay ve Uyarı Pencereleri Modernizasyonu:**
    - Uygulama genelinde bulunan standart ve eski stil Windows `MessageBox` pop-up'larının tamamı, yeni `GeneralConfirmWindow` yapısına geçirildi.
    - `GeneralConfirmWindow`, iptal butonuna ihtiyaç duymayan "Bilgi" veya "Hata" pop-up'ları için 'Sadece Tamam' destekli hale getirildi.
- **Profil Bazlı Sıfırlama**: Artık "Uygulamayı Sıfırla" yerine, kullanıcıların sadece seçili profilin verilerini güvenle temizleyebileceği izolasyon mantığına geçildi.
- **Giriş Ekranı ve UX İyileştirmeleri**: 
    - Giriş ekranından riskli `-` butonu kaldırılarak yetki "Profili Sil" linkine taşındı (Sonraki güncellemeyle `-` butonu güvenli ayrıştırma işleviyle geri döndü).
    - Tüm pencereler (Özellikle `RecoveryCodeWindow`) `SizeToContent="Height"` ile içeriğe, Windows metin küçültme/büyütme (DPI) ayarlarına tam uyumlu hale getirildi. Artık butonların veya metinlerin yarım kalma sorunu yok.

### Fixed
- **XAML Compilation (MC1000 & MC3000 & MC3015)**: `ReportsView.xaml` ve `LoginWindow.xaml` dosyalarındaki derleme hataları (XAML Syntax) düzeltildi.
- **UI Sığmama Sorunu**: Şifre onay ve profil ekleme pencerelerindeki butonların kaybolma/sığmama sorunu giderildi.

## [0.8.0] - 2026-02-23

### Added
- **Yatırım Portföyü Modülü (`InvestmentsView`)**: Hisse senedi, döviz, altın ve kripto para gibi varlıkları takip etmek için detaylı kâr/zarar göstergeli yeni ekran eklendi.
- **Yatırım Alım ve Satım İşlemleri**: Dinamik maliyet ve mevcut değer hesaplamalarına sahip alım ve satım operasyon panelleri (Pencereler) tasarlandı.
- **Kredi Kartı ve Banka Entegrasyonu**: Yatırım işlemlerinde doğrudan sistemde kayıtlı Banka Hesapları veya Kredi Kartlarından ödeme yapma (ya da tahsil etme) özelliği sisteme bağlandı. Alım sırasında cüzdandan limit/bakiye düşürülürken, satım işleminde gelir hesaba aktarılır.
- **Masraf ve Komisyon (Fee) Takibi**: Yatırım işlemleri sırasında oluşan aracı kurum kesintilerini sisteme dâhil etmek için Komisyon alanı eklendi ve toplam maliyet/gelir hesaplamasına entegre edildi.
- **Yahoo Finance API Entegrasyonu**: Döviz (USD, EUR vb.), Emtia (XAU-Gram Altın çevirisi ile) ve BIST Hisse Senetleri için ücretsiz ve canlı fiyat çekme köprüsü (API) entegre edildi.
- **Kişiselleştirilmiş API Ayarları ve Kayıt**: `SettingsView` içerisine yeni bir "API ve Entegrasyonlar" sekmesi (Kılavuz sekmesi ile Bulut sekmesi arasına taşındı) eklenerek uygulamanın fiyat takibi *Sadece Manuel*, *Yahoo Finance* ve kişisel veri sunucularınız için *Özel API (Custom URL)* modlarına bölündü.
- **Bulut Senkronizasyonu Ayar Kontrolü**: Bulut Senkronizasyonu (Google Drive / OneDrive) sekmesindeki klasör seçimi adımına da "Ayarları Kaydet" butonu eklendi. Böylece, klasör seçildikten hemen sonra arka planda yapılan işlemler sadece kayıt onayı verildiğinde tetikleniyor.
- **Ayarları Kaydet Desteği**: API ve Bulut sağlayıcı seçimlerinin kaydedilmesi onaylı hale getirildi.
- **Panel Refaktoringi ve UX Düzeltmeleri**: `AddInvestmentWindow`, `SellInvestmentWindow` ve `UpdatePricesWindow` pencerelerindeki ScrollViewer sınırlandırmaları kaldırılarak `SizeToContent="Height"` sistemine geçiş yapıldı. Pencere altındaki gereksiz gri alanlar yok edildi.
- **Menü Optimizasyonu**: Kullanıcının ana menüdeki erişim sıklığı hedeflenerek "📈 Yatırımlar" butonu "📊 Raporlar" butonunun hemen üstüne taşındı.

## [0.7.0] - 2026-02-22

### Added
- **Banka Hesapları Altyapısı**: Sisteme banka hesapları (`BankAccount` modeli) entegre edildi ve veritabanı altyapısı (Migration) güncellendi.
- **Hesaplarım Ekranı (`AccountsView`)**: Kullanıcıların tüm banka hesaplarını, açılış bakiyelerini ve güncel bakiyelerini görebileceği yeni SPA ekranı eklendi (Sol menüden erişilebilir).
- **Banka Hesabı Yönetimi**: Banka hesaplarını ekleme, düzenleme, aktif/pasif yapma ve silme işlemlerini barındıran **Banka Hesaplarım** yönetim paneli eklendi (IBAN desteği mevcut).
- **Gelişmiş Transfer Sistemi (`TransferWindow`)**: Hesaplar arası çift yönlü para transferlerini destekleyen özel sistem eklendi.
  - **EFT / Havale**: Bankadan bankaya transfer.
  - **Kredi Kartı Ödemesi**: Banka veya nakit hesabından kredi kartı borç ödemesi.
  - **ATM Nakit**: Bankadan nakit çekme ve nakitten bankaya para yatırma (Cash to Bank / Bank to Cash).
- **Kredi Kartı Ödemesi Kaynak Seçimi**: "Kartlarım" menüsü üzerinden yapılan hızlı ödemelerde (`MakePaymentWindow`) artık ödemenin hangi hesaptan (Nakit veya herhangi bir aktif Banka Hesabı) düşüleceği seçilebiliyor. Bu sayede kart borcu ödenirken, ilgili banka hesabının veya toplam nakit varlığın bakiyesi de otomatik olarak azaltılarak "Toplam Varlık" bakiyesi ile tam uyum sağlanıyor.
- **Ödeme Yöntemi Cüzdan Seçimi**: İşlem Ekleme (`AddTransactionWindow`) ve İşlem Düzenleme (`EditTransactionWindow`) panellerine "Nakit" veya kayıtlı "Banka Hesapları" seçenekleri üzerinden işlem yapma imkanı eklendi. Sabit bir Gelir (örn: Maaş, EFT) veya gider (örn: Kira) eklerken doğrudan ilgili banka hesabı seçilerek otomatik bakiye düşümü/artışı sağlanır.
- **Gelişmiş Dashboard Toplamı**: Ana Ekranda (Dashboard) yer alan *Güncel Bakiye*, banka açılış tutarları ile birleştirilerek **Toplam Varlık (Nakit + Banka)** şekline dönüştürüldü.
- **Hesap Hareketleri Detay Penceresi (`AccountDetailWindow`)**: Her banka hesabı için tüm gelir, gider ve transfer geçmişini tarih sırasına göre gösteren detaylı inceleme ekranı eklendi. "Hesaplar" sekmesindeki "Hareketler" butonuyla erişilebilir.
- **Kullanım Kılavuzu (Help Section)**: Ayarlar menüsüne uygulamanın detaylı kullanım rehberini içeren "📖 Kullanım Kılavuzu" sekmesi (Tab) eklendi. Banka işlemleri, asıl/bağlı kart mantığı ve bütçe enflasyonu gibi detaylar akordeon yapısında sunuldu.
- **Kullanıcı Deneyimi (UX)**: `Banka Hesapları Yönetimi` penceresindeki tüm metin kutularına (TextBox) şık WPF stilleriyle yer tutucu (Watermark/Placeholder) ipucu metinleri (örn. "Banka Adı (Garanti)", "Açılış ₺" vb.) eklendi.


## [0.6.0] - 2026-02-21

### Added
- **Modern Uygulama Simgesi (App Icon)**: FinTrack için Türk Lirası (₺) temalı, yüksek çözünürlüklü gümüş madeni para tasarımlı yeni ikon eklendi. Hem uygulama penceresinde hem de derlenmiş `.exe` dosyasında (Masaüstü/Görev Çubuğu) görünür hale getirildi.
- **Truly Single-File Publish**: Yayınlama (Publish) süreci mükemmelleştirildi. Artık native kütüphaneler (SQLCipher vb.) EXE içerisine gömülür, gereksiz hata ayıklama dosyaları (.pdb) temizlenir ve dosya boyutu sıkıştırılarak tam taşınabilir tek bir dosya üretilir.

## [0.5.0] - 2026-02-21

### Added
- **Merkezi Ayarlar Ekranı (`SettingsView`)**: Güvenlik ve yapılandırma ayarları tek bir modern ekran altında toplandı.
    - **🔒 Güvenlik Sekmesi**: Şifre değiştirme işlemleri artık uygulama içinden direkt yapılabiliyor.
    - **☁️ Bulut Senkron Sekmesi**: Veritabanını bulut klasörlerine taşıma ve yönetme özelliği eklendi.
    - **ℹ️ Hakkında Sekmesi**: Uygulama sürümü ve geliştirici bilgileri eklendi.
- **Tam Veritabanı Şifrelemesi (SQLCipher)**: Veritabanı dosyası (`fintrack.db`) bir bütün olarak AES-256 ile şifrelendi. Rakamlar, tarihler ve kategoriler dahil tüm içerik koruma altına alındı.
- **Dinamik Şifreleme Yönetimi**: Uygulama açılışında anahtar doğrulaması ve şifresiz veritabanından şifreliye otomatik geçiş (migration) sistemi eklendi.
- **Bulut Senkronizasyonu (Google Drive, OneDrive, Dropbox)**: Veritabanını herhangi bir klasöre taşıma ve o klasörden çalıştırma (Dynamic DB Path) desteği eklendi.
- **Dashboard UI İyileştirmeleri**: "Alacak" ve "Borç" sütunları için bağımsız kaydırma (Scroll) desteği eklendi. "Fatura Dağılımı" paneli daha iyi bir mantıksal gruplama için Giderler (Borç) tarafına taşındı.
- **Sidebar Yenilemesi**: Yan menü sadeleştirildi; "Bulut Senkron" butonu Ayarlar içerisine taşındı, "Şifre ve Güvenlik" butonu "⚙️ Ayarlar" olarak güncellendi.

### Fixed
- **Veritabanı Erişim Hatası (File in Use)**: SQLCipher şifreleme işlemi sırasında dosya kilitleme sorunu yaşayan bağlantı havuzu (Pooling) çakışması giderildi.

### Removed
- **Bağımsız Pencereler**: Artık kullanılmayan `ChangePasswordWindow` ve `CloudSyncView` modal pencereleri kaldırıldı.

## [0.4.0] - 2026-02-21

### Added
- **Gelişmiş Raporlar Görünümü (`ReportsView`)**: Belirli tarih aralıklarına göre gelir/gider analizi yapabilen yeni SPA ekranı eklendi.
- **Dinamik Kredi Kartı Ekstre Yönetimi**: Kredi kartları için "Hesap Kesim Günü" ve "Son Ödeme Günü" özellikleri eklendi. Borç takibi artık takvim ayı yerine ekstre dönemine göre yapılıyor.
- **Aktif Kartlar Ekranı (`CardsView`)**: Kartlarım menüsü tamamen yenilenerek SPA tarzı interaktif bir ekrana dönüştürüldü.
    - Kalan borç ve ekstre kesimine kalan gün sayısını gösteren akıllı ilerleme çubukları (Progress Bars) eklendi.
    - Kart bazlı hızlı ödeme ve detay inceleme butonları eklendi.
- **Asıl ve Bağlı (Sanal) Kart Yönetimi**: Birden fazla kartın borcunu tek bir "Ana Kart" altında birleştirme (Consolidation) özelliği eklendi. Harcamalar ayrı takip edilirken borçlar tekilleştirildi.
- **Kategori Hiyerarşisi (Alt Kategoriler)**: Kategorilere hiyerarşi desteği eklendi. "Faturalar" gibi ana kategoriler altında "Elektrik", "Su", "İnternet" gibi alt kategoriler tanımlanabiliyor.
- **Hiyerarşik İşlem Yönetimi**: İşlem ekleme ve düzenleme pencereleri, kategorileri hiyerarşik bir yapıda (örn. Faturalar > Elektrik) gösterecek şekilde yenilendi.
- **⚡ Fatura Dağılım Paneli**: Ana ekrana (Dashboard), o ayki fatura harcamalarınızı türlerine göre (Elektrik: ₺X, Su: ₺Y vb.) anlık özetleyen dinamik bir panel eklendi.
- **Kredi Kartı Borç Ödeme**: `MakePaymentWindow` üzerinden kart borcu ödeme özelliği eklendi. Ödemeler çift sayımı engellemek için "Transfer" türünde kaydediliyor.
- **Yeni İşlem Türü: Transfer**: Gelir ve Gider dışında, hesaplar arası para transferi (örn. kart ödemesi) için yeni bir kategori türü eklendi.

### Fixed
- **Tarih Hesaplama Sistemi**: Kredi kartı ekstre dönemleri, gün sonu (23:59:59) ve gün başı sınırlarına göre daha hassas hesaplanacak şekilde güncellendi.
- **Veri Doğrulama**: Kart eklerken/güncellerken kesim ve ödeme günlerinin zorunlu ve geçerli (1-31) olması sağlandı.
- **Uyumluluk Yaması**: Eski kayıtlarda `0` olarak kalmış olan kesim günü bilgilerinin uygulamayı çökertmesi engellendi (Otomatik güvenlik bariyeri).

## [0.3.0] - 2026-02-21

### Added
- **Tek Sayfa Uygulaması (SPA) Mimarisi**: Uygulama içi gezinti sistemi yenilendi. "Bütçe" ve "Ana Ekran" gibi bölümler artık yeni pencere açmak yerine ana ekran içerisinde dinamik olarak (UserControl ile) değişiyor.
- **Yanal Navigasyon (Sidebar)**: Ana pencereye modern bir sol menü eklendi. Gelişmiş görsellik için navigasyon butonları bu menüye taşındı.
- **Hızlı Kategori Seçimi**: "İşlem Ekle" penceresindeki kategori listesi artık türüne göre (önce Gelir/Yeşil, sonra Gider/Kırmızı) sıralanarak gösteriliyor.
- **Taşınabilirlik (SQLite Geçişi)**: SQL Server bağımlılığı tamamen kaldırıldı. Uygulama artık yerel bir `fintrack.db` dosyası kullanıyor.
- **Cross-Platform Altyapısı**: Veritabanı ve çekirdek mantık (Core) macOS ve Linux ile uyumlu hale getirildi.
- **Tek Dosya Yayınlama (Publish)**: Kurulum gerektirmeyen, taşınabilir `.exe` oluşturma desteği eklendi.
- **Güvenlik (Şifre Değiştir)**: Ana ekrana `🔑 Şifre` butonu eklendi. Eski şifreyi doğrulayarak yeni şifre belirlenebilir; şifreli veriler etkilenmez.
- **Bütçe Yönetimi**: Her gider kategorisi için aylık harcama limiti tanımlama sistemi eklendi.
- **Otomatik Aylık Sıfırlama**: Bütçe takibi her ay başında otomatik olarak sıfırlanır; geçmiş aylık veriler kaybolmaz.
- **Yıllık Karşılaştırma**: Aynı aydı bir önceki yıla göre harcama farkını gösteren karşılaştırma satırı eklendi.
- **Enflasyon Düzeltmesi (TÜİK)**: TÜYFE verisiyle düzeltilmiş gerçek harcama artışı/azalışı hesaplaması. TÜİK açık API'si (kayıt gerektirmez) kullanılır; internet yoksa yerel cache ve gömülü 2023-2026 TÜFE tablosu devreye girer.
- **Bütçe Penceresi (`BudgetWindow`)**: Ay navigasyonu, ilerleme çubukları, durum renklendirmesi (Yeşil / Sarı / Kırmızı) ve limit düzenleyici panel içeren yeni ekran.
- **Ana Menü Güncellemesi**: MainWindow başlığına "Bütçe" butonu eklendi.
- **Veritabanı**: `BudgetLimits` ve `InflationCaches` tabloları EF Core migration ile eklendi.
- **Yeni Kategoriler (Gider)**: Kişisel Harçlık, Çocuk Harçlığı, Taze Gıda & Pazar, Aile Harcamaları, Ulaşım, Ev & Yaşam, Giyim, Eğitim, Sağlık, Hediye & Bağış eklendi.
- **Yeni Kategoriler (Gelir)**: Yemek Ödeneği, Aile Desteği eklendi.
- **Kredi Kartı Takibi**: Banka ve kart bazlı harcama takibi sistemi eklendi.
- **Ekstre Görünümü**: Ana ekranda her banka/kart için o ayki toplam harcama otomatik listelenir.
  - Banka bazlı gruplandırma ve ara toplamlar eklendi.
  - Kart harcamaları otomatik olarak "Toplam Gelir" hanesine dahil edilerek bakiye dengelemesi sağlandı.
- **Kart Harcama Detayı**: Kart toplamına tıklandığında o aya ait tüm kart harcamalarını gösteren detay penceresi açılır.
- **UI**: İşlem Ekle ve Düzenle ekranlarında kategoriler yeşil (Gelir) / kırmızı (Gider) renk kodlaması ile gösteriliyor.
- **Ödeme Yöntemi**: İşlem eklerken "Nakit" veya kayıtlı "Kredi Kartı" seçme imkanı getirildi.
- **Kart Yönetimi**: Mevcut kartların banka adı ve etiketini düzenlemek için "💾 Güncelle" özelliği eklendi.
- **UI Geliştirmeleri**: 
  - Gelir tablosu ve kart özeti arasında **%50 paylaşımlı (50/50 split)** dinamik düzen sağlandı.
  - Kart özeti çok uzadığında ana tablonun yarıda kalmaması için kaydırılabilir (Scrollable) alan eklendi.

## [0.2.0] - 2026-02-21 00:00

### Added
- **UI Localization**: Translated all user-facing text, buttons, and error messages to Turkish.
- **Dashboard Cards**: Added modern visual cards for Total Income (Toplam Gelir), Total Expense (Toplam Gider), and Net Balance (Güncel Bakiye) at the top of the main window.
- **Context Menus**: Added right-click context menus to the DataGrids for quick actions.
- **Transaction Management**: Implemented "Sil" (Delete) feature with a confirmation dialog to remove transactions.
- **Transaction Management**: Added "Düzenle" (Edit) feature with a dedicated `EditTransactionWindow`.
- **Database Seeding**: Translated default database transaction categories to Turkish (Maaş, Kira, vb.) via EF Core Migrations.
- **Security (Login)**: Introduced `LoginWindow` requiring users to define and enter a password to prevent unauthorized access.
- **Security (Two-Tier Encryption)**: Implemented AES-256 military-grade encryption for the `Transaction.Description` field in the database.
- **Security (Recovery)**: Added an automated 16-character Recovery Code generation and display system during first launch.
- **Security (Reset)**: Created `PasswordResetWindow` allowing users to securely reset their password using the Recovery Code without losing their encrypted data.
- **UI Enhancements**: Added color-coding (Green for Income, Red for Expense) and currency formatting to the transaction amounts in the Dashboard DataGrids for better visual scannability.

## [0.1.0] - 2026-02-18 22:58

### Added
- Initial project structure created.
- **FinTrack.Core**: Class Library for business logic.
- **FinTrack.Data**: Class Library for database access (SQL Server Express).
- **FinTrack.WPF**: Windows Presentation Foundation application for UI.
- **Database**: Configured Entity Framework Core with SQL Server.
- **UI**: Added 'Add Transaction' window with category selection.
- **Transactions**: Implemented saving new transactions to SQL Server.
- **Dashboard**: Redesigned to show Income (Alacak) and Expense (Borç) side-by-side.
- **Dashboard**: Added total balance calculation to status bar.
- **System**: Reverted to .NET 10 (Preview) per user request.