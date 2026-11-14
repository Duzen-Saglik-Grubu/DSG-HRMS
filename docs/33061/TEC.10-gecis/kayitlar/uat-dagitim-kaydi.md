# UAT Dağıtım Kaydı

**Belge kimliği:** TEC.10-KYT-UAT
**Süreç:** TEC.10 — Geçiş, MAN.5 — Konfigürasyon Yönetimi
**Ortam:** UAT — https://insankaynaklaritest.duzen.com.tr

UAT'ye yapılan her dağıtım bu tabloya bir satır olarak yazılır. Satırı `docker/deploy-uat.sh`
doğrulama geçtikten sonra ekler; satır PR ile commit edilir. Aynı bilgi sunucuda
`/opt/dsg-hrms/deployments.log` dosyasında da tutulur (runbook §3.5).

- **Sürüm:** `git describe --tags --always` çıktısı. Etiketli commit'te etiketin kendisidir (örn. `v0.2.0-rc.1`). Etiketsiz commit'te en yakın etiket, uzaklık ve kısa SHA'dır (örn. `v0.1.0-74-g89a027c`).
- **Zaman:** UTC.
- **A6:** Kurulum sonrası kısa kontrolün sonucu; betik 14.11.2026'dan (#391) itibaren her kurulumda yazar. Önceki satırlarda "—": A6 elle ve yalnızca adaylarda yapılıyordu (`KR-110`). Sürüme eklenen "(dal)", `main`'in atası olmayan bir commit'in bilerek kurulduğunu gösterir (`ALLOW_NON_MAIN=1`).

> **Kayıt öncesi:** Bu kayıt 04.10.2026'da başladı (#125). Bilinen son dağıtım `2d79f01`'dir
> (#141, 04.10.2026). O tarihe kadarki dağıtımlar etiketsizdi ve commit kaydı tutulmadı.

> **`v0.2.0` (13.10.2026):** Kabul edilen sürümün etiketi `149f6ee`'dedir, yani
> `v0.2.0-rc.5` ile aynı commit. UAT'ye yeniden kurulum yapılmadı; 12.10.2026 19:51 UTC
> kurulumu geçerlidir (`KR-097`, `KR-105`). Dağıtım betiği satırı yeniden kurulumda yazar;
> bu not elle eklendi (#218).

> **`e488898` (21.10.2026):** PR #258'in dal commit'idir; `main`'in atası değildir. Kodu (`src`, `docker`)
> birleşen `f312510` ile birebir aynıdır. Dağıtım betiği dağıtılan commit'in `main`'de olduğunu
> denetlemiyordu (T1 denetimi YB-04, #391).
>
> **Notlar tablonun üstündedir:** betik satırı dosyanın sonuna ekler; tablonun altına yazılan not
> tabloyu böler (T1 denetimi A-16, YB-05).

| Zaman (UTC) | Sürüm | Commit | Dağıtan | A6 |
|---|---|---|---|---|
| 2026-10-04T18:47:00Z | v0.2.0-rc.1 | `6cfeef0` | Doğuş Uçanok | — |
| 2026-10-04T20:04:34Z | v0.2.0-rc.2 | `69bafad` | Doğuş Uçanok | — |
| 2026-10-10T13:02:28Z | v0.2.0-rc.3 | `fa3e194` | Doğuş Uçanok | — |
| 2026-10-10T21:56:02Z | v0.2.0-rc.4 | `353eae7` | Doğuş Uçanok | — |
| 2026-10-12T19:51:46Z | v0.2.0-rc.5 | `149f6ee` | Doğuş Uçanok | — |
| 2026-10-21T11:33:58Z | v0.2.0-27-ge488898 | `e488898` | Doğuş Uçanok | — |
| 2026-11-09T21:26:10Z | v0.2.0-89-g6dbe9d5 | `6dbe9d5` | Claude (Doğuş Uçanok onayıyla) | — |
