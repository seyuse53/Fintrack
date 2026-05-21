# FinTrack → Avalonia UI Geçiş Planı

WPF arayüzünü Avalonia UI'a taşıyarak Linux ve Android desteği kazandırma planı. **12 küçük faza** bölünmüştür.

---

## 📅 Proje Takvimi ve Durum
- **Başlangıç:** 2026-05-10
- **Durum:** 🟡 Devam Ediyor (%95 Tamamlandı)
- **Hedef Platformlar:** Windows (x64), Linux (x64), Android (arm64)

---

## Faz 0: Proje Altyapısı (Temel Kurulum) `[DONE]`
- [x] `dotnet new install Avalonia.Templates` ile şablonları yükle
- [x] `FinTrack.Avalonia` projesini oluştur (`net10.0`)
- [x] Core ve Data projelerini referans ekle
- [x] NuGet paketlerini yükle (`CommunityToolkit.Mvvm`, `SQLCipher`)
- [x] Global exception handling ve SQLite init ayarlarını yap

## Faz 1: MVVM Altyapısı (ViewModels) `[DONE]`
- [x] `ViewModelBase` oluştur (CommunityToolkit.Mvvm)
- [x] `MainWindowViewModel.cs` portu (Root shell)
- [x] `DashboardViewModel.cs` portu (Data loading logic)
- [x] `LoginViewModel.cs` portu (Auth & First launch)

## Faz 2: Giriş Ekranı (Login) `[DONE]`
- [x] `LoginView.axaml` tasarımı (WPF stilinde polish edildi)
- [x] Profil seçimi ve şifre doğrulama akışı
- [x] `AddProfileView.axaml` (Yeni profil / DB import)

## Faz 3: Ana Pencere + Navigasyon `[DONE]`
- [x] `MainAppView.axaml` tasarımı (Sidebar + Content area)
- [x] SPA tarzı sayfa geçiş mekanizması (ViewLocator üzerinden)
- [x] Üst bar (Tarih filtresi, Global Arama)

## Faz 4: Dashboard `[DONE]`
- [x] Özet kartları (Gelir/Gider/Bakiye)
- [x] İşlem listeleri (DataGrid entegrasyonu)
- [x] İşlem verilerinin asenkron yüklenmesi ve asıl/borç ayrımı

## Faz 5: Hesaplarım + Kartlarım `[DONE]`
- [x] `AccountsView.axaml` (Banka hesap kartları ve nakit bakiyesi)
- [x] `CardsView.axaml` (Kredi kartı takibi, limit ve ekstre hesaplama)
- [x] Navigasyon komutlarının menüye bağlanması

## Faz 6: Yatırımlar (Yönetim & İşlem Diyalogları) `[DONE]`
- [x] `InvestmentsView.axaml` portu (Modern CardView)
- [x] Fiyat çekme (API) entegrasyonu (Yahoo / GenelPara)
- [x] Kripto portföy izleme (Filtre duyarlı özet kartlar)
- [x] Alım/Satım işlem pencereleri (`AddInvestmentWindow`, `SellInvestmentWindow`, `EditInvestmentAssetWindow`, `InvestmentDetailWindow`)

## Faz 7: SparklineControl Port (DrawingContext) `[DONE]`
- [x] WPF DrawingVisual kodunu Avalonia DrawingContext/StreamGeometry'ye çevir
- [x] `AvaloniaSparklineControl` oluştur
- [x] Yatırım detaylarına grafik desteği ekle

## Faz 8: Bütçe + Raporlar `[DONE]`
- [x] `BudgetView.axaml` (Limit takibi, ilerleme çubukları)
- [x] `ReportsView.axaml` (Harcama dağılımı grafikleri)
- [x] Vergi hesaplama raporu

## Faz 9: Ayarlar Ekranı (Modüler) `[DONE]`
- [x] Güvenlik, Kategori, API ve Yedekleme sekmelerini oluştur
- [x] Kategori hiyerarşi yönetimi portu

## Faz 10: Platform Servisleri `[DONE]`
- [x] `CrossPlatformAutoLockService` (Avalonia event tabanlı)
- [x] Platforma özel dosya yolu yönetimi (Android/Linux/Windows - Zaten uyumlu)
- [x] GitHub Updater platform adaptasyonu

## Faz 11: Diyaloglar + UX Polish `[ ]`
- [ ] Tüm modern diyalog pencerelerini (Error, Info, Confirm) port et
- [ ] Tema sistemi (Dark/Light toggle)
- [ ] Font ve ikon setini (platform bağımsız) sabitle

## Faz 12: Build, Test & Dağıtım `[ ]`
- [ ] `FinTrack.Tests` (xUnit) projesini kur
- [ ] GitHub Actions CI/CD pipeline yapılandır
- [ ] `.apk` (Android) ve `.AppImage` (Linux) çıktılarını doğrula

---

## 🛠️ Teknik Notlar
- **UI Framework:** Avalonia UI 12.0.2
- **Mimar:** MVVM (CommunityToolkit.Mvvm)
- **Render Motoru:** Skia (Cross-platform performans için)
- **Veritabanı:** SQLCipher (Şifreleme korunacak)
