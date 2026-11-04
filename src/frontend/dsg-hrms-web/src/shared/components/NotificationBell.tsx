import { useState } from 'react';
import {
  Badge,
  Box,
  Divider,
  IconButton,
  ListItemText,
  Menu,
  MenuItem,
  Tooltip,
  Typography,
} from '@mui/material';
import NotificationsNoneRounded from '@mui/icons-material/NotificationsNoneRounded';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { formatDistanceToNow } from 'date-fns';
import { tr } from 'date-fns/locale';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { inboxApi, type InboxItem } from '@/shared/api/inbox';
import { queryKeys } from '@/shared/api/queryKeys';
import { routes } from '@/shared/routes';

/** Okunmamis sayisinin yenilenme araligi: yeni talep sayfa yenilenmeden gorunsun. */
const REFRESH_MS = 60_000;

/** Bildirimin goturdugu adres; bilinmeyen turde bildirim yalnizca okunur. */
function targetOf(item: InboxItem): string | null {
  return item.kind === 'correctionRequested'
    ? `${routes.corrections}?request=${encodeURIComponent(item.subjectId)}`
    : null;
}

/**
 * Ust cubuktaki bildirim zili (SYG-PERS-046): okunmamis sayisi rozet olarak gorunur; liste
 * acilir, bildirime tiklaninca okundu sayilir ve ilgili kayda gidilir.
 */
export function NotificationBell() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  const unread = useQuery({
    queryKey: queryKeys.notifications.unread,
    queryFn: inboxApi.unreadCount,
    refetchInterval: REFRESH_MS,
  });
  const items = useQuery({
    queryKey: queryKeys.notifications.inbox,
    queryFn: inboxApi.list,
    enabled: anchor !== null,
  });
  const markRead = useMutation({
    mutationFn: (itemId: string) => inboxApi.markRead(itemId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all }),
  });

  const count = unread.data ?? 0;
  const label = count > 0 ? t('notifications.openWithCount', { count }) : t('notifications.open');

  const open = (item: InboxItem) => {
    setAnchor(null);
    if (!item.readAt) {
      markRead.mutate(item.id);
    }

    const target = targetOf(item);
    if (target) {
      void navigate(target);
    }
  };

  return (
    <>
      <Tooltip title={label}>
        <IconButton
          aria-label={label}
          aria-haspopup="menu"
          aria-expanded={anchor ? 'true' : undefined}
          onClick={(event) => setAnchor(event.currentTarget)}
          sx={(theme) => ({
            bgcolor: 'background.paper',
            boxShadow: theme.palette.surface.shadow,
            '&:hover': { bgcolor: 'background.paper' },
          })}
        >
          <Badge badgeContent={count} color="error" max={99}>
            <NotificationsNoneRounded />
          </Badge>
        </IconButton>
      </Tooltip>

      <Menu
        anchorEl={anchor}
        open={anchor !== null}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{ paper: { sx: { width: 340, maxWidth: 'calc(100vw - 32px)' } } }}
      >
        <Box sx={{ px: 2, py: 1 }}>
          <Typography variant="subtitle2" component="p" sx={{ fontWeight: 700 }}>
            {t('notifications.title')}
          </Typography>
        </Box>
        <Divider />
        {items.data && items.data.length === 0 ? (
          <MenuItem disabled>{t('notifications.empty')}</MenuItem>
        ) : null}
        {items.data?.map((item) => (
          <MenuItem key={item.id} onClick={() => open(item)} sx={{ whiteSpace: 'normal' }}>
            <ListItemText
              primary={t(`notifications.kinds.${item.kind}`, {
                defaultValue: t('notifications.kinds.unknown'),
              })}
              secondary={formatDistanceToNow(new Date(item.createdAt), {
                addSuffix: true,
                locale: tr,
              })}
              slotProps={{
                primary: { sx: { fontWeight: item.readAt ? 400 : 700 } },
              }}
            />
            {item.readAt ? null : (
              <Box
                component="span"
                sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: 'error.main', ml: 1 }}
              >
                <Box component="span" sx={visuallyHidden}>
                  {t('notifications.unread')}
                </Box>
              </Box>
            )}
          </MenuItem>
        ))}
      </Menu>
    </>
  );
}

const visuallyHidden = {
  position: 'absolute',
  width: 1,
  height: 1,
  overflow: 'hidden',
  clip: 'rect(0 0 0 0)',
  whiteSpace: 'nowrap',
} as const;
