# SYG-KMLK-064 Metin İncelemesi

**Belge kimliği:** TEC.9-INC-2026-10-04-SYG064
**Süreç:** TEC.9 — Doğrulama (yöntem: İnceleme)
**Gereksinim:** SYG-KMLK-064 (REQ-KMLK-043): "Tüm ekran metinleri ve hata iletileri Türkçedir, teknik terim içermez ve kullanıcıya ne yapması gerektiğini söyler."
**Kabul kriteri (REQ-KMLK-043):** "Hata mesajı ne yapılacağını söylüyor ('Bir hata oluştu' demiyor)."
**İncelenen sürüm:** `main` @ `a4ffe01`
**Tarih:** 2026-10-04 · **İnceleyen:** Bilgi İşlem · **Onaylayacak:** Doğuş Uçanok; nihai teyit İK kabulünde (TEC.11)

---

## 1. Kapsam ve yöntem

| Kaynak | Adet | Nasıl çıkarıldı |
|---|---|---|
| Ekran metinleri (`src/locales/tr/common.json`) | 260 | Dosyanın tamamı |
| Sunucunun kullanıcıya döndürdüğü iletiler | 100 satır | Türkçe karakter içeren dizgeler (Application, Api, Domain, Infrastructure) |

Sunucu taramasındaki 100 satırın bir kısmı kullanıcıya hiç ulaşmaz: kod yorumları, izin adları, iç
denetim iletileri. Bunlar ölçüte tabi değildir ve aşağıda ayrıca sayılmıştır.

Her metin üç ölçüte göre okundu:
- **(T) Türkçe:** Metin Türkçe mi?
- **(K) Teknik terim:** Kullanıcının bilmesi beklenmeyen terim var mı?
- **(Y) Yönlendirme:** Hata iletisi kullanıcıya ne yapacağını söylüyor mu?

**Hedef kitle ayrımı:** Sistem parametreleri ekranı yalnızca Sistem Yöneticisi rolüne açıktır (`system.parameter.view`) ve kullanıcısı Bilgi İşlem'dir. Bu ekrandaki teknik terimler ayrı değerlendirildi (B-07, B-08).

---

## 2. Sonuç

| | Sonuç |
|---|---|
| Ekran metinleri (260) | 2 bulgu: B-01 (kullanılmayan metin) ve B-08 (yönetici ekranı etiketleri); diğerleri ölçütleri karşılıyor |
| Sunucu dizgeleri (100 satır) | 8 bulgu (B-02…B-07, B-09, B-10). Satırların bir kısmı kullanıcıya hiç ulaşmaz (kod yorumları, izin adları, iç denetim iletileri) ve ölçüt dışı bırakıldı |
| **Bulgu toplamı** | **10:** 6 düzeltme önerisi (B-02…B-06, B-09), 1 kaldırma önerisi (B-01), 3 kabul edilebilir istisna (B-07, B-08, B-10) |

**İngilizce metin yok.** Bütün kullanıcı metinleri Türkçe.

Bulguların hiçbiri personelin her gün gördüğü bir akışı bozmuyor. Altısı yalnızca hata
durumlarında görünüyor.

---

## 3. Bulgular

