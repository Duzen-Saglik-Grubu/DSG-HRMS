// main saglik taramasinin kayit secimi (#208, #291).
//
// Tarama bir sorun buldugunda once ACIK bir kayit arar; varsa yeni kayit acmaz, o kayda
// yorum ekler. #290'da tarama yalnizca kendi basligini ("main dogrulanmadi") aradi ve
// "main kirmizi denetimi"nin ayni calisma icin actigi kaydi (#286) gormedi; ayni olay
// iki kayitla izlendi. Kural bu modulde durur ve health-sweep-issues-test.mjs ile sinanir.

/** "main kirmizi denetimi"nin actigi kaydin basligi (main-failure-check.yml). */
export const redTitle = (workflowName) => `[HATA] main kırmızı: ${workflowName}`;

/** Taramanin "dogrulanmadi" kaydinin basligi. */
export const unverifiedTitle = (workflowName) => `[HATA] main doğrulanmadı: ${workflowName}`;

/**
 * Sorun icin yorum eklenecek acik kayit; yoksa null (yeni kayit acilir).
 *
 * "Dogrulanmadi" sorununda ayni is akisinin acik "main kirmizi" kaydi ONCE aranir: is akisi
 * kirmiziyken ayni durumu ikinci bir kayit izlememeli. Ikisi de yoksa kendi basligi aranir.
 *
 * @param {{ title: string, workflowName?: string }} problem
 * @param {{ number: number, title: string }[]} openIssues
 */
export function findOpenIssue(problem, openIssues) {
  if (problem.workflowName) {
    const red = openIssues.find((i) => i.title === redTitle(problem.workflowName));
    if (red) return red;
  }
  return openIssues.find((i) => i.title === problem.title) ?? null;
}
