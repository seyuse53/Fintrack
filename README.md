# FinTrack Kullanım Kılavuzu

FinTrack, kişisel finansınızı yönetmek, hesaplarınızı takip etmek ve yatırım portföyünüzü tek bir ekrandan kontrol etmek için geliştirilmiş yüksek güvenlikli bir finansal takip uygulamasıdır.

Bu dokümanda uygulamanın kullanımına dair önemli özelliklerin açıklamaları yer almaktadır.

---

## 📥 Manuel Veri Aktarımı (JSON Export / Import)

FinTrack, verilerinizi **SQLCipher ve AES** algoritmaları ile şifreleyerek veritabanında güvende tutar. Ancak verilerinizi farklı bir cihaza taşımak, dışarıdan (örneğin Not Defteri ile) okumak veya içindeki olası bir hatayı (yanlış yazılmış isimler vb.) manuel olarak düzeltmek istediğinizde "Manuel Veri Aktarımı" (Export/Import) özelliğini kullanabilirsiniz.

### 1. Verileri Dışa Aktarma (Export)
Mevcut hesaplarınızı, bakiyelerinizi, kredi kartlarınızı ve işlemlerinizi şifresiz, düz bir metin (JSON) dosyasına aktarmak için:
1. Uygulama içerisinden **Ayarlar > Yedekleme** sekmesine girin.
2. Sayfanın en altındaki "Manuel Veri Aktarımı" panelinden **Verileri Dışa Aktar (JSON)** butonuna tıklayın.
3. Çıkan güvenlik uyarısını (**verilerinizin şifresiz kaydedileceğini unutmayın!**) dikkatlice okuyup onaylayın.
4. Bilgisayarınızda (örneğin Masaüstü) güvenli bir konum seçerek dosyanızı kaydedin.

### 2. Verileri İçe Aktarma (Import)
Daha önce dışa aktardığınız (veya elle düzenlediğiniz) bir JSON dosyasını uygulamaya geri yüklemek için:
1. **Ayarlar > Yedekleme** sekmesine gidin.
2. **Verileri İçe Aktar (JSON)** butonuna tıklayın.
3. Çıkan uyarıyı dikkatlice okuyun: **DİKKAT:** İçe aktarım yaptığınızda, uygulamanın içerisindeki mevcut tüm verileriniz (hesaplar, işlemler) tamamen silinecek ve yerine seçtiğiniz dosyanın içerisindeki veriler yüklenecektir!
4. Uyarıyı onayladıktan sonra bilgisayarınızdaki temiz `.json` dosyasını seçin.
5. İşlem bittikten sonra uygulamanın (hesapların ve işlemlerin) tüm ekranlarda güncellenebilmesi için uygulamayı kapatıp yeniden açın.

> **İpucu (Veri Düzeltme):** Uygulama veritabanında düzeltilemeyen bir hata oluşursa, verilerinizi "Dışa Aktar" diyerek masaüstüne JSON olarak çıkartın. Dosyayı sağ tıklayıp "Birlikte Aç > Not Defteri" diyerek açın. Yanlış olan yeri bularak düzeltin ve kaydedin. Daha sonra bu dosyayı "İçe Aktar" diyerek temizlenmiş haliyle tekrar yükleyin.
