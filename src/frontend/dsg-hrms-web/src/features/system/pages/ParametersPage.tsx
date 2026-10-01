import { useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Divider,
  FormControlLabel,
  Paper,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { permissions } from '@/shared/auth/permissions';
import { usePermission } from '@/shared/auth/usePermission';
import { DEFAULT_LOGO_URL } from '@/shared/branding';
import { ErrorState } from '@/shared/components/ErrorState';
import { PageHeader } from '@/shared/components/PageHeader';
import { parametersApi, type Parameter } from '../api/parametersApi';

/** Katalog kimliginin onekine gore gruplar (Y4 katalogundaki bolumler). */
const GROUPS = ['GRN', 'KML', 'HSP', 'ENT', 'BLD'] as const;

const groupOf = (key: string) => key.split('-')[1] ?? 'GRN';

/**
 * En kucuk parametre ekrani (SYG-KMLK-076): yalnizca T3 parametreleri ve kurumsal logo.
 *
 * Deger turune gore duzenlenir ve sunucuda dogrulanir; gecersiz deger kaydedilmez. Sir
 * niteligindeki parametrelerin degeri gosterilmez, yalnizca yeni deger yazilir (`KR-071`).
 * Parametre ekraninin tamami Y4 kapsamindadir ve bu ekrani genisletir.
 */
export function ParametersPage() {
  const { t } = useTranslation();
  const canView = usePermission(permissions.parameterView);
  const canUpdate = usePermission(permissions.parameterUpdate);

  const parameters = useQuery({
    queryKey: queryKeys.system.parameters,
    queryFn: parametersApi.list,
    enabled: canView,
  });

  if (!canView) {
    return (
      <Stack spacing={3}>
        <PageHeader title={t('system.parameters.title')} />
        <Alert severity="warning">{t('error.forbidden')}</Alert>
      </Stack>
    );
  }

  return (
    <Stack spacing={3}>
      <PageHeader
        title={t('system.parameters.title')}
        description={t('system.parameters.subtitle')}
      />

      <LogoSection canUpdate={canUpdate} />

      {parameters.error ? (
        <ErrorState
          error={
            parameters.error instanceof ApiError
              ? parameters.error
              : new ApiError({ message: t('error.generic'), isNetworkError: false })
          }
          onRetry={() => void parameters.refetch()}
        />
      ) : null}

      {GROUPS.map((group) => {
        const items = (parameters.data ?? []).filter((item) => groupOf(item.key) === group);
        if (items.length === 0) {
          return null;
        }

        return (
          <Paper
            key={group}
            variant="outlined"
            component="section"
            aria-labelledby={`group-${group}`}
          >
            <Typography
              id={`group-${group}`}
              variant="h6"
              component="h2"
              sx={{ px: 3, pt: 2.5, pb: 1 }}
            >
              {t(`system.parameters.groups.${group}`)}
            </Typography>
            <Stack divider={<Divider flexItem />}>
              {items.map((item) => (
                <ParameterRow key={item.key} parameter={item} canUpdate={canUpdate} />
              ))}
            </Stack>
          </Paper>
        );
      })}
    </Stack>
  );
}

interface ParameterRowProps {
  parameter: Parameter;
  canUpdate: boolean;
}

/** Tek parametre: turune uygun alan, kaydet dugmesi ve sunucunun dogrulama iletisi. */
function ParameterRow({ parameter, canUpdate }: ParameterRowProps) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const isSecret = parameter.type === 'secret';
  const [value, setValue] = useState(isSecret ? '' : (parameter.value ?? ''));
  const [saved, setSaved] = useState(false);

  const labelKey = `system.parameters.labels.${parameter.key}`;
  const label = i18n.exists(labelKey) ? t(labelKey) : parameter.description;

  const save = useMutation({
    mutationFn: (next: string) => parametersApi.update(parameter.key, next),
    onMutate: () => setSaved(false),
    onSuccess: async () => {
      setSaved(true);
      if (isSecret) {
        setValue('');
      }

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.system.parameters }),
        queryClient.invalidateQueries({ queryKey: queryKeys.identity.publicSettings }),
      ]);
    },
  });

  const error =
    save.error instanceof ApiError
      ? (save.error.errors?.['value']?.[0] ?? save.error.message)
      : save.error
        ? t('error.generic')
        : null;
  const changed = isSecret ? value.length > 0 : value !== (parameter.value ?? '');
  const range =
    parameter.type === 'number' && parameter.min != null && parameter.max != null
      ? t('system.parameters.range', { min: parameter.min, max: parameter.max })
      : undefined;
  const id = `parameter-${parameter.key}`;

  return (
    <Stack
      direction={{ xs: 'column', md: 'row' }}
      spacing={2}
      sx={{ px: 3, py: 2, alignItems: { md: 'center' } }}
    >
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Typography component="label" htmlFor={id} variant="body1" sx={{ fontWeight: 500 }}>
          {label}
        </Typography>
        <Stack direction="row" spacing={1} sx={{ mt: 0.5, alignItems: 'center', flexWrap: 'wrap' }}>
          <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
            {parameter.key}
          </Typography>
          <Chip
            size="small"
            variant="outlined"
            label={
              isSecret
                ? t(
                    parameter.isSet
                      ? 'system.parameters.secretSet'
                      : 'system.parameters.secretNotSet',
                  )
                : t(`system.parameters.source.${parameter.source}`)
            }
          />
        </Stack>
      </Box>

      <Box sx={{ width: { xs: '100%', md: 360 } }}>
        {parameter.type === 'toggle' ? (
          <FormControlLabel
            control={
              <Switch
                id={id}
                checked={value === 'true'}
                disabled={!canUpdate || save.isPending}
                onChange={(event) => {
                  const next = event.target.checked ? 'true' : 'false';
                  setValue(next);
                  save.mutate(next);
                }}
              />
            }
            label={t(value === 'true' ? 'system.parameters.on' : 'system.parameters.off')}
          />
        ) : (
          <Stack
            component="form"
            direction="row"
            spacing={1}
            noValidate
            sx={{ alignItems: 'flex-start' }}
            onSubmit={(event) => {
              event.preventDefault();
              save.mutate(value);
            }}
          >
            <TextField
              id={id}
              size="small"
              fullWidth
              value={value}
              onChange={(event) => {
                setValue(event.target.value);
                setSaved(false);
              }}
              disabled={!canUpdate}
              type={isSecret ? 'password' : parameter.type === 'number' ? 'number' : 'text'}
              autoComplete={isSecret ? 'new-password' : 'off'}
              placeholder={isSecret ? t('system.parameters.secretPlaceholder') : undefined}
              error={Boolean(error)}
              helperText={
                error ??
                (saved ? t('system.parameters.saved') : range) ??
                (parameter.type === 'list' ? t('system.parameters.listHint') : undefined)
              }
              slotProps={{
                htmlInput: {
                  min: parameter.min ?? undefined,
                  max: parameter.max ?? undefined,
                },
                formHelperText: { role: error ? 'alert' : undefined },
              }}
            />
            {canUpdate ? (
              <Button type="submit" variant="outlined" disabled={!changed || save.isPending}>
                {t('system.parameters.save')}
              </Button>
            ) : null}
          </Stack>
        )}
        {parameter.type === 'toggle' && (error || saved) ? (
          <Typography
            variant="caption"
            color={error ? 'error' : 'success.main'}
            role={error ? 'alert' : 'status'}
          >
            {error ?? t('system.parameters.saved')}
          </Typography>
        ) : null}
      </Box>
    </Stack>
  );
}

