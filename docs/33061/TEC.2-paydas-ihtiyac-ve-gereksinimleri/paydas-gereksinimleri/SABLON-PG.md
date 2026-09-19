# Paydaş Gereksinimleri — &lt;MODÜL ADI&gt;

**Belge kimliği:** TEC.2-PG-&lt;MODÜL&gt;
**Süreç:** TEC.2 — Paydaş İhtiyaç ve Gereksinimlerinin Tanımlanması
**Son güncelleme:** &lt;YYYY-AA-GG&gt;
**Kaynak toplantı:** `kayitlar/<YYYY-AA-GG>-<modül>-gereksinim-toplantisi.md`
**Onay durumu:** &lt;Taslak / İK onayında / **Onaylandı** &lt;tarih&gt;&gt;

> **Kullanım:** `PG-<MODÜL>.md` adıyla kopyalayın; açıklama satırlarını silin.
>
> **Onay durumu "Onaylandı" olmadan geliştirmeye başlanmaz** (`KR-068`).

---

## 1. Kapsam

&lt;Bu modülün ne yaptığı, iki-üç cümle. Kapsam dışı olanlar ayrıca yazılır.&gt;

**Kapsam dışı:** &lt;sınırı netleştirir; "her şeyi kapsar" sanılmasını önler&gt;

---

## 2. Gereksinimler

> **Her gereksinim `ne` ister, `nasıl` yapılacağını söylemez.** Çözüm yazmak, daha iyi
> bir çözümün önünü kapatır.
>
> **Kabul kriteri ölçülebilir olmalıdır.** "Hızlı açılmalı" değil, "liste 2 saniyede
> gelmeli". TEC.11 geçerleme testi bu kriterden yazılır.

### PG-&lt;MODÜL&gt;-01 — &lt;kısa başlık&gt;

| Alan | Değer |
|---|---|
| **Gereksinim** | &lt;Kim, ne yapabilmeli / sistem ne sağlamalı&gt; |
| **Gerekçe** | &lt;Neden gerekli — hangi ihtiyacı karşılıyor&gt; |
| **Kaynak** | &lt;Paydaş kodu ve toplantı tarihi&gt; |
| **Öncelik** | &lt;Zorunlu / Yüksek / Orta / Düşük&gt; |
| **Kabul kriteri** | &lt;Ölçülebilir; "şu yapıldığında şu gözlenir"&gt; |
| **Ölçüt** *(varsa)* | &lt;Kritik performans ölçütü — TEC.2 çıktısı f&gt; |
| **Bağımlılık** | &lt;Başka gereksinim veya modül&gt; |
| **Durum** | &lt;Taslak / Onaylandı / Değişti / İptal&gt; |

> **Numara yeniden kullanılmaz.** İptal edilen gereksinimin kaydı durur, durumu
> `İptal` olur; numarası başka bir gereksinime verilmez. Aksi hâlde eski belgelerdeki
> atıflar sessizce yanlış hedefi gösterir.

### PG-&lt;MODÜL&gt;-02 — &lt;kısa başlık&gt;

&lt;aynı biçim&gt;

---

## 3. Doldurulmuş örnek

> Boş şablon yanlış doldurulur; bu örnek beklenen ayrıntı düzeyini gösterir.
> Yeni belgede bu bölüm **silinir**.

### PG-ORNEK-01 — Çalışan kendi izin bakiyesini görebilmeli

| Alan | Değer |
|---|---|
| **Gereksinim** | Çalışan, kendi yıllık izin bakiyesini ve kullandığı izinleri sisteme girdiğinde görebilmelidir. |
| **Gerekçe** | Bakiye bilgisi bugün İK'ya telefonla soruluyor; İK'ya gelen soruların önemli bir bölümü bu. Çalışanın kendi verisine erişmesi bu yükü kaldırır. |
| **Kaynak** | P3 Çalışan, P1 İK — 18.09.2026 toplantısı, ihtiyaç #4 |
| **Öncelik** | Zorunlu |
| **Kabul kriteri** | Çalışan giriş yaptığında ana ekranda **kalan gün sayısı** görünür; "İzinlerim" sayfasında son 12 ayın izin kayıtları tarih ve gün sayısıyla listelenir. Başka bir çalışanın bakiyesi **hiçbir şekilde** görünmez. |
| **Ölçüt** | Sayfa 2 saniyede açılmalı (500 kayıt) |
| **Bağımlılık** | T3 Kimlik Yönetimi (giriş), İ1 İzin Yönetimi (bakiye hesabı) |
| **Durum** | Taslak |

> Kabul kriterinin son cümlesine dikkat: **olumsuz** bir koşul da ölçülebilir
> yazılmıştır. Yetki sızıntısı testi (ADR-0011 §2) doğrudan bu cümleden üretilir.

---

## 4. İzlenebilirlik

Bu belgedeki her gereksinim `docs/33061/izlenebilirlik-matrisi.md` üzerinde sistem
gereksinimine, tasarıma, koda ve teste bağlanır. **Bağlanmamış gereksinim
tamamlanmamış sayılır.**

---

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| &lt;YYYY-AA-GG&gt; | 0.1 | İlk oluşturma — &lt;tarih&gt; toplantısından | &lt;ad&gt; |
