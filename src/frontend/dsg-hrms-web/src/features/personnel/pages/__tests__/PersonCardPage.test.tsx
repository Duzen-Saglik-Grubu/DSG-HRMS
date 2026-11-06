import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient } from '@tanstack/react-query';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AppProviders } from '@/app/AppProviders';
import { PersonnelListRoute } from '@/app/PersonnelListRoute';
import { ApiError } from '@/shared/api/problemDetails';
import { permissions } from '@/shared/auth/permissions';
import { routes } from '@/shared/routes';
import { setSession } from '@/test/session';
import { personsApi, type EmploymentCard, type PersonCard } from '../../api/personsApi';
import { personPhotosApi } from '@/shared/api/personPhotos';
import { orderEmployments } from '../../display';
import { PersonCardPage } from '../PersonCardPage';
import { ProfilePage } from '../ProfilePage';

/** Kisi karti ve Profilim (SYG-PERS-017, 034, 037–039, 044). Veriler SENTETIKTIR. */
const ACTIVE: EmploymentCard = {
  registryCode: '00012',
  companyName: 'Düzen Laboratuvarlar',
  hireDate: '2021-03-01',
  terminationDate: null,
  isActive: true,
  seniority: { years: 5, months: 7, days: 30, text: '5 Yıl 7 Ay 30 Gün' },
  workLife: {
    since: '2021-03-01',
    city: 'Ankara',
    branchName: 'Merkez',
    departmentName: 'Bilgi İşlem',
    titleCode: '1',
    titleName: 'Uzman',
    statusCode: 'A',
    occupationCode: '2512.01',
    occupationName: 'Yazılım Geliştirici',
  },
  duty: { id: 'd-1', name: 'Biyolog', validFrom: '2021-03-01', validTo: null },
};

const ENDED: EmploymentCard = {
  ...ACTIVE,
  registryCode: '00004',
  hireDate: '2020-01-01',
  terminationDate: '2020-12-31',
  isActive: false,
  seniority: { years: 1, months: 0, days: 0, text: '1 Yıl 0 Ay 0 Gün' },
  workLife: null,
  duty: null,
};

function person(overrides: Partial<PersonCard> = {}): PersonCard {
  return {
    personId: 'p-1',
    firstName: 'AYŞE',
    lastName: 'DEMİR',
    nationalId: '123******01',
    isNationalIdMasked: true,
    birthDate: '1985-04-12',
    age: 41,
    email: 'ayse.demir@duzen.com.tr',
    mobilePhone: null,
    isSelf: false,
    employments: [ENDED, ACTIVE],
    gender: 'female',
    previousSurname: 'KAYA',
    maritalStatus: 'married',
    educationName: 'Lisans',
    sensitive: { address: 'Kızılay Mah. 1', bloodGroup: 'A Rh+', emergencyContacts: [] },
    hasPhoto: false,
    ...overrides,
  };
}

function renderAt(path: string) {
  const router = createMemoryRouter(
    [
      { path: '/personnel/:personId', element: <PersonCardPage /> },
      { path: routes.profile, element: <ProfilePage /> },
      {
        path: routes.personnel,
        element: (
          <PersonnelListRoute>
            <p>Liste sayfası</p>
          </PersonnelListRoute>
        ),
      },
    ],
    { initialEntries: [path] },
  );
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  return render(
    <AppProviders queryClient={queryClient}>
      <RouterProvider router={router} />
    </AppProviders>,
  );
}

