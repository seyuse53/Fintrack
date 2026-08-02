# 🌐 FinTrack İngilizce Dil Desteği — Kademeli Uygulama Planı

> **Durum:** ✅ Tamamlandı  
> **Son Güncelleme:** 2026-07-30  
> **Aktif Faz:** Yok (Tüm Fazlar Tamamlandı)  
> **Uygulama Sırası:** Faz 0 → 1 → 2 → 3 → 4A → 4B → 4C → 5 → 6 → 7 (sıralı)

Bu doküman, FinTrack'e İngilizce dil desteği (i18n/localization) eklemek için oluşturulan kademeli uygulama planını takip eder.  
Asistan her yeni geliştirme oturumunda bu dosyayı kontrol ederek mevcut fazın durumunu bilecek ve devam edilecek noktayı hatırlatacaktır.

---

## ⚠️ Dokunulmayacak Dosyalar (Güvenlik Garantisi)

Bu dosyalara **hiçbir fazda** dokunulmayacak:
- ❌ `CryptoProvider.cs` — Şifreleme motoru
- ❌ `EncryptionService.cs` — Şifreleme servisi  
- ❌ `AppDbContext.cs` — Veritabanı bağlamı (şifreleme konfigürasyonu)
- ❌ `settings_*.json` dosyaları — Şifreli profil ayarları
- ❌ `LoginViewModel.cs` içindeki `EnsureCreated()` / `Migrate()` fallback mantığı

---

## Faz 0 — Altyapı (Temel)
**Durum:** ✅ Tamamlandı  
**Risk:** 🟢 Minimal — Mevcut koda dokunmuyoruz, sadece yeni dosyalar ekliyoruz.

> Hiçbir ekranı değiştirmeden, sadece lokalizasyon altyapısını kurarız.  
> Mevcut uygulama **aynen çalışmaya devam eder.**  
> ⚡ **Anında dil değişimi** desteklenecek — `INotifyPropertyChanged` tabanlı reaktif binding ile AXAML'deki tüm string'ler uygulama yeniden başlatılmadan güncellenecek.

### Yapılacaklar:
- `[x]` **[NEW]** `FinTrack.Avalonia/Localization/LocalizationService.cs` — Dil yönetimi servisi + `LanguageChanged` event + reaktif binding desteği
- `[x]` **[NEW]** `FinTrack.Avalonia/Localization/TranslateExtension.cs` — AXAML MarkupExtension
- `[x]` **[NEW]** `FinTrack.Avalonia/Resources/Strings.resx` — Türkçe string'ler (varsayılan)
- `[x]` **[NEW]** `FinTrack.Avalonia/Resources/Strings.en.resx` — İngilizce string'ler
- `[x]` **[MODIFY]** `FinTrack.Avalonia/FinTrack.Avalonia.csproj` — resx EmbeddedResource ayarı
- `[x]` **[MODIFY]** `FinTrack.Core/Services/SettingsManager.cs` — `Get/SetLanguagePreference()` (global_settings.json'a yazılır, settings.json'a DOKUNULMAZ)

### Test Kriterleri:
- `[x]` Derleme başarılı mı?
- `[x]` Mevcut tüm ekranlar aynen çalışıyor mu?

---

## Faz 1 — Sidebar + TopBar (~15 string)
**Durum:** ✅ Tamamlandı  
**Risk:** 🟢 Düşük

> İlk görsel değişiklik. Sadece sol menü ve üst bar çevrilir.

### Yapılacaklar:
- `[x]` **[MODIFY]** `Views/MainAppView.axaml` — Sidebar buton metinleri (~10 string)
- `[x]` **[MODIFY]** `ViewModels/MainAppViewModel.cs` — ViewTitle atamaları (~8 string)
- `[x]` **[UPDATE]** `Resources/Strings.resx` + `Strings.en.resx` — ~15 yeni key

### Test Kriterleri:
- `[x]` Sidebar menü metinleri Türkçe görünüyor mu?
- `[x]` `global_settings.json`'da `"Language": "en"` yapınca İngilizce'ye dönüyor mu?
- `[x]` Sayfa geçişleri çalışıyor mu?

---

## Faz 2 — Login Ekranı (~25 string)
**Durum:** ✅ Tamamlandı  
**Risk:** 🟡 Orta-Düşük

> Giriş ekranı bağımsız çalıştığı için güvenli bir test alanı.  
> ⚠️ `LoginViewModel` şifreleme ile etkileşir ama biz **sadece UI string'lerini** değiştireceğiz.

### Yapılacaklar:
- `[x]` **[MODIFY]** `Views/LoginView.axaml` — Giriş ekranı metinleri (~15 string)
- `[x]` **[MODIFY]** `ViewModels/LoginViewModel.cs` — ErrorMessage/InfoMessage atamaları (~10 string)
- `[x]` **[UPDATE]** `Resources/Strings.resx` + `Strings.en.resx` — ~25 yeni key

