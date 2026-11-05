import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient } from '@tanstack/react-query';
import { createMemoryRouter, RouterProvider, useLocation } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AppProviders } from '@/app/AppProviders';
import { inboxApi, type InboxItem } from '@/shared/api/inbox';
import { queryKeys } from '@/shared/api/queryKeys';
import { setSession } from '@/test/session';
import { NotificationBell } from '../NotificationBell';

/** Ust cubuktaki bildirim zili (SYG-PERS-046). */
const ITEM: InboxItem = {
  id: 'n-1',
  kind: 'correctionRequested',
  subjectId: 'c-1',
  createdAt: new Date().toISOString(),
  readAt: null,
};

function Where() {
  const location = useLocation();
  return <p data-testid="where">{`${location.pathname}${location.search}`}</p>;
}

function renderBell(
  queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } }),
) {
  const router = createMemoryRouter(
    [
      {
        path: '*',
        element: (
          <>
            <NotificationBell />
            <Where />
          </>
        ),
      },
    ],
    { initialEntries: ['/'] },
  );
  return render(
    <AppProviders queryClient={queryClient}>
      <RouterProvider router={router} />
    </AppProviders>,
  );
}

describe('NotificationBell', () => {
  beforeEach(async () => {
    vi.restoreAllMocks();
    await setSession({ status: 'authenticated' });
  });

  it('okunmamis sayisini erisilebilir adiyla gosterir', async () => {
    vi.spyOn(inboxApi, 'unreadCount').mockResolvedValue(3);
    renderBell();

    expect(
      await screen.findByRole('button', { name: 'Bildirimler, 3 okunmamış' }),
    ).toBeInTheDocument();
  });

  it('bildirime tiklaninca okundu sayar ve talebe goturur', async () => {
    vi.spyOn(inboxApi, 'unreadCount').mockResolvedValue(1);
    vi.spyOn(inboxApi, 'list').mockResolvedValue([ITEM]);
    const markRead = vi
      .spyOn(inboxApi, 'markRead')
      .mockResolvedValue({ ...ITEM, readAt: ITEM.createdAt });
    const user = userEvent.setup();
    renderBell();

    await user.click(await screen.findByRole('button', { name: 'Bildirimler, 1 okunmamış' }));
    await user.click(await screen.findByRole('menuitem', { name: /Yeni düzeltme talebi/ }));

    expect(markRead).toHaveBeenCalledWith('n-1');
    expect(screen.getByTestId('where')).toHaveTextContent('/personnel/corrections?request=c-1');
  });

  it('talep bildirimine tiklaninca talep listesini sunucudan yeniden ister', async () => {
    // #338: liste onbellekte "taze" sayilsa da yeni talep gorunmeli.
    vi.spyOn(inboxApi, 'unreadCount').mockResolvedValue(1);
    vi.spyOn(inboxApi, 'list').mockResolvedValue([ITEM]);
    vi.spyOn(inboxApi, 'markRead').mockResolvedValue({ ...ITEM, readAt: ITEM.createdAt });
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const listKey = queryKeys.personnel.corrections('new', 1, 25);
    queryClient.setQueryData(listKey, { items: [], totalCount: 0 });
    const user = userEvent.setup();
    renderBell(queryClient);

    expect(queryClient.getQueryState(listKey)?.isInvalidated).toBe(false);
    await user.click(await screen.findByRole('button', { name: 'Bildirimler, 1 okunmamış' }));
    await user.click(await screen.findByRole('menuitem', { name: /Yeni düzeltme talebi/ }));

    expect(queryClient.getQueryState(listKey)?.isInvalidated).toBe(true);
  });

  it('bildirim yoksa bunu soyler', async () => {
    vi.spyOn(inboxApi, 'unreadCount').mockResolvedValue(0);
    vi.spyOn(inboxApi, 'list').mockResolvedValue([]);
    const user = userEvent.setup();
    renderBell();

    await user.click(await screen.findByRole('button', { name: 'Bildirimler' }));

    expect(await screen.findByText('Bildiriminiz yok.')).toBeInTheDocument();
  });
});
