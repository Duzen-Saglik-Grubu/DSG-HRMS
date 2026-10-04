/**
 * Mailpit'in yakaladigi e-postalari okur (#146). Uctan uca yiginda e-posta disari cikmaz;
 * dogrulama kodu ve davet baglantisi buradan alinir.
 */
const MAILPIT = process.env.E2E_MAILPIT_URL ?? 'http://localhost:8025';

interface MessageSummary {
  ID: string;
  Created: string;
}

/** Aliciya verilen andan SONRA gelen ilk iletinin metni; gelene kadar bekler. */
export async function nextMessageText(
  to: string,
  since: Date,
  timeoutMs = 20_000,
): Promise<string> {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const search = await fetch(
      `${MAILPIT}/api/v1/search?query=${encodeURIComponent(`to:"${to}"`)}`,
    );
    const { messages } = (await search.json()) as { messages: MessageSummary[] };
    const fresh = messages.find((m) => new Date(m.Created) >= since);
    if (fresh) {
      const message = await fetch(`${MAILPIT}/api/v1/message/${fresh.ID}`);
      const { Text } = (await message.json()) as { Text: string };
      return Text;
    }
    await new Promise((resolve) => setTimeout(resolve, 500));
  }
  throw new Error(`${to} adresine ${since.toISOString()} sonrasinda ileti gelmedi.`);
}

/** Iletideki dogrulama kodu ("... kodunuz: 123456"). */
export function codeIn(text: string): string {
  const match = /kodunuz: (\d+)/.exec(text);
  if (!match?.[1]) {
    throw new Error('Iletide dogrulama kodu bulunamadi.');
  }
  return match[1];
}

/** Iletideki davet baglantisi. */
export function inviteLinkIn(text: string): string {
  const match = /(https?:\/\/\S+\/invite#token=[A-Za-z0-9_-]+)/.exec(text);
  if (!match?.[1]) {
    throw new Error('Iletide davet baglantisi bulunamadi.');
  }
  return match[1];
}