describe('PersonCardPage', () => {
  beforeEach(async () => {
    vi.restoreAllMocks();
    vi.spyOn(personsApi, 'syncFreshness').mockResolvedValue({
      enabled: true,
      lastSucceededAt: '2026-11-04T06:15:00Z',
      lastRunFailed: false,
    });
    await setSession({ status: 'authenticated', permissions: [permissions.personView] });
  });

  it('kart basliginda son senkronizasyon bilgisi yer alir (SYG-PERS-010)', async () => {
    vi.spyOn(personsApi, 'card').mockResolvedValue(person());
    renderAt('/personnel/p-1');

    expect(await screen.findByText(/Son senkronizasyon/)).toBeInTheDocument();
  });

  it('genel ve iletisim bilgilerini gosterir; bos alanda "Kayitli degil" yazar', async () => {
    vi.spyOn(personsApi, 'card').mockResolvedValue(person());
    renderAt('/personnel/p-1');

    expect(
      await screen.findByRole('heading', { level: 1, name: 'AYŞE DEMİR' }),
    ).toBeInTheDocument();
    const general = screen.getByRole('region', { name: 'Genel Bilgiler' });
    expect(within(general).getByText('KAYA')).toBeInTheDocument();
    expect(within(general).getByText('123******01')).toBeInTheDocument();
    expect(within(general).getByText('Kadın')).toBeInTheDocument();
    expect(within(general).getByText('12.04.1985')).toBeInTheDocument();
    expect(within(general).getByText('41 yaş')).toBeInTheDocument();
    expect(within(general).getByText('Evli')).toBeInTheDocument();
    expect(within(general).getByText('A Rh+')).toBeInTheDocument();
    const contact = screen.getByRole('region', { name: 'İletişim Bilgileri' });
    expect(within(contact).getByText('Kızılay Mah. 1')).toBeInTheDocument();
    expect(within(contact).getByText('Kayıtlı değil')).toBeInTheDocument();
    expect(personsApi.card).toHaveBeenCalledWith('p-1');
  });

  it('korumali alanlari goremeyen kullanicida adres ve kan grubu alani hic yoktur', async () => {
    vi.spyOn(personsApi, 'card').mockResolvedValue(person({ sensitive: null }));
    renderAt('/personnel/p-1');

    const contact = await screen.findByRole('region', { name: 'İletişim Bilgileri' });
    expect(within(contact).queryByText('Adres')).not.toBeInTheDocument();
    const general = screen.getByRole('region', { name: 'Genel Bilgiler' });
    expect(within(general).queryByText('Kan grubu')).not.toBeInTheDocument();
  });

  it('kan grubu kayitli degilse "Kayitli degil" yazar (SYG-PERS-034)', async () => {
    vi.spyOn(personsApi, 'card').mockResolvedValue(
      person({ sensitive: { address: 'Kızılay Mah. 1', bloodGroup: null, emergencyContacts: [] } }),
    );
    renderAt('/personnel/p-1');

    const general = await screen.findByRole('region', { name: 'Genel Bilgiler' });
    expect(within(general).getByText('Kan grubu')).toBeInTheDocument();
    expect(within(general).getByText('Kayıtlı değil')).toBeInTheDocument();
  });

  it('her istihdam bir sekmedir: once aktif, sonra ayrilan; kidem yil ay gun', async () => {
    vi.spyOn(personsApi, 'card').mockResolvedValue(person());
    const user = userEvent.setup();
    renderAt('/personnel/p-1');

    const tabs = await screen.findAllByRole('tab');
    expect(tabs.map((tab) => tab.textContent)).toEqual(['00012 · Aktif', '00004 · Ayrıldı']);
    const work = screen.getByRole('region', { name: 'Çalışma Hayatı' });
    expect(within(work).getByText('Biyolog')).toBeInTheDocument();
    expect(within(work).getByText('5 Yıl 7 Ay 30 Gün')).toBeInTheDocument();

    await user.click(tabs[1]!);
    expect(within(work).getByText('31.12.2020')).toBeInTheDocument();
    expect(within(work).getByText('1 Yıl 0 Ay 0 Gün')).toBeInTheDocument();
  });

  it('gorev girme izni olan her sicilde gorev atar; izni olmayan dugme gormez (SYG-PERS-042)', async () => {
    vi.spyOn(personsApi, 'card').mockResolvedValue(person());
    const view = renderAt('/personnel/p-1');

    const work = await screen.findByRole('region', { name: 'Çalışma Hayatı' });
    expect(within(work).getByText('Biyolog')).toBeInTheDocument();
    expect(within(work).queryByRole('button', { name: /görev ata/ })).not.toBeInTheDocument();
    view.unmount();

    await setSession({
      status: 'authenticated',
      permissions: [permissions.personView, permissions.personUpdate],
    });
    const user = userEvent.setup();
    renderAt('/personnel/p-1');

    const editable = await screen.findByRole('region', { name: 'Çalışma Hayatı' });
    expect(
      within(editable).getByRole('button', { name: '00012 siciline görev ata' }),
    ).toBeVisible();
    expect(within(editable).getByRole('button', { name: 'Biyolog görevini sil' })).toBeVisible();

    // Iki sicilli kiside her sicilin gorevi ayridir.
    await user.click(screen.getAllByRole('tab')[1]!);
    expect(
      within(editable).getByRole('button', { name: '00004 siciline görev ata' }),
    ).toBeVisible();
  });

  it('tek istihdamda sekme cubugu gosterilmez', async () => {
    vi.spyOn(personsApi, 'card').mockResolvedValue(person({ employments: [ACTIVE] }));
    renderAt('/personnel/p-1');

    await screen.findByRole('region', { name: 'Çalışma Hayatı' });
    expect(screen.queryByRole('tablist')).not.toBeInTheDocument();
  });

  it('kapsam disindaki kisi "bulunamadi" gorur', async () => {
    vi.spyOn(personsApi, 'card').mockRejectedValue(
      new ApiError({ message: 'Bulunamadı', status: 404, isNetworkError: false }),
    );
    renderAt('/personnel/p-9');

    expect(await screen.findByText('Kişi bulunamadı')).toBeInTheDocument();
  });

  it('istihdam sirasi: aktifler ise giris eskiden yeniye, ayrilanlar cikis yeniden eskiye', () => {
    // KR-130: ilk sekme esas sicildir.
    const older = { ...ACTIVE, registryCode: 'A', hireDate: '2019-01-01' };
    const newer = { ...ACTIVE, registryCode: 'B', hireDate: '2022-01-01' };
    const endedLater = { ...ENDED, registryCode: 'C', terminationDate: '2023-06-30' };

    expect(orderEmployments([ENDED, newer, endedLater, older]).map((e) => e.registryCode)).toEqual([
      'A',
      'B',
      'C',
      '00004',
    ]);
  });

  it('ayni giris tarihli aktiflerde sicil kodu kucuk olan once gelir', () => {
    const second = { ...ACTIVE, registryCode: '00007', hireDate: '2020-01-01' };
    const first = { ...ACTIVE, registryCode: '00003', hireDate: '2020-01-01' };

    expect(orderEmployments([second, first]).map((e) => e.registryCode)).toEqual([
      '00003',
      '00007',
    ]);
  });
});

