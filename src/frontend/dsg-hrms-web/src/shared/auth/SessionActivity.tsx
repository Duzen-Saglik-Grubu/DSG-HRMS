import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from '@mui/material';
import { useTranslation } from 'react-i18next';
import { formatCountdown } from '@/shared/utils/formatCountdown';
import { session as appSession } from './session';
import type { SessionManager } from './sessionManager';
import { useSessionActivity } from './useSessionActivity';

interface SessionActivityProps {
  manager?: SessionManager;
}

/**
 * Oturum acikken etkinligi izler ve hareketsizlik suresi dolmadan once uyarir
 * (SYG-KMLK-038).
 *
 * Uyari, kullanicinin yarim kalan isini kaybetmeden oturumu surdurebilmesi icindir. Herhangi
 * bir tiklama veya tus da oturumu surdurur; dugme yalnizca ne yapilacagini acikca soyler.
 */
export function SessionActivity({ manager = appSession }: SessionActivityProps) {
  const { t } = useTranslation();
  const remainingMs = useSessionActivity(manager);
  const open = remainingMs !== null;

  const keepAlive = () => {
    manager.recordInteraction();
    void manager.flushActivity();
  };

  return (
    <Dialog
      open={open}
      onClose={keepAlive}
      aria-labelledby="idle-warning-title"
      aria-describedby="idle-warning-text"
    >
      <DialogTitle id="idle-warning-title">{t('session.idleWarning.title')}</DialogTitle>
      <DialogContent>
        <DialogContentText id="idle-warning-text">
          {t('session.idleWarning.text', {
            time: formatCountdown(Math.ceil((remainingMs ?? 0) / 1000)),
          })}
        </DialogContentText>
      </DialogContent>
      <DialogActions>
        <Button onClick={() => void manager.signOut()}>{t('session.signOut')}</Button>
        <Button variant="contained" onClick={keepAlive} autoFocus>
          {t('session.idleWarning.continue')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