| # | Metin (bugün) | Nerede görünür | Ölçüt | Öneri |
|---|---|---|---|---|
| B-01 | `Bir sorun oluştu` (ekran, `status.error`) | **Hiçbir yerde** kullanılmıyor | Y (kabul kriterinin yasakladığı ifade) | Kaldırılsın; ileride yanlışlıkla kullanılmasın |
| B-02 | `Parola çok kısa.` (sunucu) | Üyelik, sıfırlama, davet ve parola değiştirme; ekran kuralı ile sunucu parametresi farklılaşınca | Y (kaç karakter gerektiği yok) | `Parola en az {n} karakter olmalıdır.` |
| B-03 | `Bağlantı geçersiz.` (sunucu) | Davet bağlantısı bozuk veya kırpılmış açıldığında | Y | `Bu bağlantı geçersiz. Yeni bir bağlantı için İnsan Kaynakları birimine başvurun veya parolanızı kendiniz sıfırlayın.` (ekrandaki iletiyle aynı) |
| B-04 | `Doğrulama kodu geçerli değil.` (sunucu) | Koda harf veya fazla karakter yazıldığında | Y | `Doğrulama kodu yalnızca rakamlardan oluşur. E-posta veya SMS ile gelen kodu girin.` |
| B-05 | `İleti kuyruğu dolu. Lütfen biraz sonra tekrar deneyin.` (sunucu) | Davet gönderiminde, ileti yoğunluğunda | K ("ileti kuyruğu") | `Şu anda çok fazla istek var. Lütfen birkaç dakika sonra tekrar deneyin.` |
| B-06 | `Bu işlem yalnızca güvenli bağlantı (HTTPS) üzerinden yapılabilir.` (sunucu) | Normalde **görünmez**; yalnızca sunucu yanlış yapılandırılırsa | K ("HTTPS") | `Bu işlem yalnızca güvenli bağlantı üzerinden yapılabilir. Sisteme kurumun bildirdiği adresten girin.` |
| B-07 | `Gizli değer kaydedilemiyor: şifreleme anahtarı (ParameterProtection:Key) tanımlı değil. Bilgi İşlem birimine başvurun.` | Yalnızca parametre ekranı (Sistem Yöneticisi) | K | **Kabul edilebilir istisna:** okuyan Bilgi İşlem'dir ve anahtar adı sorunu çözmek için gereklidir |
| B-08 | Parametre etiketleri: "Erişim jetonu ömrü", "SMTP sunucusu ve portu", "NetGSM API parolası", "İki adımlı doğrulama (2FA)" | Yalnızca parametre ekranı (Sistem Yöneticisi) | K | **Kabul edilebilir istisna:** ekranın kullanıcısı Bilgi İşlem'dir |
| B-09 | `Değer beklenen biçimde değil.` (sunucu) | Yalnızca parametre ekranı | Y | Biçimi söylesin: SMTP için `Değer sunucu:port biçiminde olmalıdır (örneğin mail.duzen.com.tr:587).`, alan adları için `Değer bir alan adı olmalıdır (örneğin duzen.com.tr).` |
| B-10 | `Etkinlik bildirimi çok sık gönderildi.` (sunucu) | **Görünmez**; tarayıcının arka plan isteğine döner, ekranda gösterilmez | — | **Kabul edilebilir:** kullanıcıya ulaşmaz |

### 3.1 Özellikle iyi bulunanlar

- **Oturum sonu iletileri:** Yedi kapanma nedeninin her biri ayrı bir iletiyle bildiriliyor ve ne yapılacağını söylüyor (örn. "Hesabınıza başka bir cihazdan giriş yapıldı. Devam etmek için yeniden giriş yapın.").
- **Kod iletileri:** Kodun süresi dolduğunda, deneme hakkı bittiğinde ve kod kullanılamaz olduğunda hepsi "Yeni kod isteyin." diye bitiyor.
- **Genel hata ekranı:** "Beklenmeyen bir sorun oluştu" diyor ama hemen ardından "Lütfen tekrar deneyin." diye yönlendiriyor ve bir takip numarası gösteriyor (REQ-KMLK-046).
- **Hesabı olmayan veya pasif kişiye yönlendirme:** İleti kişiyi doğru birime yönlendiriyor ("Üye olarak…", "İnsan Kaynakları birimiyle iletişime geçin.").

---

## 4. Karar beklenen

1. **B-01 ile B-06 ve B-09:** Önerilen metinlerle düzeltilsin mi? Onaylanırsa ayrı bir PR'da uygulanır ve testleri güncellenir.
2. **B-07, B-08 ve B-10:** Kabul edilebilir istisna olarak kayda geçsin mi?

Karar sonrası durum T3 doğrulama raporuna (SYG-KMLK-064) işlenir. Nihai teyit İK kabulünde, kabul senaryosu KS-11 ile verilir.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-04 | 1.0 | İlk inceleme (#124) | Bilgi İşlem |
