/**
 * Uygulamanin sabit yollari.
 *
 * Yollar tek yerde tanimlanir: ozellikler birbirine (ve uygulama kabuguna) dogrudan
 * erisemedigi icin (ADR-0015 §1) ortak ihtiyac `shared/` altindadir. Yollar Ingilizcedir ve
 * API ile uyumludur (`KR-058`).
 */
export const routes = {
  home: '/',
  login: '/login',
  register: '/register',
  forgotPassword: '/forgot-password',
  changePassword: '/account/password',
} as const;

/** Giristen sonra donulecek adresin tasindigi sorgu parametresi. */
export const RETURN_TO_PARAM = 'returnTo';

/**
 * Giristen sonra donulecek adres guvenli mi; degilse ana sayfa doner.
 *
 * Yalnizca uygulama ici bir yol kabul edilir. `//ornek.com` veya `https://…` gibi bir deger
 * kabul edilseydi giris ekrani, kullaniciyi basarili giristen sonra baska bir siteye
 * goturen bir araca donusurdu (acik yonlendirme). Giris ekraninin kendisi de kabul
 * edilmez: oturumu acik kullaniciyi giris ekranina gondermek sonsuz yonlendirme olurdu.
 */
export function safeReturnPath(value: string | null | undefined): string {
  if (
    !value ||
    !value.startsWith('/') ||
    value.startsWith('//') ||
    value.startsWith('/\\') ||
    value === routes.login ||
    value.startsWith(`${routes.login}?`) ||
    value.startsWith(`${routes.login}/`)
  ) {
    return routes.home;
  }

  return value;
}
