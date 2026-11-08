import { useState } from 'react';
import {
  Alert,
  Button,
  Chip,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { encryptionApi, type ReEncryptionResult } from '../api/encryptionApi';

/**
 * Ozel nitelikli veri sifrelemesi (SYG-PERS-062; ADR-0020 §3): guncel anahtar, degerlerin
 * anahtara gore sayilari ve yeniden sifreleme isi. Yalnizca `system.encryption.update` olana
 * (Sistem Yoneticisi, KR-138) gosterilir; cagiran yer izni denetler. Deger ve kisi gosterilmez.
 */
export function EncryptionSection() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [confirming, setConfirming] = useState(false);
  const [result, setResult] = useState<ReEncryptionResult | null>(null);

  const status = useQuery({
    queryKey: queryKeys.system.encryption,
    queryFn: encryptionApi.status,
  });

  const run = useMutation({
    mutationFn: encryptionApi.reEncrypt,
    onSuccess: async (outcome) => {
      setResult(outcome);
      await queryClient.invalidateQueries({ queryKey: queryKeys.system.encryption });
    },
  });

  const pending = status.data?.keys.filter((key) => !key.isCurrent && key.count > 0) ?? [];

  return (
    <Paper variant="outlined" component="section" aria-labelledby="encryption-title">
      <Stack spacing={2} sx={{ px: 3, py: 2.5 }}>
        <Typography id="encryption-title" variant="h6" component="h2">
          {t('system.encryption.title')}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {t('system.encryption.description')}
        </Typography>

        {status.error ? (
          <Alert severity="error">{t('error.generic')}</Alert>
        ) : status.data ? (
          <Table size="small" aria-label={t('system.encryption.keys')}>
            <TableHead>
              <TableRow>
                <TableCell>{t('system.encryption.key')}</TableCell>
                <TableCell align="right">{t('system.encryption.count')}</TableCell>
                <TableCell>{t('system.encryption.state')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {status.data.keys.map((key) => (
                <TableRow key={key.keyId}>
                  <TableCell sx={{ fontFamily: 'monospace' }}>{key.keyId}</TableCell>
                  <TableCell align="right">{key.count}</TableCell>
                  <TableCell>
                    {key.isCurrent ? (
                      <Chip size="small" color="primary" label={t('system.encryption.current')} />
                    ) : key.isConfigured ? (
                      <Chip size="small" variant="outlined" label={t('system.encryption.old')} />
                    ) : (
                      <Chip size="small" color="warning" label={t('system.encryption.missing')} />
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ) : null}

        {result ? (
          <Alert severity={result.remainingOnOldKeys === 0 ? 'success' : 'warning'} role="status">
            {t('system.encryption.result', {
              count: result.reEncrypted,
              target: result.targetKeyId,
              remaining: result.remainingOnOldKeys,
              undecryptable: result.undecryptable,
            })}
          </Alert>
        ) : null}
        {run.error ? (
          <Alert severity="error">
            {run.error instanceof ApiError ? run.error.message : t('error.generic')}
          </Alert>
        ) : null}

        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <Button
            variant="outlined"
            disabled={!status.data || run.isPending}
            onClick={() => setConfirming(true)}
          >
            {t('system.encryption.run')}
          </Button>
          {status.data && pending.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              {t('system.encryption.upToDate')}
            </Typography>
          ) : null}
        </Stack>
      </Stack>

      <ConfirmDialog
        open={confirming}
        title={t('system.encryption.confirmTitle')}
        description={t('system.encryption.confirmText', {
          target: status.data?.currentKeyId ?? '',
          count: pending.reduce((sum, key) => sum + key.count, 0),
        })}
        confirmLabel={t('system.encryption.run')}
        onCancel={() => setConfirming(false)}
        onConfirm={() => {
          setConfirming(false);
          run.mutate();
        }}
      />
    </Paper>
  );
}