### Test Kriterleri:
- `[x]` Giriş yapılabiliyor mu?
- `[x]` Yanlış şifre girilince hata mesajı doğru dilde çıkıyor mu?
- `[x]` Kurtarma kodu akışı çalışıyor mu?
- `[x]` Yeni profil kurulumu çalışıyor mu?

---

## Faz 3 — Dashboard (~20 string)
**Durum:** ✅ Tamamlandı  
**Risk:** 🟡 Orta

> Ana ekran kartları ve bölüm başlıkları.  
> ⚠️ Para birimi her zaman ₺ kalacak (kullanıcı kararı).

### Yapılacaklar:
- `[x]` **[MODIFY]** `Views/DashboardView.axaml` — Kart başlıkları (~15 string)
- `[x]` **[MODIFY]** `ViewModels/DashboardViewModel.cs` — Format string'leri (~5 string)
- `[x]` **[MODIFY]** `ViewModels/MainAppViewModel.cs` — Ay isimleri (CultureInfo düzenlemesi)
- `[x]` **[UPDATE]** `Resources/Strings.resx` + `Strings.en.resx` — ~20 yeni key

### Test Kriterleri:
- `[x]` Dashboard kartları doğru dilde mi?
- `[x]` Ay isimleri doğru dilde mi?
- `[x]` ₺ sembolü hâlâ gösteriliyor mu?

---

## Faz 4 — Ana Sayfalar (~250 string)
**Durum:** `[ ]` Başlanmadı  
**Risk:** 🟡 Orta

> En büyük faz. 3 alt-faza bölünmüştür.

### Faz 4A — Hesaplar + Kartlar (~70 string)
- `[x]` `AccountsView.axaml` + `AccountsViewModel.cs` (~20 string)
- `[x]` `CardsView.axaml` + `CardsViewModel.cs` (~30 string)
- `[x]` `ManageAccountsWindow.axaml` + VM (~10 string)
- `[x]` `ManageCardsWindow.axaml` + VM (~10 string)

### Faz 4B — Yatırımlar + BES ✅ Tamamlandı
- `[x]` `InvestmentsView.axaml` + VM
- `[x]` `BesView.axaml` + VM
- `[x]` `ManageInvestmentsWindow.axaml` + VM
- `[x]` `UpdateBesValuesWindow.axaml` + VM

### Faz 4C — Bütçe + Raporlar ✅ Tamamlandı
- `[x]` `BudgetView.axaml` + VM
- `[x]` `ReportsView.axaml` + VM

### Test Kriterleri:
- `[ ]` Her sayfa açılıyor mu?
- `[ ]` Tüm label'lar doğru dilde mi?
- `[ ]` Veri girişi/düzenleme işlemleri çalışıyor mu?

---

## Faz 5 — Diyaloglar ve Window'lar (~120 string)
**Durum:** ### Faz 5: İkincil Pencereler ve İşlemler (✅ Tamamlandı)
Bu aşama çok fazla modal ve pencere içerdiği için 3 alt faza bölünmüştür.

#### Faz 5A: Temel İşlem ve Transfer Ekranları (✅ Tamamlandı)
- `[x]` `AddTransactionWindow.axaml` ve ViewModel'i
- `[x]` `EditTransactionWindow.axaml` ve ViewModel'i
- `[x]` `TransferWindow.axaml` ve ViewModel'i
- `[x]` `PayCreditCardWindow.axaml` ve ViewModel'i

#### Faz 5B: Yatırım ve Uyarı Diyalogları (✅ Tamamlandı)
- `[x]` `AddInvestmentWindow.axaml` ve ViewModel'i
- `[x]` `SellInvestmentWindow.axaml` ve ViewModel'i
- `[x]` `EditInvestmentAssetWindow.axaml` ve ViewModel'i
- `[x]` `ConfirmDialog.axaml` (ve code-behind)
- `[x]` `ErrorDialogWindow.axaml` (ve code-behind)
- `[x]` `InfoDialogWindow.axaml` (ve code-behind)

#### Faz 5C: Kategori, Hesap Detayları ve Diğer Modallar (✅ Tamamlandı)
- `[x]` `ImportPreviewWindow.axaml` + VM (~10 string)
- `[x]` `TaxCalculationWindow.axaml` + VM (~10 string)
- `[x]` `CategoryDetailWindow` + `CategoryEditWindow` (~10 string)
- `[x]` `AccountDetailWindow` + `CardDetailWindow` + `InvestmentDetailWindow` (~15 string)
- `[x]` `UpdateAvailableWindow` (~5 string)

### Test Kriterleri:
- `[ ]` Her dialog açılıyor mu?
- `[ ]` Hata mesajları doğru dilde mi?
- `[ ]` İşlem tamamlama (kaydet/iptal) çalışıyor mu?

