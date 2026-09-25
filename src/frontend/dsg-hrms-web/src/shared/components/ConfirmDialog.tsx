import { useState } from 'react';
import {
  Alert,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from '@mui/material';
import { useTranslation } from 'react-i18next';

interface ConfirmDialogProps {
  open: boolean;
  title: string;

  /**
   * Neyin etkilenecegini ACIKCA yazar.
   *
   * "Emin misiniz?" tek basina yetersizdir: kullanici hangi kaydi sildigini
   * hatirlamayabilir. Ornek: "Ahmet Yilmaz'in 12-16 Eylul tarihli izin talebi
   * silinecek."
   */
  description: string;

  /** Yikici islem mi? Dugme rengi ve ek uyari bunu izler. */
  destructive?: boolean;

  confirmLabel?: string;
  cancelLabel?: string;

  onConfirm: () => void | Promise<void>;
  onCancel: () => void;
}

/**
 * Onay penceresi (ADR-0015 §6).
 *
 * Yikici islemler onay ister ve neyin silinecegi yazilir. Islem suresince dugme
 * devre disi kalir; cift gonderim engellenir (ADR-0015 §4).
 */
export function ConfirmDialog({
  open,
  title,
  description,
  destructive = false,
  confirmLabel,
  cancelLabel,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const { t } = useTranslation();
  const [islemde, setIslemde] = useState(false);

  const handleConfirm = async () => {
    setIslemde(true);

    try {
      await onConfirm();
    } finally {
      setIslemde(false);
    }
  };

  return (
    <Dialog
      open={open}
      onClose={islemde ? undefined : onCancel}
      aria-labelledby="onay-basligi"
      aria-describedby="onay-aciklamasi"
      maxWidth="xs"
      fullWidth
    >
      <DialogTitle id="onay-basligi">{title}</DialogTitle>

      <DialogContent>
        <DialogContentText id="onay-aciklamasi">{description}</DialogContentText>

        {destructive ? (
          // Renk TEK BASINA anlam tasimaz (ADR-0015 §7): kirmizi dugmenin yaninda
          // metinle de uyarilir.
          <Alert severity="warning" sx={{ mt: 2 }}>
            {t('confirm.irreversible')}
          </Alert>
        ) : null}
      </DialogContent>

      <DialogActions>
        <Button onClick={onCancel} disabled={islemde}>
          {cancelLabel ?? t('confirm.cancel')}
        </Button>

        <Button
          onClick={() => void handleConfirm()}
          color={destructive ? 'error' : 'primary'}
          variant="contained"
          disabled={islemde}
          startIcon={islemde ? <CircularProgress size={16} color="inherit" /> : undefined}
        >
          {confirmLabel ?? t('confirm.confirm')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
