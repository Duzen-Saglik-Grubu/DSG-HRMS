import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { permissions } from '@/shared/auth/permissions';
import * as files from '@/shared/utils/saveFile';
import { renderWithProviders } from '@/test/render';
import { setSession } from '@/test/session';
import {
  qualityApi,
  type DataQualityItem,
  type DataQualityReport,
  type IneligiblePerson,
} from '../../api/qualityApi';
import { DataQualityPage } from '../DataQualityPage';

/** Veri kalitesi raporu (SYG-PERS-070, 071). Veriler SENTETIKTIR. */
const NO_PERSON: DataQualityItem = {
  issue: 'missingNationalId',
  registryCode: '99001',
  personId: null,
  firstName: null,
  lastName: null,
  company: null,
  branch: null,
  department: null,
};

const TITLE: DataQualityItem = {
  issue: 'missingTitle',
  registryCode: '00012',
  personId: 'p-1',
  firstName: 'AYŞE',
  lastName: 'DEMİR',
  company: 'DÜZEN A.Ş.',
  branch: 'MERKEZ',
  department: 'LABORATUVAR',
};

const INELIGIBLE: IneligiblePerson = {
  personId: 'p-2',
  registryCode: '00020',
  firstName: 'CAN',
  lastName: 'KAYA',
  company: 'DÜZEN A.Ş.',
  branch: 'MERKEZ',
  department: 'BİLGİ İŞLEM',
  reasons: ['domainNotAccepted', 'sharedEmail'],
};

function report(
  items: DataQualityItem[],
  overrides: Partial<DataQualityReport> = {},
): DataQualityReport {
  return {
    lastRun: {
      status: 'completedWithWarnings',
      startedAt: '2026-11-05T06:00:00Z',
      finishedAt: '2026-11-05T06:01:00Z',
      recordsRead: 612,
      warningCount: 3,
    },
    warningsRun: {
      status: 'completedWithWarnings',
      startedAt: '2026-11-05T06:00:00Z',
      finishedAt: '2026-11-05T06:01:00Z',
      recordsRead: 612,
      warningCount: 3,
    },
    counts: {
      missingNationalId: 1,
      invalidNationalId: 0,
      missingBirthDate: 0,
      noContact: 0,
      invalidMobilePhone: 0,
      sharedEmail: 0,
      missingTitle: 13,
      missingDuty: 0,
    },
    ineligibleCount: 1,
    items: { items, page: 1, pageSize: 25, totalCount: items.length, totalPages: 1 },
    ...overrides,
  };
}

