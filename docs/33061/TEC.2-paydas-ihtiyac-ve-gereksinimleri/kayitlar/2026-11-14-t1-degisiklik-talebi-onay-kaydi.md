# T1 Dönemi Değişiklik Talepleri — Onaylayan Rol Kaydı

**Belge kimliği:** TEC.2-KYT-2026-11-14
**Süreç:** TEC.2 — Paydaş İhtiyaç ve Gereksinimlerinin Tanımlanması
**Tarih:** 2026-11-14
**Kaynak:** T1 geliştirme sonu denetimi, bulgu TEC.2-7 (`MAN.8-kalite-guvence/raporlar/2026-11-12-t1-gelistirme-sonu-denetimi.md`); düzeltici faaliyet #387

---

## 1. Amaç

#210'daki denetim, değişiklik talebinde yalnızca **tarihli** bir onay satırı arıyordu; onaylayanın **rolüne** bakmıyordu. RACI §3.1'e göre paydaş gereksinimlerinin (TEC.2) onaylayanı İK'dır. Bu kayıt, T1 dönemindeki dokuz değişiklik talebini #387'de eklenen kuralla (`.github/scripts/change-request-rules.cjs`) geriye dönük gösterir.

## 2. T1 değişiklik talepleri (22.10–08.11.2026)

"Gereken rol": kuralın talebin başlığından, "Etkilenen kayıt" alanından, kapatan PR'ın değiştirdiği dosyalardan ve karar defterinden çıkardığı rol. "—" ise herhangi bir rolün tarihli onayı yeter (yalnızca SYG veya teknik değişiklik; TEC.3'ün onaylayanı Bilgi İşlem).

| Talep | PR | Gereken rol | Gerekçe | Birleştirme anında | Bugün |
|---|---|---|---|---|---|
| #270 | #271 | İK | PG-PERS-12; PG belgesi değişti | uygun | uygun |
| #284 | #287 | İK | PG-PERS-12; PG belgesi değişti | **İK onayı yok** (Bilgi İşlem onayı vardı; İK onayı 6 dk sonra yazıldı) | uygun |
| #321 | #323 | İK | PG-PERS-09; İK kararı `KR-126` (4) | **İK onayı yok** | **İK onayı yok** |
| #333 | #334 | İK | PG-PERS-37; PG belgesi değişti | **İK onayı yok** | **İK onayı yok** |
| #344 | #345 | — | yalnızca SYG-PERS-012 | uygun | uygun |
| #348 | #349 | — | yalnızca SYG-PERS-043 | uygun | uygun |
| #352 | #351 | — | yalnızca SYG-PERS-072 | uygun | uygun |
| #355 | #356 | İK | PG-PERS-44; PG belgesi değişti | **İK onayı yok** | **İK onayı yok** |
| #374 | #376 | — | yalnızca SYG-PERS-055/062 | uygun | uygun |

- **Dört talep, İK onayı kayda geçmeden birleşti.** Hepsi CI'dan geçti: eski kural Bilgi İşlem'in tarihli satırını yeterli saydı.
- **Kuralın ilk sürümü #374'ü yanlışlıkla İK gerektiren talep saydı.** Talep yalnızca SYG değiştiriyordu; "Etkilenen kayıt" alanında PG kimliği, izlenebilirlik matrisi satırını anarken bağlam olarak geçiyordu. Kural, PG kimliğini yalnızca başlıktan veya satırın ilk öğesinden alacak biçimde daraltıldı; bu örnek testte negatif örnek olarak duruyor.
- **T3 talepleri (#77, #185, #190) bu kaydın kapsamı dışındadır.** #210'dan önce açıldılar ve onaylar serbest metinle yazıldı; kural onları ayrıştıramaz. #185 ve #190, T3 kabul formunda `T-03` ve `T-04` olarak İK teyidiyle kayıtlıdır (`TEC.11-gecerleme/kayitlar/2026-10-13-t3-kabul-formu.md`). #77'nin onayı "İK görüşmesinden sonra Bilgi İşlem tarafından onaylandı (26.09.2026)" biçimindedir ve kabul formunda ayrı bir teyit maddesi yoktur; T3 kabul edildiği için yeniden açılmaz, bu kayıtta not edilir.

## 3. Düzeltici faaliyet

| # | İş | Sorumlu | Durum |
|---|---|---|---|
| 1 | #284, #321, #333, #355 için İK'dan geriye dönük teyit alınır; her talebe `Paydaş onayı: GG.AA.YYYY — İK — <kanal> (geriye dönük teyit, #387)` yorumu yazılır | Doğuş Uçanok (İK'ya iletir) | Bekliyor |
| 2 | Dokuz talep T1 kabul planına `T-xx` teyit maddeleri olarak girer (TEC.2 YAKLASIM §4.1 adım 7) | Bilgi İşlem | #398 (kabul planı) |
| 3 | İK teyit etmezse ilgili değişiklik için yeni değişiklik talebi açılır (geri alma veya düzeltme) | Bilgi İşlem | Teyide bağlı |

## 4. Önlem (#387)

- **Rol denetimi:** Onay satırının rol alanı (`tarih — rol — kanal`) ayrıştırılır. Alıntı veya kanal içinde geçen rol adı rol sayılmaz.
- **İK gerektiren talep:** PG kimliği başlıkta veya "Etkilenen kayıt" satırının başında; PR bir PG belgesini değiştiriyor; etkilenen kararın kaynağında İK var; ya da şablonda "Onaylaması gereken rol" İK seçilmiş.
- **Birleştirmeden sonra:** `main`'deki Dal Koruma Denetimi yalnızca birleştirmeden önce yazılmış onayları sayar; eksikse `[DÜZELTİCİ]` issue'su açar. CI kırmızıyken birleştirme engellenemediği için (GitHub Free, R-16) bu ikinci katman gereklidir.
- **Şablon:** "Onaylaması gereken rol" zorunlu alan; onay satırında rol zorunlu.
- **Sınama:** `change-request-approval-test.mjs`, kuralı #284, #321, #333 ve #374'ün biçimleriyle negatif örnek olarak sınar.

## Değişiklik Geçmişi

| Tarih | Sürüm | Değişiklik | Yapan |
|---|---|---|---|
| 2026-11-14 | 1.0 | İlk kayıt (#387) | Bilgi İşlem |