---

## Faz 6 — Ayarlar Sayfası + Dil Seçici (~100 string)
**Durum:** ✅ Tamamlandı  
**Risk:** 🟡 Orta-Yüksek

> En son yapılır. 704 satırlık SettingsView.axaml + dil seçici UI eklenmesi.

### Yapılacaklar:
- `[x]` **[MODIFY]** `Views/SettingsView.axaml` — 7 tab'ın tüm metinleri (~80 string)
- `[x]` **[MODIFY]** `ViewModels/SettingsViewModel.cs` — Mesajlar + tema key dönüşümü (~20 string)
- `[x]` **[MODIFY]** `App.axaml.cs` — Tema karşılaştırma string'lerini internal key'lere dönüştürme
- `[x]` **[NEW]** Görünüm Tab'ına Dil Seçici ComboBox eklenmesi

### ⚠️ Kritik Not — Tema String Dönüşümü:
Şu an tema `"Aydınlık"` / `"Karanlık"` stringleriyle karşılaştırılıyor. Bu fazda:
- `global_settings.json`'da saklanan değer: `"light"`, `"dark"`, `"system"` (internal key)
- UI'da gösterilen metin: Çeviriden gelecek ("Aydınlık"/"Light", "Karanlık"/"Dark")

### Test Kriterleri:
- `[x]` Tema değiştirme hâlâ çalışıyor mu? (**KRİTİK**)
- `[x]` Dil seçici düzgün çalışıyor mu?
- `[x]` Tüm ayar tab'ları doğru dilde mi?
- `[x]` Dil değiştirdikten sonra uygulama stabil mi?

---

## Faz 7 — Kalan Eksik Ekranlar ve Final
**Durum:** ✅ Tamamlandı  

### Yapılacaklar:
- `[x]` **[MODIFY]** `Views/AddProfileView.axaml` — Yeni profil oluşturma ekranındaki tüm metinler.
- `[x]` **[MODIFY]** `ViewModels/AddProfileViewModel.cs` — Hata mesajlarının `LocalizationService`'e bağlanması.
- `[x]` **[MODIFY]** `Views/MainWindow.axaml` — Ana pencere başlığı.

---

## 📊 Genel İlerleme Tablosu

| Faz | İçerik | String | Dosya | Risk | Durum |
|---|---|---|---|---|---|
| **0** | Altyapı | ~20 | 6 | 🟢 | `[x]` |
| **1** | Sidebar + TopBar | ~15 | 3 | 🟢 | `[x]` |
| **2** | Login | ~25 | 3 | 🟡 | `[x]` |
| **3** | Dashboard | ~20 | 4 | 🟡 | `[x]` |
| **4A** | Hesaplar + Kartlar | ~70 | 8 | 🟡 | `[x]` |
| **4B** | Yatırımlar + BES | ~80 | 8 | 🟡 | `[x]` |
| **4C** | Bütçe + Raporlar | ~100 | 4 | 🟡 | `[x]` |
| **5** | Diyaloglar | ~120 | ~30 | 🟡 | `[x]` |
| **6** | Ayarlar + Dil Seçici | ~100 | 4 | 🟡 | `[x]` |
| **7** | Kalan Ekranlar + Final | ~15 | 3 | 🟢 | `[x]` |

---

## 📝 Notlar ve Kararlar

- **Para birimi:** Her zaman ₺ kalacak, dil ne olursa olsun (kullanıcı kararı: 2026-07-28)
- **Dil değişimi:** ⚡ **Anında değişim** — uygulama yeniden başlatılmadan, reaktif binding ile tüm UI string'leri anında güncellenecek (kullanıcı kararı: 2026-07-28)
- **Tema string dönüşümü:** `"Aydınlık"` / `"Karanlık"` → `"light"` / `"dark"` geçişi **Faz 6'da** yapılacak. Daha erken fazlarda tema mekanizmasına dokunulmayacak (kullanıcı kararı: 2026-07-28)
- **Uygulama sırası:** Sıralı — Faz 0 → 1 → 2 → 3 → 4A → 4B → 4C → 5 → 6 (kullanıcı kararı: 2026-07-28)
- **Hedef diller:** Şimdilik sadece Türkçe (varsayılan) + İngilizce
- 🛠️ **Mimari Karar (Anlık Çeviri):** Avalonia'daki "Indexer Binding" (örn. `[Key]`) güncelleme bug'ını aşmak için, anında dil değiştirme işlemi `LocalizationService` içindeki `LanguageVersion` int değerinin artırılması ve `TranslateConverter` (IValueConverter) kullanılarak çözülmüştür. Gelecekteki fazlarda eklenecek tüm metinler `TranslateExtension` üzerinden bu yapıyı kullanacaktır.
