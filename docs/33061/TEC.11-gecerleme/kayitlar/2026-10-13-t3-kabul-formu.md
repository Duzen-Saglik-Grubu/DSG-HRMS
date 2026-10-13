# T3 Kimlik Yönetimi — Kabul Formu

<!--
  Kabul oturumunda doldurulan formun (Word) depo kaydıdır (TEC.11 YAKLASIM §6).
  Asıl nüsha depo dışındadır: MAN.6 bilgi kayıt defteri BK-24. Kişisel veri (TCKN,
  telefon, e-posta) bu forma yazılmaz.
-->

**Belge kimliği:** TEC.11-KBL-2026-10-13-T3
**Modül:** T3 Kimlik Yönetimi
**Kabul planı:** `docs/33061/TEC.11-gecerleme/T3-kabul-plani.md` (sürüm 0.16)
**Kabul edilen sürüm (etiket):** `v0.2.0-rc.5` (`149f6ee`) · **Ortam:** UAT
**Kabul oturumu tarihi:** 13.10.2026 · **Yer:** Düzen Akademi Toplantı Salonu

---

## 1. Katılımcılar

| Rol | Ad soyad | Birim |
|---|---|---|
| Kabul kararı | Elvan Laleli Şahin | İnsan Kaynakları |
| İK kullanıcısı | Gamze Demir | İnsan Kaynakları |
| Personel 1 | Orhan Kökbudak | Bilgi İşlem |
| Personel 2 | Cemal Alptuğ Erdem | Bilgi İşlem |
| Personel 3 | Fatih Demirtaş | Bilgi İşlem |
| Personel 4 | Süleyman Gedik | Bilgi İşlem |
| Sistem yöneticisi | Doğuş Uçanok | Bilgi İşlem |

## 2. Senaryo sonuçları

| Senaryo | Sonuç | Not / issue |
|---|---|---|
| KS-01 — Üyelik (e-posta ile) | Geçti | — |
| KS-02 — Üyelik (SMS ile), kanal değiştirme ve kodu tekrar gönderme | Geçti | — |
| KS-03 — Kanalların sunulması | Geçti | — |
| KS-04 — Eşleşmeyen bilgiler | Geçti | — |
| KS-05 — Doğrulama kodu kuralları | Geçti | — |
| KS-06 — Giriş ve hatalı giriş | Geçti | — |
| KS-07 — Oturum | Geçti | — |
| KS-08 — Parola işlemleri | Geçti | — |
| KS-09 — İK hesap işlemleri | Geçti | — |
| KS-10 — Sistem yönetimi | Geçti | — |
| KS-11 — Dil, erişilebilirlik ve hata ekranı | Geçti | — |

## 3. Kanıt gösterimi

| Gösterim | İK inceledi | Not |
|---|---|---|
| KG-01 — LOGO'daki ayrılmış personelle üyelik denemesi; TCKN'siz kartların uyarı listesi; ayrılan personelin hesabının senkronizasyonda kapanması ve yeniden işe girişte aynı hesabın açılması | Evet | — |
| KG-02 — Kurumsal e-postası olmayan, kişisel adresli ve ortak adresli kişilere e-posta kanalının sunulmaması | Evet | — |
| KG-03 — Kodların güvenli üreteçle üretilmesi; kod ve parolanın veritabanında özet olarak saklanması | Evet | — |
| KG-04 — Günlükte kişisel veri, kod ve parola bulunmaması; kimlik olaylarının denetim izinde kaydı | Evet | — |
| KG-05 — Üyelik ve kod gönderim hız sınırları | Evet | — |
| KG-06 — İşlemsel iletilerin bildirim istisnasından muaf olması | Evet | — |
| KG-07 — 2FA'nın sistem ve kullanıcı düzeyinde açık ve kapalı hâllerinin otomatik testi | Evet | — |
| KG-08 — Eşleşen ve eşleşmeyen üyelik isteklerinin yanıt süresi farkı (< 20 ms) | Evet | — |
| KG-09 — Kodun tek kullanımlık olması | Evet | — |
| KG-10 — Video veya ses oynarken etkinlik sinyali | Evet | — |

## 4. İK'ya ayrıca gösterilen durumlar

| Durum | Gösterildi | İK'nın görüşü |
|---|---|---|
| AN-01: Cep telefonu kayıtlı olmayan kişide, üç bilginin doğru olup olmadığı sunulan doğrulama kanallarından dolaylı olarak anlaşılabilir (Kabul Planı KS-04 notu) | Evet | Uygun |
| SYG-064 metin incelemesinin kabul edilen istisnaları: Sistem Yöneticisi ekranındaki teknik terimler (B-07, B-08) ve ekranda görünmeyen bir ileti (B-10) | Evet | Uygun |

### 4.1 İK'nın teyit ettiği yorumlar (Kabul Planı §6.1)

| Yorum | İK'nın kararı |
|---|---|
| T-01 — Hareketsizlik neye göre sayılır | Uygun |
| T-02 — Parola değişimi açılırsa kimleri, ne sıklıkla etkiler | Uygun |
| T-03 — Üyelikte deneme ve kod gönderim sınırları (değişiklik talebi #185, İK onaylı 10.10.2026) | Uygun |
| T-04 — İki adımlı doğrulama kimlere uygulanır; İK kurtarma işlemi (değişiklik talebi #190, İK onaylı 10.10.2026) | Uygun |

## 5. Gözlemler (kullanım kolaylığı)

> Herhangi bir sorun çıkmadı. Sistemin parametrik yapısı sayesinde, ihtiyaç duyulan birçok
> ayarın yazılım değişikliğine gerek kalmadan yapılandırılabilmesi kullanım kolaylığı
> açısından olumlu bir özellik olarak değerlendirildi.

## 6. Karar (G3)

**Kabul:** Zorunlu gereksinimlerin tamamı karşılandı. Word formdaki onay kutusu işaretlenemediği için karar kutunun yanına "Kabul edildi" olarak yazıldı.

- Koşullu kabul koşulu: yok
- Ret gerekçesi: yok
- Karşılanmayan "Olmalı" gereksinim: yok

| Rol | Ad soyad | Unvan | Tarih | İmza |
|---|---|---|---|---|
| Kabul eden (İK) | Elvan Laleli Şahin | İnsan Kaynaklarından Sorumlu Yönetim Kurulu Üyesi | 13.10.2026 | İmzalı nüsha: BK-24 |
| Hazırlayan (Bilgi İşlem) | Doğuş Uçanok | Bilgi İşlem Birim Sorumlusu | 13.10.2026 | İmzalı nüsha: BK-24 |

> **İmza:** İmza gereken belgelerin (İK, Üst Yönetim) tamamı daha sonra onaylı kalite
> belgeleriyle birlikte imzalatılacak; **asıl kanıt imzalı nüsha olacak** (Doğuş Uçanok'un
> kararı, 13.10.2026). O zamana kadar kabulün tarihli kanıtı bu kayıt ve oturumda doldurulan
> Word formudur (`Masaüstü\HRMS\T3_Kabul_IK_Inceleme\02_T3_Kabul_Formu.docx`; belge kimliği
> 13.10.2026'da forma işlendi).

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-10-13 | 1.0 | Oturumda doldurulan formdan depoya alındı (#204) | Bilgi İşlem |
