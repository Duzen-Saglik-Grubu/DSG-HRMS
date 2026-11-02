import { StrictMode } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient } from '@tanstack/react-query';
import { AppProviders } from '@/app/AppProviders';
import { personPhotosApi } from '@/shared/api/personPhotos';
import { PersonAvatar } from '../PersonAvatar';

afterEach(() => {
  vi.restoreAllMocks();
});

describe('PersonAvatar', () => {
  it('bilesen kalkip yeniden gelince fotograf onbellekten ayni adresle gelir (#315)', async () => {
    const photo = vi
      .spyOn(personPhotosApi, 'photo')
      .mockResolvedValue(new Blob(['jpeg'], { type: 'image/jpeg' }));
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    // StrictMode efektleri bir kez kaldirip yeniden kurar; liste -> kart -> geri dongusu de
    // ayni sekilde bileseni kaldirip onbellekteki fotografi yeniden kullanir.
    const avatar = (
      <StrictMode>
        <AppProviders queryClient={queryClient}>
          <PersonAvatar
            source={{ personId: 'p-1' }}
            hasPhoto
            firstName="AYŞE"
            lastName="DEMİR"
            size={72}
          />
        </AppProviders>
      </StrictMode>
    );

    const first = render(avatar);
    await waitFor(() =>
      expect(screen.getByRole('img', { name: 'AYŞE DEMİR fotoğrafı' })).toHaveAttribute(
        'src',
        'data:image/jpeg;base64,anBlZw==',
      ),
    );
    first.unmount();
    render(avatar);

    expect(screen.getByRole('img', { name: 'AYŞE DEMİR fotoğrafı' })).toHaveAttribute(
      'src',
      'data:image/jpeg;base64,anBlZw==',
    );
    expect(photo).toHaveBeenCalledTimes(1);
  });
});