interface LogoSectionProps {
  canUpdate: boolean;
}

/** Kurumsal logo (PRM-GRN-01): onizleme, yukleme ve varsayilana donus. */
function LogoSection({ canUpdate }: LogoSectionProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const input = useRef<HTMLInputElement>(null);
  const [version, setVersion] = useState(() => Date.now());
  const [uploadedMissing, setUploadedMissing] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  const refresh = async (text: string) => {
    setUploadedMissing(false);
    setVersion(Date.now());
    setMessage(text);
    await queryClient.invalidateQueries({ queryKey: queryKeys.identity.publicSettings });
  };

  const upload = useMutation({
    mutationFn: parametersApi.uploadLogo,
    onMutate: () => setMessage(null),
    onSuccess: () => refresh(t('system.logo.uploaded')),
  });

  const remove = useMutation({
    mutationFn: parametersApi.removeLogo,
    onMutate: () => setMessage(null),
    onSuccess: () => refresh(t('system.logo.removed')),
  });

  const error = upload.error ?? remove.error;

  return (
    <Paper variant="outlined" component="section" aria-labelledby="logo-title" sx={{ p: 3 }}>
      <Typography id="logo-title" variant="h6" component="h2">
        {t('system.logo.title')}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {t('system.logo.hint')}
      </Typography>

      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={3}
        sx={{ alignItems: { sm: 'center' } }}
      >
        <Box
          sx={{
            p: 2,
            borderRadius: 2,
            border: 1,
            borderColor: 'divider',
            minWidth: 200,
            display: 'grid',
            placeItems: 'center',
          }}
        >
          <Box
            component="img"
            src={uploadedMissing ? DEFAULT_LOGO_URL : `/api/v1/system/logo?t=${version}`}
            alt={t('system.logo.previewAlt')}
            onError={() => setUploadedMissing(true)}
            sx={{ height: 44, width: 'auto', maxWidth: '100%' }}
          />
          <Typography variant="caption" color="text.secondary" sx={{ mt: 1 }}>
            {t(uploadedMissing ? 'system.logo.usingDefault' : 'system.logo.usingUploaded')}
          </Typography>
        </Box>

        {canUpdate ? (
          <Stack spacing={1} sx={{ alignItems: 'flex-start' }}>
            <input
              ref={input}
              type="file"
              accept="image/png,image/jpeg"
              hidden
              aria-label={t('system.logo.choose')}
              onChange={(event) => {
                const file = event.target.files?.[0];
                if (file) {
                  upload.mutate(file);
                }

                event.target.value = '';
              }}
            />
            <Button
              variant="contained"
              onClick={() => input.current?.click()}
              disabled={upload.isPending}
            >
              {t('system.logo.choose')}
            </Button>
            {!uploadedMissing ? (
              <Button color="inherit" onClick={() => remove.mutate()} disabled={remove.isPending}>
                {t('system.logo.reset')}
              </Button>
            ) : null}
          </Stack>
        ) : null}
      </Stack>

      {message ? (
        <Alert severity="success" role="status" sx={{ mt: 2 }}>
          {message}
        </Alert>
      ) : null}
      {error ? (
        <Alert severity="error" role="alert" sx={{ mt: 2 }}>
          {error instanceof ApiError ? error.message : t('error.generic')}
        </Alert>
      ) : null}
    </Paper>
  );
}
