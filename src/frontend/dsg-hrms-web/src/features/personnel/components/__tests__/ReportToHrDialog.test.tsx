import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { renderWithProviders } from '@/test/render';
import { correctionsApi } from '../../api/correctionsApi';
import { ReportToHrDialog } from '../ReportToHrDialog';

/** "IK'ya bildir" formu (SYG-PERS-045). Veriler SENTETIKTIR. */
describe('ReportToHrDialog', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.spyOn(correctionsApi, 'own').mockResolvedValue({
      openFields: ['email'],
      bloodGroups: [
        { value: '1', label: '0 Rh+' },
        { value: '3', label: 'A Rh+' },
      ],
    });
  });

  async function chooseField(user: ReturnType<typeof userEvent.setup>, label: string) {
    await user.click(screen.getByRole('combobox', { name: /Düzeltilecek bilgi/ }));
    await user.click(await screen.findByRole('option', { name: label }));
  }

  it('acik talebi olan alan secilemez ve nedeni yazar (AN-08)', async () => {
    const user = userEvent.setup();
    renderWithProviders(<ReportToHrDialog onClose={vi.fn()} onSent={vi.fn()} />);
    await vi.waitFor(() => expect(correctionsApi.own).toHaveBeenCalled());

    await user.click(screen.getByRole('combobox', { name: /Düzeltilecek bilgi/ }));
    const option = await screen.findByRole('option', { name: /E-posta/ });

    expect(option).toHaveAttribute('aria-disabled', 'true');
    expect(option).toHaveTextContent("İK'da bekleyen talebiniz var");
  });

  it('alan ve deger secilmeden gondermez', async () => {
    const user = userEvent.setup();
    const submit = vi.spyOn(correctionsApi, 'submit');
    renderWithProviders(<ReportToHrDialog onClose={vi.fn()} onSent={vi.fn()} />);

    await user.click(screen.getByRole('button', { name: 'Gönder' }));
    expect(screen.getByText('Düzeltilecek bilgiyi seçin.')).toBeInTheDocument();

    await chooseField(user, 'Adres');
    await user.click(screen.getByRole('button', { name: 'Gönder' }));
    expect(screen.getByText('Doğru bilgiyi yazın.')).toBeInTheDocument();
    expect(submit).not.toHaveBeenCalled();
  });

  it('dogru degeri gonderir ve basariyi bildirir', async () => {
    const user = userEvent.setup();
    const onSent = vi.fn();
    const submit = vi.spyOn(correctionsApi, 'submit').mockResolvedValue('c-1');
    renderWithProviders(<ReportToHrDialog onClose={vi.fn()} onSent={onSent} />);

    await chooseField(user, 'Adres');
    await user.type(
      screen.getByRole('textbox', { name: /Doğru bilgi/ }),
      'Yeni Mah. 3. Sok. No: 4',
    );
    await user.click(screen.getByRole('button', { name: 'Gönder' }));

    await vi.waitFor(() => expect(onSent).toHaveBeenCalled());
    expect(submit).toHaveBeenCalledWith('address', 'Yeni Mah. 3. Sok. No: 4');
  });

  it('medeni hal ve kan grubu listeden secilir', async () => {
    const user = userEvent.setup();
    const submit = vi.spyOn(correctionsApi, 'submit').mockResolvedValue('c-1');
    renderWithProviders(<ReportToHrDialog onClose={vi.fn()} onSent={vi.fn()} />);

    await chooseField(user, 'Kan grubu');
    await user.click(screen.getByRole('combobox', { name: /Doğru bilgi/ }));
    const listbox = await screen.findByRole('listbox');
    await user.click(within(listbox).getByRole('option', { name: 'A Rh+' }));
    await user.click(screen.getByRole('button', { name: 'Gönder' }));

    await vi.waitFor(() => expect(submit).toHaveBeenCalledWith('bloodGroup', '3'));
  });

  it('sunucunun deger hatasini alanin altinda gosterir', async () => {
    const user = userEvent.setup();
    vi.spyOn(correctionsApi, 'submit').mockRejectedValue(
      new ApiError({
        message: 'Gönderilen veri geçerli değil.',
        status: 400,
        isNetworkError: false,
        errors: { value: ['Cep telefonu 5 ile başlayan 10 haneli olmalıdır.'] },
      }),
    );
    renderWithProviders(<ReportToHrDialog onClose={vi.fn()} onSent={vi.fn()} />);

    await chooseField(user, 'Cep telefonu');
    await user.type(screen.getByRole('textbox', { name: /Doğru bilgi/ }), '0212 123 45 67');
    await user.click(screen.getByRole('button', { name: 'Gönder' }));

    expect(
      await screen.findByText('Cep telefonu 5 ile başlayan 10 haneli olmalıdır.'),
    ).toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: /Doğru bilgi/ })).toHaveAttribute(
      'aria-invalid',
      'true',
    );
  });
});