describe('DataQualityPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('izni olmayan kullaniciya raporu gostermez', async () => {
    await setSession({ status: 'authenticated', permissions: [] });
    const load = vi.spyOn(qualityApi, 'report');
    renderWithProviders(<DataQualityPage />);

    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(load).not.toHaveBeenCalled();
    expect(screen.queryByRole('button', { name: "Excel'e aktar" })).not.toBeInTheDocument();
  });

  it('son senkronizasyonu, tur sayilarini ve uyarilari gosterir', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.qualityView] });
    vi.spyOn(qualityApi, 'report').mockResolvedValue(report([NO_PERSON, TITLE]));
    renderWithProviders(<DataQualityPage />);

    expect(await screen.findByText('612 kart okundu', { exact: false })).toBeInTheDocument();
    expect(screen.getByText('Uyarılarla tamamlandı')).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Uyarılar (14)' })).toHaveAttribute(
      'aria-selected',
      'true',
    );
    expect(screen.getByRole('tab', { name: 'Üye olamayacak kişiler (1)' })).toBeInTheDocument();

    const filter = screen.getByRole('group', { name: 'Uyarı türü' });
    expect(within(filter).getByRole('button', { name: 'Ünvanı boş · 13' })).toBeInTheDocument();

    const rows = screen.getAllByRole('row');
    expect(within(rows[1]!).getByText('99001')).toBeInTheDocument();
    expect(within(rows[1]!).getByText('Kişi oluşturulmadı')).toBeInTheDocument();
    expect(within(rows[2]!).getByRole('link', { name: 'AYŞE DEMİR' })).toHaveAttribute(
      'href',
      '/personnel/p-1',
    );

    // Gorme izni aktarma hakki vermez.
    expect(screen.queryByRole('button', { name: "Excel'e aktar" })).not.toBeInTheDocument();
  });

  it('tur secilince sunucudan suzer ve ne yapilacagini yazar', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.qualityView] });
    const load = vi.spyOn(qualityApi, 'report').mockResolvedValue(report([TITLE]));
    const user = userEvent.setup();
    renderWithProviders(<DataQualityPage />);

    await user.click(await screen.findByRole('button', { name: 'Ünvanı boş · 13' }));

    expect(load).toHaveBeenLastCalledWith('missingTitle', 1, 25);
    expect(screen.getByRole('button', { name: 'Ünvanı boş · 13' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    expect(screen.getByRole('note')).toHaveTextContent("Ünvanı LOGO'da girin.");
  });

  it('son calisma basarisizsa kart uyarilarinin hangi calismadan geldigini yazar', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.qualityView] });
    vi.spyOn(qualityApi, 'report').mockResolvedValue(
      report([], {
        lastRun: {
          status: 'failed',
          startedAt: '2026-11-05T07:00:00Z',
          finishedAt: '2026-11-05T07:00:30Z',
          recordsRead: 0,
          warningCount: 0,
        },
      }),
    );
    renderWithProviders(<DataQualityPage />);

    expect(await screen.findByText('Başarısız')).toBeInTheDocument();
    expect(screen.getByText(/Kart uyarıları .* tarihli çalışmadan/)).toBeInTheDocument();
  });

  it('uye olamayacak kisileri nedenleriyle listeler', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.qualityView] });
    vi.spyOn(qualityApi, 'report').mockResolvedValue(report([]));
    const ineligible = vi.spyOn(qualityApi, 'ineligible').mockResolvedValue({
      items: [INELIGIBLE],
      page: 1,
      pageSize: 25,
      totalCount: 1,
      totalPages: 1,
    });
    const user = userEvent.setup();
    renderWithProviders(<DataQualityPage />);

    await user.click(await screen.findByRole('tab', { name: 'Üye olamayacak kişiler (1)' }));

    const panel = await screen.findByRole('tabpanel');
    expect(await within(panel).findByRole('link', { name: 'CAN KAYA' })).toBeInTheDocument();
    expect(
      within(panel).getByText('E-postası kabul edilen alan adlarında değil'),
    ).toBeInTheDocument();
    expect(within(panel).getByText('E-postası başka bir kişiyle paylaşılıyor')).toBeInTheDocument();
    expect(ineligible).toHaveBeenCalledWith(1, 25);
  });

  it('aktarma izni olana Excel dugmesi gorunur ve secili turle indirir', async () => {
    await setSession({
      status: 'authenticated',
      permissions: [permissions.qualityView, permissions.qualityExport],
    });
    vi.spyOn(qualityApi, 'report').mockResolvedValue(report([TITLE]));
    const blob = new Blob(['xlsx']);
    const exportFile = vi
      .spyOn(qualityApi, 'exportFile')
      .mockResolvedValue({ blob, fileName: 'veri-kalitesi-20261105-0915.xlsx' });
    const save = vi.spyOn(files, 'saveFile').mockImplementation(() => undefined);
    const user = userEvent.setup();
    renderWithProviders(<DataQualityPage />);

    await user.click(await screen.findByRole('button', { name: 'Ünvanı boş · 13' }));
    await user.click(screen.getByRole('button', { name: "Excel'e aktar" }));

    expect(exportFile).toHaveBeenCalledWith('missingTitle');
    await vi.waitFor(() =>
      expect(save).toHaveBeenCalledWith(blob, 'veri-kalitesi-20261105-0915.xlsx'),
    );
  });
});
