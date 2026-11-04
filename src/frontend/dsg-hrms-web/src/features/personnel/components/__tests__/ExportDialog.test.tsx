import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { renderWithProviders } from '@/test/render';
import * as files from '@/shared/utils/saveFile';
import { personsApi, type ExportField } from '../../api/personsApi';
import { ExportDialog } from '../ExportDialog';

/** Personel listesini disa aktarma (SYG-PERS-032; PG-PERS-42). Veriler SENTETIKTIR. */
const FIELDS: ExportField[] = [
  { field: 'registryCode', label: 'Sicil numarası', pdfOnly: false },
  { field: 'nationalId', label: 'TCKN', pdfOnly: false },
  { field: 'company', label: 'Firma', pdfOnly: false },
  { field: 'department', label: 'Birim', pdfOnly: false },
  { field: 'photo', label: 'Fotoğraf', pdfOnly: true },
];

function renderDialog(totalCount = 12, params = 'status=active&companyId=c-1') {
  const onClose = vi.fn();
  renderWithProviders(
    <ExportDialog
      listParams={new URLSearchParams(params)}
      totalCount={totalCount}
      onClose={onClose}
    />,
  );
  return onClose;
}

describe('ExportDialog', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
    vi.spyOn(personsApi, 'exportFields').mockResolvedValue(FIELDS);
    vi.spyOn(files, 'saveFile').mockImplementation(() => undefined);
  });

  it('yalnizca sunucunun sundugu alanlari gosterir; fotograf Excel icin kapali', async () => {
    renderDialog();

    expect(await screen.findByRole('checkbox', { name: 'TCKN' })).toBeInTheDocument();
    expect(screen.queryByRole('checkbox', { name: 'Adres' })).not.toBeInTheDocument();
    expect(screen.getByText('Süzgece uyan 12 kişi aktarılacak.')).toBeInTheDocument();

    const photo = screen.getByRole('checkbox', { name: 'Fotoğraf (yalnızca PDF)' });
    expect(photo).toBeDisabled();

    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: 'PDF' }));
    expect(photo).toBeEnabled();
  });

  it('secilen bicim ve alanlarla liste suzgecleri uzerinden indirir ve secimi hatirlar', async () => {
    const exportFile = vi
      .spyOn(personsApi, 'exportFile')
      .mockResolvedValue({ blob: new Blob(['x']), fileName: 'personel-listesi-20261104-1015.pdf' });
    const user = userEvent.setup();
    const onClose = renderDialog();

    // Varsayilan secim: sicil, firma, birim (sunucunun sundugu varsayilanlar).
    await screen.findByRole('checkbox', { name: 'TCKN' });
    await user.click(screen.getByRole('checkbox', { name: 'TCKN' }));
    await user.click(screen.getByRole('button', { name: 'PDF' }));
    await user.click(screen.getByRole('checkbox', { name: 'Fotoğraf (yalnızca PDF)' }));
    await user.click(screen.getByRole('button', { name: 'İndir' }));

    await vi.waitFor(() => expect(onClose).toHaveBeenCalled());
    const [params, format, fields] = exportFile.mock.calls[0]!;
    expect(params.toString()).toBe('status=active&companyId=c-1');
    expect(format).toBe('pdf');
    expect(fields).toEqual(['registryCode', 'nationalId', 'company', 'department', 'photo']);
    expect(files.saveFile).toHaveBeenCalledWith(
      expect.any(Blob),
      'personel-listesi-20261104-1015.pdf',
    );

    // Sonraki acilista ayni secim gelir.
    expect(JSON.parse(localStorage.getItem('dsg-hrms.person-export')!)).toEqual({
      format: 'pdf',
      fields: ['registryCode', 'nationalId', 'company', 'department', 'photo'],
    });
  });

  it('Excel secilince fotograf gonderilmez', async () => {
    localStorage.setItem(
      'dsg-hrms.person-export',
      JSON.stringify({ format: 'pdf', fields: ['registryCode', 'photo'] }),
    );
    const exportFile = vi
      .spyOn(personsApi, 'exportFile')
      .mockResolvedValue({ blob: new Blob(['x']), fileName: 'a.xlsx' });
    const user = userEvent.setup();
    renderDialog();

    await screen.findByRole('checkbox', { name: 'TCKN' });
    await user.click(screen.getByRole('button', { name: 'Excel' }));
    await user.click(screen.getByRole('button', { name: 'İndir' }));

    await vi.waitFor(() => expect(exportFile).toHaveBeenCalled());
    expect(exportFile.mock.calls[0]![2]).toEqual(['registryCode']);
  });

  it('tumunu sec ve temizle', async () => {
    const user = userEvent.setup();
    renderDialog();

    await screen.findByRole('checkbox', { name: 'TCKN' });
    await user.click(screen.getByRole('button', { name: 'Tümünü seç' }));
    const group = screen.getByRole('group', { name: 'Aktarılacak alanlar' });
    expect(
      within(group)
        .getAllByRole('checkbox')
        .filter((c) => !(c as HTMLInputElement).disabled)
        .every((c) => (c as HTMLInputElement).checked),
    ).toBe(true);

    await user.click(screen.getByRole('button', { name: 'Temizle' }));
    expect(
      within(group)
        .getAllByRole('checkbox')
        .some((c) => (c as HTMLInputElement).checked),
    ).toBe(false);
  });

  it('sinir asildiginda ve kisi yokken indirmez', async () => {
    renderDialog(50_001);
    await screen.findByRole('checkbox', { name: 'TCKN' });

    expect(screen.getByRole('alert')).toHaveTextContent('en fazla 50.000 kişi');
    expect(screen.getByRole('button', { name: 'İndir' })).toBeDisabled();
  });

  it('sunucunun iletisini gosterir', async () => {
    vi.spyOn(personsApi, 'exportFile').mockRejectedValue(
      new ApiError({
        message: 'Bu alanı dışa aktarma yetkiniz yok: Adres.',
        status: 400,
        isNetworkError: false,
      }),
    );
    const user = userEvent.setup();
    renderDialog();

    await screen.findByRole('checkbox', { name: 'TCKN' });
    await user.click(screen.getByRole('button', { name: 'İndir' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Bu alanı dışa aktarma yetkiniz yok: Adres.',
    );
  });
});
