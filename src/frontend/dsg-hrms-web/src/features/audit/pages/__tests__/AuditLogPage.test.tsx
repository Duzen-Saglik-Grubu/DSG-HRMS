import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { format, subDays } from 'date-fns';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { permissions } from '@/shared/auth/permissions';
import { renderWithProviders } from '@/test/render';
import { setSession } from '@/test/session';
import { auditApi, type AuditEntry } from '../../api/auditApi';
import { AuditLogPage } from '../AuditLogPage';

/** Denetim kayitlari ekrani (SYG-PERS-069; ADR-0022 §2). Veriler SENTETIKTIR. */
const ENTRIES: AuditEntry[] = [
  {
    id: 'access-2',
    type: 'export',
    occurredAt: '2026-11-06T09:30:00Z',
    action: 'Export',
    actor: { personId: 'p-1', fullName: 'Ayşe Demir' },
    entityName: 'Person',
    entityId: null,
    subject: null,
    recordCount: 12,
    subjectCount: 12,
    details: '{"search":"***"}',
    traceId: 'trace-2',
    ipAddress: '10.0.0.5',
  },
  {
    id: 'change-1',
    type: 'change',
    occurredAt: '2026-11-06T08:00:00Z',
    action: 'Update',
    actor: null,
    entityName: 'Person',
    entityId: 'p-3',
    subject: { personId: 'p-3', fullName: 'Ali Kaya' },
    recordCount: null,
    subjectCount: 0,
    details: '{"Email":{"old":"***","new":"***"}}',
    traceId: 'trace-1',
    ipAddress: null,
  },
];

const page = (items: AuditEntry[]) => ({
  items,
  page: 1,
  pageSize: 25,
  totalCount: items.length,
  totalPages: 1,
});

describe('AuditLogPage', () => {
  beforeEach(async () => {
    vi.restoreAllMocks();
    await setSession({ status: 'authenticated', permissions: [permissions.auditLogView] });
    vi.spyOn(auditApi, 'list').mockResolvedValue(page(ENTRIES));
  });

  it('son 7 gunun kayitlarini turu, islemi ve kisileriyle listeler', async () => {
    renderWithProviders(<AuditLogPage />);

    const exportRow = (await screen.findByText('Ayşe Demir')).closest('tr')!;
    expect(
      within(exportRow).getByText('Dışa aktarma', { selector: '.MuiChip-label' }),
    ).toBeVisible();
    expect(within(exportRow).getByText('12 kişi')).toBeInTheDocument();

    const changeRow = screen.getByText('Ali Kaya').closest('tr')!;
    // Arka plan islemi "Sistem" olarak gorunur.
    expect(within(changeRow).getByText('Sistem')).toBeInTheDocument();
    expect(within(changeRow).getByText('Güncelleme')).toBeInTheDocument();
    expect(within(changeRow).getByText('Kişi')).toBeInTheDocument();

    const today = new Date();
    expect(auditApi.list).toHaveBeenCalledWith(
      {
        from: format(subDays(today, 6), 'yyyy-MM-dd'),
        to: format(today, 'yyyy-MM-dd'),
        type: null,
        actor: null,
        subject: null,
      },
      1,
      25,
    );
  });

  it('kayit turu suzgeci istege gider', async () => {
    const user = userEvent.setup();
    renderWithProviders(<AuditLogPage />);
    await screen.findByText('Ayşe Demir');

    await user.click(screen.getByRole('combobox', { name: 'Kayıt türü' }));
    await user.click(await screen.findByRole('option', { name: 'Güvenlik olayı' }));

    await waitFor(() =>
      expect(auditApi.list).toHaveBeenLastCalledWith(
        expect.objectContaining({ type: 'security' }),
        1,
        25,
      ),
    );
  });

  it('ayrinti kayitta nasilsa oyle gosterilir; maske acilmaz', async () => {
    const user = userEvent.setup();
    renderWithProviders(<AuditLogPage />);

    const changeRow = (await screen.findByText('Ali Kaya')).closest('tr')!;
    await user.click(within(changeRow).getByRole('button', { name: 'Ayrıntı' }));

    const dialog = await screen.findByRole('dialog', { name: 'Kayıt ayrıntısı' });
    expect(within(dialog).getByText('trace-1')).toBeInTheDocument();
    expect(within(dialog).getByLabelText('Kayıttaki ayrıntı')).toHaveTextContent('"old": "***"');
  });

  it('personel gorme izni varsa kisi suzgecleri sunulur ve secilen kisi istege gider', async () => {
    // Personel gorme izni yoksa kisi suzgeci yoktur (AN-25).
    const { unmount } = renderWithProviders(<AuditLogPage />);
    await screen.findByText('Ayşe Demir');
    expect(screen.queryByRole('combobox', { name: 'İşlemi yapan' })).not.toBeInTheDocument();
    unmount();

    await setSession({
      status: 'authenticated',
      permissions: [permissions.auditLogView, permissions.personView],
    });
    const search = vi
      .spyOn(auditApi, 'searchPeople')
      .mockResolvedValue([{ personId: 'p-9', fullName: 'Zeynep Ak', registryCode: '00009' }]);
    const user = userEvent.setup();
    renderWithProviders(<AuditLogPage />);

    await user.type(screen.getByRole('combobox', { name: 'Verisi görüntülenen kişi' }), 'zey');
    await user.click(await screen.findByRole('option', { name: 'Zeynep Ak · 00009' }));

    expect(search).toHaveBeenCalledWith('zey');
    await waitFor(() =>
      expect(auditApi.list).toHaveBeenLastCalledWith(
        expect.objectContaining({ subject: { personId: 'p-9', fullName: 'Zeynep Ak' } }),
        1,
        25,
      ),
    );
  });

  it('izni yoksa listeyi istemez', async () => {
    await setSession({ status: 'authenticated', permissions: [] });
    renderWithProviders(<AuditLogPage />);

    expect(screen.getByText('Bu işlem için yetkiniz bulunmuyor.')).toBeInTheDocument();
    expect(auditApi.list).not.toHaveBeenCalled();
  });
});
