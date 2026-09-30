import { fireEvent, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test/render';
import { registrationApi } from '../../api/registrationApi';
import { AuthLayout } from '../AuthLayout';

/** Kurumsal logo (SYG-KMLK-069, PRM-GRN-01). */
function settings(logoVersion: string | null) {
  return {
    supportContact: 'Bilgi İşlem',
    passwordRules: { minLength: 6, maxLength: 128, requireComplexity: false },
    verificationCodeLength: 6,
    logoVersion,
  };
}

describe('AuthLayout', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('logo yuklenmemisse varsayilan logoyu gosterir', async () => {
    vi.spyOn(registrationApi, 'publicSettings').mockResolvedValue(settings(null));
    renderWithProviders(<AuthLayout>içerik</AuthLayout>);

    await screen.findByText(/Bilgi İşlem ile/);
    expect(screen.getByAltText('Düzen Sağlık Grubu logosu')).toHaveAttribute(
      'src',
      '/brand/duzen_logo.png',
    );
  });

  it('yuklu logoyu surumuyle ister; yuklenemezse varsayilana doner', async () => {
    vi.spyOn(registrationApi, 'publicSettings').mockResolvedValue(settings('abc123'));
    renderWithProviders(<AuthLayout>içerik</AuthLayout>);

    const logo = screen.getByAltText('Düzen Sağlık Grubu logosu');
    await waitFor(() => expect(logo).toHaveAttribute('src', '/api/v1/system/logo?v=abc123'));

    fireEvent.error(logo);

    expect(logo).toHaveAttribute('src', '/brand/duzen_logo.png');
  });
});
