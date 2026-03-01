# FinTrack Geliştirme Planı (Update Plan)

Bu doküman, FinTrack projesine eklenecek yeni özellikleri, geliştirme fikirlerini ve tamamlanan aşamaları tek bir çatı altında takip etmek için oluşturulmuştur.

## 🟢 Tamamlanan Özellikler (Faz 1 & 2)

- **[x] Askeri Düzey Veritabanı Şifreleme (PBKDF2 & Salt)**
  *Tarih: 2026-03-01*
  FinTrack veritabanı daha önce standart yöntemlerle şifrelenirken, güvenlik seviyesi artırılarak yeni endüstri standardı olan PBKDF2 algoritmasına geçirildi. Kaba kuvvet (brute-force) saldırılarına karşı tam koruma sağlandı.

- **[x] Otomatik Yedekleme (Auto-Backup) Sistemi**
  *Tarih: 2026-03-01*
  Olası veri kayıplarını önlemek adına kullanıcı uygulamayı her kapattığında veritabanının şifreli bir yedeğini istenilen bir klasöre (örn. OneDrive, Google Drive) kopyalayan akıllı bir yedekleme sistemi ve ilk kurulum asistanı eklendi.

- **[x] Otomatik Kilitleme (Auto-Lock / Gelişmiş Güvenlik)**
  *Tarih: 2026-03-01*
  Kullanıcı bilgisayar başından kalktığında veya uygulama arka planda belirlenen süre (varsayılan 3 dakika) boyunca hareketsiz kaldığında ekranı kilitleyen "Oturum Zaman Aşımı" güvenlik modülü entegre edildi.

- **[x] Uygulama İçi Otomatik Güncelleme (Auto-Updater)**
  *Tarih: 2026-03-01*
  GitHub Releases API üzerine kurulu, tamamen maliyetsiz ve çok hızlı bir uygulama içi güncelleme motoru entegre edildi. Uygulama açılışta yeni sürümü denetleyip dilerse kendi kendini güncelleyebilir duruma getirildi.

---

## 🟡 Sonraki Aşamalar İçin Planlanan Özellikler (Faz 3 & Sonrası)

Aşağıdaki özellikler, uygulamanın teknik altyapısı, kullanıcı deneyimi ve platform bağımsızlığı göz önünde bulundurularak listelenmiştir. Sıralama önceliği ihtiyaca göre değiştirilebilir.

### 1. 🌙 Karanlık Mod (Dark Theme) Desteği
Şu an uygulamamız premium ve aydınlık (Light) bir tasarıma sahip. Kullanıcı deneyimini (UX) bir üst seviyeye taşımak için tek tuşla geçiş yapılabilen tam bir Karanlık Mod (Dark Mode) entegre edilebilir. Tüm grafikler, butonlar ve tablolar göz yormayan koyu gri/gece mavisi tonlarına bürünür.

### 2. 📄 Gelişmiş Dışa Aktarma (Excel / PDF Export)
Kullanıcılarınız raporları veya hesap geçmişini başka platformlarda analiz etmek veya doğrudan muhasebecilerle paylaşmak isteyebilir. "Raporlar" veya "İşlemler" sekmelerine işlemleri **Excel (.xlsx)**, **.csv** veya **PDF Rapor Çıktısı** olarak indirebilecekleri bir Dışa Aktarma (Export) aracı tasarlanabilir.

### 3. 🔍 Akıllı Arama ve Filtreleme (Global Search)
İşlem sayınız binlere ulaştığında spesifik bir harcamayı bulmak zorlaşabilir. Ana ekrana veya sol menüye her yerde çalışan bir "Hızlı Arama Çubuğu" eklenebilir. "Market", "1500" veya "Ocak" yazdığınızda anında tüm veritabanını tarayıp ilgili gelir/gider/transfer kayıtlarını listeleyebilir.

### 4. 🔔 Yinelenen İşlemler ve Hatırlatıcılar (Recurring Transactions)
Her ay aynı gün ödenen "Kira", "Netflix", "Aidat" gibi sabit giderleri her seferinde manuel girmek yerine sisteme "Her ayın 15'inde bu harcamayı otomatik ekle" kuralı tanımlama sistemi geliştirilebilir. Ayrıca fatura günü yaklaşınca ana ekranda ufak bir uyarı ("Yaklaşan 3 ödemeniz var") gösterilebilir.

### 5. 🔄 Gerçek Cross-Platform (Çapraz Platform) Arayüzüne Geçiş
Şu anda `FinTrack.Core` ve `FinTrack.Data` katmanlarımız Windows'tan bağımsız (macOS ve Linux uyumlu) çalışabilecek şekilde tasarlandı. Ancak arayüzümüz (FinTrack.WPF) sadece Windows'u destekliyor.
Uygulama arayüzünü WPF ile birebir aynı kod yapısına (XAML) sahip olan **Avalonia UI** çerçevesine taşıyabiliriz. Tasarımı bozmadan uygulamanın macOS ve Linux'ta da yerel (native) olarak çalışmasını sağlayabiliriz.

### 6. 🧪 Otomatik Testlerin (Unit Tests) Kurulması
Gelecekte projeye eklenecek yeni özelliklerin mevcut sağlam yapıyı bozmadığından emin olmak (Regresyonları önlemek) için `FinTrack.Tests` adında bir xUnit test projesi oluşturulabilir. Özellikle yazdığımız `CryptoProvider` ve fatura/bütçe hesaplama mantıkları otomatik testlere bağlanabilir.
