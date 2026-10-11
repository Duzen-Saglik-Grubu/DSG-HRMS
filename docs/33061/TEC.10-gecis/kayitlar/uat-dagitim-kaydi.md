# UAT Dağıtım Kaydı

**Belge kimliği:** TEC.10-KYT-UAT
**Süreç:** TEC.10 — Geçiş, MAN.5 — Konfigürasyon Yönetimi
**Ortam:** UAT — https://insankaynaklaritest.duzen.com.tr

UAT'ye yapılan her dağıtım bu tabloya bir satır olarak yazılır. Satırı `docker/deploy-uat.sh`
doğrulama geçtikten sonra ekler; satır PR ile commit edilir. Aynı bilgi sunucuda
`/opt/dsg-hrms/deployments.log` dosyasında da tutulur (runbook §3.5).

- **Sürüm:** `git describe --tags --always` çıktısı. Etiketli commit'te etiketin kendisidir (örn. `v0.2.0-rc.1`). Etiketsiz commit'te en yakın etiket, uzaklık ve kısa SHA'dır (örn. `v0.1.0-74-g89a027c`).
- **Zaman:** UTC.

> **Kayıt öncesi:** Bu kayıt 04.10.2026'da başladı (#125). Bilinen son dağıtım `2d79f01`'dir
> (#141, 04.10.2026). O tarihe kadarki dağıtımlar etiketsizdi ve commit kaydı tutulmadı.

| Zaman (UTC) | Sürüm | Commit | Dağıtan |
|---|---|---|---|
| 2026-10-04T18:47:00Z | v0.2.0-rc.1 | `6cfeef0` | Doğuş Uçanok |
| 2026-10-04T20:04:34Z | v0.2.0-rc.2 | `69bafad` | Doğuş Uçanok |
| 2026-10-10T13:02:28Z | v0.2.0-rc.3 | `fa3e194` | Doğuş Uçanok |
| 2026-10-10T21:56:02Z | v0.2.0-rc.4 | `353eae7` | Doğuş Uçanok |
