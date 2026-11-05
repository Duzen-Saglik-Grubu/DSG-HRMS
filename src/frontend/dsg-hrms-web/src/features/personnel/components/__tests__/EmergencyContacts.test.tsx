import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { renderWithProviders } from '@/test/render';
import { emergencyContactsApi, type EmergencyContact } from '../../api/emergencyContactsApi';
import { displayMobilePhone } from '../../display';
import { EmergencyContacts } from '../EmergencyContacts';

/** Kartin "Acil Durum Kisileri" bolumu (PG-PERS-44; SYG-PERS-043). Veriler SENTETIKTIR. */
const SPOUSE: EmergencyContact = {
  id: 'c-1',
  relationship: 'Eşi',
  firstName: 'Ayşe',
  lastName: 'Deniz',
  mobilePhone: '5321112233',
};

describe('EmergencyContacts', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('kayitlari yakinlik ve okunur telefonla listeler', () => {
    renderWithProviders(<EmergencyContacts personId="p-1" contacts={[SPOUSE]} editable={false} />);

    expect(screen.getByText('Ayşe Deniz')).toBeInTheDocument();
    expect(screen.getByText('Eşi · 532 111 22 33')).toBeInTheDocument();
  });

  it('kayit yoksa "Kayitli degil" yazar', () => {
    renderWithProviders(<EmergencyContacts personId="p-1" contacts={[]} editable={false} />);

    expect(screen.getByText('Kayıtlı değil')).toBeInTheDocument();
  });

  it('duzenleme hakki yoksa ekleme, duzenleme ve silme dugmesi yoktur', () => {
    // Kisinin kendisi gorur ama degistiremez (PG-PERS-44).
    renderWithProviders(<EmergencyContacts personId="p-1" contacts={[SPOUSE]} editable={false} />);

    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });

  it('bos alanlari gondermeden isaretler', async () => {
    const add = vi.spyOn(emergencyContactsApi, 'add');
    const user = userEvent.setup();
    renderWithProviders(<EmergencyContacts personId="p-1" contacts={[]} editable />);

    await user.click(screen.getByRole('button', { name: 'Acil durum kişisi ekle' }));
    const dialog = await screen.findByRole('dialog');
    await user.type(within(dialog).getByRole('textbox', { name: /^Adı/ }), 'Fatma');
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(add).not.toHaveBeenCalled();
    expect(within(dialog).getAllByText('Bu alanı doldurun.')).toHaveLength(3);
  });

  it('yeni kaydi gonderir; sunucunun telefon hatasini alanin altinda gosterir', async () => {
    const add = vi.spyOn(emergencyContactsApi, 'add').mockRejectedValue(
      new ApiError({
        message: 'Gönderilen veri geçerli değil.',
        status: 400,
        isNetworkError: false,
        errors: { mobilePhone: ['Cep telefonu 5 ile başlayan 10 haneli olmalıdır.'] },
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<EmergencyContacts personId="p-1" contacts={[]} editable />);

    await user.click(screen.getByRole('button', { name: 'Acil durum kişisi ekle' }));
    const dialog = await screen.findByRole('dialog');
    await user.type(within(dialog).getByRole('textbox', { name: /Yakınlığı/ }), 'Annesi');
    await user.type(within(dialog).getByRole('textbox', { name: /^Adı/ }), 'Fatma');
    await user.type(within(dialog).getByRole('textbox', { name: /Soyadı/ }), 'Deniz');
    await user.type(within(dialog).getByRole('textbox', { name: /Cep telefonu/ }), '212 111 22 33');
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(add).toHaveBeenCalledWith('p-1', {
      relationship: 'Annesi',
      firstName: 'Fatma',
      lastName: 'Deniz',
      mobilePhone: '212 111 22 33',
    });
    expect(
      await within(dialog).findByText('Cep telefonu 5 ile başlayan 10 haneli olmalıdır.'),
    ).toBeInTheDocument();
  });

  it('duzenleme mevcut degerlerle acilir ve degisikligi gonderir', async () => {
    const change = vi.spyOn(emergencyContactsApi, 'change').mockResolvedValue(SPOUSE);
    const user = userEvent.setup();
    renderWithProviders(<EmergencyContacts personId="p-1" contacts={[SPOUSE]} editable />);

    await user.click(screen.getByRole('button', { name: 'Ayşe Deniz kaydını düzenle' }));
    const dialog = await screen.findByRole('dialog');
    const phone = within(dialog).getByRole('textbox', { name: /Cep telefonu/ });
    expect(phone).toHaveValue('532 111 22 33');

    await user.clear(phone);
    await user.type(phone, '533 999 88 77');
    await user.click(within(dialog).getByRole('button', { name: 'Kaydet' }));

    expect(change).toHaveBeenCalledWith('p-1', 'c-1', {
      relationship: 'Eşi',
      firstName: 'Ayşe',
      lastName: 'Deniz',
      mobilePhone: '533 999 88 77',
    });
  });

  it('silme once kimin silinecegini sorar', async () => {
    const remove = vi.spyOn(emergencyContactsApi, 'remove').mockResolvedValue();
    const user = userEvent.setup();
    renderWithProviders(<EmergencyContacts personId="p-1" contacts={[SPOUSE]} editable />);

    await user.click(screen.getByRole('button', { name: 'Ayşe Deniz kaydını sil' }));
    const dialog = await screen.findByRole('dialog');
    expect(dialog).toHaveTextContent('Ayşe Deniz bu kişinin acil durum kişilerinden çıkarılacak.');

    await user.click(within(dialog).getByRole('button', { name: 'Sil' }));

    expect(remove).toHaveBeenCalledWith('p-1', 'c-1');
  });

  it('telefonu okunur bicimde yazar', () => {
    expect(displayMobilePhone('5321112233')).toBe('532 111 22 33');
    expect(displayMobilePhone('12345')).toBe('12345');
  });
});
