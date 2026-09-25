# Pull Request

<!--
  BASLIK: Conventional Commits bicimi kullanilir - "feat(uat): ..." gibi.
  Issue bicimi ("[GOREV] ...") PR'da KULLANILMAZ: squash merge, bu basligi
  main uzerindeki commit'in konu satiri yapar. Ayrinti: CONTRIBUTING.md §5.1
-->

## Bağlantılı issue

<!--
  Zorunlu.
  Closes #42  -> gorev/hata issue'u: issue kapanir, resmi baglanti olusur
  Refs #42    -> gereksinim issue'u: issue acik kalir
  Ayrinti: CONTRIBUTING.md §5.2
-->
Closes #

## Ne değişti?

<!-- Kısa ve net. Madde madde yazılabilir. -->

## Neden?

<!-- Değişikliğin gerekçesi. Gereksinim kimliği varsa yazın: REQ-KIMLIK-07 -->

## Nasıl doğrulandı?

<!-- Hangi testler yazıldı/çalıştırıldı, hangi senaryolar denendi. -->

---

## Tamamlanma Tanımı

<!-- Uygulanmayan maddeyi işaretlemeyin ve "Notlar" bölümünde gerekçesini yazın. -->

**Kod ve test**
- [ ] Kodlama standartlarına uygun — tanımlayıcılar İngilizce, yorumlar Türkçe; **betikler ve iş akışları dâhil** (CONTRIBUTING §3.2)
- [ ] Birim testleri yazıldı; kapsam eşikleri sağlanıyor (genel %75, Domain %90)
- [ ] Entegrasyon testleri yazıldı (Testcontainers — gerçek PostgreSQL)
- [ ] **Yetki sızıntısı testi** yazıldı (kapsam dışı kayıt `404` dönüyor)
- [ ] **Maskeleme testi** yazıldı (kişisel veri log'a düz metin düşmüyor)
- [ ] Mimari testi geçiyor (katman ve modül sınırları korunuyor)

**Güvenlik ve KVKK**
- [ ] Yetki kontrolü veri katmanında uygulanıyor (ADR-0007 §3)
- [ ] Denetim izi / erişim kaydı üretiliyor (ADR-0009)
- [ ] Sır, bağlantı dizesi veya anahtar koda yazılmadı (ADR-0008)
- [ ] Hata mesajları iç ayrıntı sızdırmıyor (ADR-0010 §5)

**Veri**
- [ ] Migration yazıldı ve **geri alınabilir**
- [ ] Yıkıcı değişiklik çok adımlı uygulandı (ekle → doldur → geçiş → temizle)
- [ ] Yabancı anahtar ve `CHECK` kısıtları tanımlı

**Sözleşme ve arayüz**
- [ ] OpenAPI güncel; frontend tipleri yeniden üretildi
- [ ] Listeleme uç noktaları sayfalanmış

**Doküman**
- [ ] İlgili dokümanlar güncellendi
- [ ] Yeni mimari karar alındıysa **ADR yazıldı**
- [ ] Karar kayıt defteri güncellendi (gerekiyorsa)
- [ ] **İzlenebilirlik matrisi** güncellendi — gereksinim, tasarım, kod veya test
      değiştiyse zorunlu. Ayrı bir işe bırakılan izlenebilirlik kaydı çürür
      (`docs/33061/izlenebilirlik-matrisi.md` §3)

---

## 33061 izlenebilirliği

| Alan | Değer |
|---|---|
| İlgili süreç(ler) | <!-- TEC.7, TEC.9 … --> |
| Gereksinim kimliği | <!-- REQ-... veya "yok" --> |
| Modül | <!-- kimlik / izin / organizasyon … --> |

## Notlar

<!-- İnceleyenin bilmesi gereken her şey: bilinçli ödünler, atlanan maddelerin
     gerekçesi, sonraki adımlar, dikkat edilmesi gereken noktalar. -->