describe('Profilim', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('kullanici kendi kartini acar (SYG-PERS-044)', async () => {
    await setSession({ status: 'authenticated' });
    vi.spyOn(personsApi, 'me').mockResolvedValue(person({ isSelf: true }));
    renderAt(routes.profile);

    expect(
      await screen.findByRole('heading', { level: 1, name: 'AYŞE DEMİR' }),
    ).toBeInTheDocument();
    expect(personsApi.me).toHaveBeenCalled();
  });

  it('yalnizca kendi kartinda IK-ya bildir dugmesi vardir (SYG-PERS-045)', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.personView] });
    vi.spyOn(personsApi, 'me').mockResolvedValue(person({ isSelf: true }));
    vi.spyOn(personsApi, 'card').mockResolvedValue(person({ isSelf: false }));

    const own = renderAt(routes.profile);
    expect(await screen.findByRole('button', { name: "İK'ya bildir" })).toBeInTheDocument();
    own.unmount();

    renderAt('/personnel/p-1');
    expect(
      await screen.findByRole('heading', { level: 1, name: 'AYŞE DEMİR' }),
    ).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: "İK'ya bildir" })).not.toBeInTheDocument();
  });

  it('giris izni olan kendi gorevini ve acil durum kisilerini girer; izni olmayan yalnizca gorur (KR-137)', async () => {
    await setSession({ status: 'authenticated' });
    vi.spyOn(personsApi, 'me').mockResolvedValue(person({ isSelf: true }));

    const plain = renderAt(routes.profile);
    const emergency = await screen.findByRole('region', { name: 'Acil Durum Kişileri' });
    expect(within(emergency).queryByRole('button')).not.toBeInTheDocument();
    expect(
      within(screen.getByRole('region', { name: 'Çalışma Hayatı' })).queryByRole('button', {
        name: /görev ata/,
      }),
    ).not.toBeInTheDocument();
    plain.unmount();

    await setSession({ status: 'authenticated', permissions: [permissions.personUpdate] });
    renderAt(routes.profile);
    const own = await screen.findByRole('region', { name: 'Acil Durum Kişileri' });
    expect(within(own).getByRole('button', { name: 'Acil durum kişisi ekle' })).toBeVisible();
    expect(
      within(screen.getByRole('region', { name: 'Çalışma Hayatı' })).getByRole('button', {
        name: '00012 siciline görev ata',
      }),
    ).toBeVisible();
  });

  it('kendi fotografi "me" ucundan gelir (SYG-PERS-041)', async () => {
    await setSession({ status: 'authenticated' });
    vi.spyOn(personsApi, 'me').mockResolvedValue(person({ isSelf: true, hasPhoto: true }));
    const myPhoto = vi
      .spyOn(personPhotosApi, 'myPhoto')
      .mockResolvedValue(new Blob(['jpeg'], { type: 'image/jpeg' }));
    const photo = vi.spyOn(personPhotosApi, 'photo');
    renderAt(routes.profile);

    await waitFor(() =>
      expect(screen.getByRole('img', { name: 'AYŞE DEMİR fotoğrafı' })).toHaveAttribute(
        'src',
        'data:image/jpeg;base64,anBlZw==',
      ),
    );
    expect(myPhoto).toHaveBeenCalled();
    expect(photo).not.toHaveBeenCalled();
  });

  it('personeli goremeyen kullanici liste adresini yazsa da profiline gider (#311)', async () => {
    await setSession({ status: 'authenticated' });
    vi.spyOn(personsApi, 'me').mockResolvedValue(person({ isSelf: true }));
    renderAt(routes.personnel);

    expect(
      await screen.findByRole('heading', { level: 1, name: 'AYŞE DEMİR' }),
    ).toBeInTheDocument();
    expect(screen.queryByText('Liste sayfası')).not.toBeInTheDocument();
  });

  it('personeli gorebilen kullanici listeye girer', async () => {
    await setSession({ status: 'authenticated', permissions: [permissions.personView] });
    renderAt(routes.personnel);

    expect(await screen.findByText('Liste sayfası')).toBeInTheDocument();
  });
});
