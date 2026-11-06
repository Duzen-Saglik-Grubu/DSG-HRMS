import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Paper,
  Skeleton,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import AddRounded from '@mui/icons-material/AddRounded';
import BlockRounded from '@mui/icons-material/BlockRounded';
import CategoryOutlined from '@mui/icons-material/CategoryOutlined';
import EditOutlined from '@mui/icons-material/EditOutlined';
import RestoreRounded from '@mui/icons-material/RestoreRounded';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { permissions } from '@/shared/auth/permissions';
import { usePermission } from '@/shared/auth/usePermission';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { ErrorState } from '@/shared/components/ErrorState';
import { PageHeader } from '@/shared/components/PageHeader';
import { referenceApi, type ReferenceDefinition, type ReferenceType } from '../api/referenceApi';

/** Sekme sirasi; kan grubu sistem tanimi oldugu icin sondadir. */
const TYPES: readonly ReferenceType[] = ['title', 'duty', 'relationship', 'bloodGroup'];

/** Sunucudaki sinirlarla aynidir (CreateReferenceDefinitionRequestValidator). */
const MAX_NAME = 200;
const MAX_CODE = 20;

/** Satirdaki durum degisikligi: pasife alma veya yeniden aktif etme (`KR-136`). */
type StatusAction = 'deactivate' | 'reactivate';

const asApiError = (error: unknown, fallback: string) =>
  error instanceof ApiError ? error : new ApiError({ message: fallback, isNetworkError: false });

/**
 * Referans veri ekrani (SYG-PERS-072; ADR-0022 §1). Unvan, gorev, yakinlik ve kan grubu tanimlari
 * pasifleriyle listelenir. `reference.definition.update` izni olan kullanici tanim ekler, adini
 * degistirir, pasife alir ve pasif tanimi yeniden aktif eder (`KR-136`); tanim silinmez. Kan grubu sistem tanimidir, ekrandan degistirilemez
 * (`KR-129`). Izin yalnizca gosterim icindir; asil denetim sunucudadir (ADR-0007).
 */
export function ReferenceDataPage() {
  const { t } = useTranslation();
  const canView = usePermission(permissions.referenceView);
  const canUpdate = usePermission(permissions.referenceUpdate);
  const [type, setType] = useState<ReferenceType>('title');

  const header = (
    <PageHeader
      icon={CategoryOutlined}
      title={t('reference.title')}
      description={t('reference.subtitle')}
    />
  );

  if (!canView) {
    return (
      <Stack spacing={3}>
        {header}
        <Alert severity="warning">{t('error.forbidden')}</Alert>
      </Stack>
    );
  }

  return (
    <Stack spacing={3}>
      {header}

      <Paper variant="outlined">
        <Tabs
          value={type}
          onChange={(_, next: ReferenceType) => setType(next)}
          variant="scrollable"
          allowScrollButtonsMobile
          aria-label={t('reference.types')}
          sx={{ borderBottom: 1, borderColor: 'divider', px: 1 }}
        >
          {TYPES.map((item) => (
            <Tab
              key={item}
              value={item}
              label={t(`reference.type.${item}`)}
              id={`reference-tab-${item}`}
              aria-controls={`reference-panel-${item}`}
            />
          ))}
        </Tabs>

        <Box
          role="tabpanel"
          id={`reference-panel-${type}`}
          aria-labelledby={`reference-tab-${type}`}
          sx={{ p: { xs: 2, sm: 3 } }}
        >
          <DefinitionList key={type} type={type} canUpdate={canUpdate} />
        </Box>
      </Paper>
    </Stack>
  );
}

interface DefinitionListProps {
  type: ReferenceType;
  canUpdate: boolean;
}

/** Bir turun tanimlari, ekleme dugmesi ve satir islemleri. */
function DefinitionList({ type, canUpdate }: DefinitionListProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<ReferenceDefinition | 'new' | null>(null);
  const [pending, setPending] = useState<{
    definition: ReferenceDefinition;
    action: StatusAction;
  } | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [failure, setFailure] = useState<string | null>(null);

  const list = useQuery({
    queryKey: queryKeys.reference.definitions(type),
    queryFn: () => referenceApi.list(type),
  });

  const isSystemType = type === 'bloodGroup';
  const editable = canUpdate && !isSystemType;

  // Tanimlar baska ekranlarin secim listelerini de besler (kisi suzgeci, acil durum formu).
  const refresh = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.reference.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.personnel.filterOptions }),
      queryClient.invalidateQueries({ queryKey: queryKeys.personnel.emergencyRelationships }),
      queryClient.invalidateQueries({ queryKey: queryKeys.personnel.dutyOptions }),
    ]);
  };

  const typeName = t(`reference.type.${type}`).toLocaleLowerCase('tr-TR');

  return (
    <Stack spacing={2}>
      {isSystemType ? (
        <Alert severity="info">{t('reference.systemType')}</Alert>
      ) : (
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={1.5}
          sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' } }}
        >
          <Typography variant="body2" color="text.secondary">
            {t(`reference.hint.${type}`)}
          </Typography>
          {editable ? (
            <Button
              variant="contained"
              startIcon={<AddRounded />}
              onClick={() => {
                setNotice(null);
                setEditing('new');
              }}
              sx={{ whiteSpace: 'nowrap', alignSelf: { xs: 'flex-start', sm: 'auto' } }}
            >
              {t('reference.add', { type: typeName })}
            </Button>
          ) : null}
        </Stack>
      )}

      {notice ? (
        <Alert severity="success" role="status" onClose={() => setNotice(null)}>
          {notice}
        </Alert>
      ) : null}

      {failure ? (
        <Alert severity="error" role="alert" onClose={() => setFailure(null)}>
          {failure}
        </Alert>
      ) : null}

      {list.error ? (
        <ErrorState
          error={asApiError(list.error, t('error.generic'))}
          onRetry={() => void list.refetch()}
        />
      ) : (
        <TableContainer>
          <Table size="small" aria-label={t('reference.caption', { type: typeName })}>
            <TableHead>
              <TableRow>
                <TableCell sx={{ width: 120 }}>{t('reference.columns.code')}</TableCell>
                <TableCell>{t('reference.columns.name')}</TableCell>
                <TableCell sx={{ width: 110 }}>{t('reference.columns.status')}</TableCell>
                {editable ? (
                  <TableCell align="right" sx={{ width: 110 }}>
                    <Box component="span" sx={visuallyHidden}>
                      {t('reference.columns.actions')}
                    </Box>
                  </TableCell>
                ) : null}
              </TableRow>
            </TableHead>
            <TableBody>
              {list.isPending
                ? [0, 1, 2].map((row) => (
                    <TableRow key={row}>
                      <TableCell colSpan={editable ? 4 : 3}>
                        <Skeleton />
                      </TableCell>
                    </TableRow>
                  ))
                : null}

              {list.data?.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={editable ? 4 : 3}>
                    <Typography variant="body2" color="text.secondary">
                      {t('reference.empty')}
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : null}

              {list.data?.map((definition) => (
                <TableRow key={definition.id} hover>
                  <TableCell sx={{ fontVariantNumeric: 'tabular-nums' }}>
                    {definition.code}
                  </TableCell>
                  <TableCell
                    sx={{ color: definition.isActive ? 'text.primary' : 'text.secondary' }}
                  >
                    {definition.name}
                  </TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      variant={definition.isActive ? 'filled' : 'outlined'}
                      color={definition.isActive ? 'success' : 'default'}
                      label={definition.isActive ? t('reference.active') : t('reference.inactive')}
                    />
                  </TableCell>
                  {editable ? (
                    <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                      <Tooltip title={t('reference.rename')}>
                        <IconButton
                          size="small"
                          aria-label={t('reference.renameNamed', { name: definition.name })}
                          onClick={() => {
                            setNotice(null);
                            setEditing(definition);
                          }}
                        >
                          <EditOutlined fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <StatusButton
                        definition={definition}
                        onClick={(action) => {
                          setNotice(null);
                          setFailure(null);
                          setPending({ definition, action });
                        }}
                      />
                    </TableCell>
                  ) : null}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {editing ? (
        <DefinitionDialog
          type={type}
          definition={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={async (message) => {
            setEditing(null);
            setNotice(message);
            await refresh();
          }}
        />
      ) : null}

      {/* Pasife alma geri alinabilir (KR-136); bu yuzden "geri alinamaz" uyarisi gosterilmez. */}
      <ConfirmDialog
        open={pending !== null}
        title={pending ? t(`reference.${pending.action}Title`) : ''}
        description={
          pending ? t(`reference.${pending.action}Text`, { name: pending.definition.name }) : ''
        }
        confirmLabel={t(`reference.${pending?.action ?? 'deactivate'}`)}
        onCancel={() => setPending(null)}
        onConfirm={async () => {
          if (!pending) {
            return;
          }

          const { definition, action } = pending;
          try {
            await (action === 'deactivate'
              ? referenceApi.deactivate(definition.id)
              : referenceApi.reactivate(definition.id));
            setPending(null);
            setNotice(t(`reference.${action}d`, { name: definition.name }));
            await refresh();
          } catch (error) {
            setPending(null);
            setFailure(asApiError(error, t('error.generic')).message);
          }
        }}
      />
    </Stack>
  );
}

/** Ekran okuyucuda okunan ama gorunmeyen metin. */
const visuallyHidden = {
  position: 'absolute',
  width: 1,
  height: 1,
  overflow: 'hidden',
  clip: 'rect(0 0 0 0)',
  whiteSpace: 'nowrap',
} as const;

interface DefinitionDialogProps {
  type: ReferenceType;
  /** Adi degistirilen tanim; yeni tanimda `null`. */
  definition: ReferenceDefinition | null;
  onClose: () => void;
  onSaved: (message: string) => void | Promise<void>;
}

/**
 * Ekleme ve ad degistirme formu. Kod yalnizca eklerken girilir ve sonradan degismez: unvanda
 * zorunludur (LOGO kodu), gorev ve yakinlikta bos birakilirsa sunucu uretir.
 */
function DefinitionDialog({ type, definition, onClose, onSaved }: DefinitionDialogProps) {
  const { t } = useTranslation();
  const [code, setCode] = useState('');
  const [name, setName] = useState(definition?.name ?? '');
  const [missing, setMissing] = useState<Set<'code' | 'name'>>(new Set());
  const codeRequired = type === 'title';

  const save = useMutation({
    mutationFn: async () => {
      if (definition) {
        await referenceApi.rename(definition.id, name.trim());
        return t('reference.renamed', { name: name.trim() });
      }

      const created = await referenceApi.create(type, code, name.trim());
      return t('reference.created', { name: created.name, code: created.code });
    },
    onSuccess: onSaved,
  });

  const serverErrors = save.error instanceof ApiError ? save.error.errors : undefined;
  const fieldError = (field: 'code' | 'name'): string | undefined =>
    missing.has(field) ? t('reference.required') : serverErrors?.[field]?.[0];
  const generalError =
    save.error && !serverErrors?.code && !serverErrors?.name
      ? asApiError(save.error, t('error.generic')).message
      : null;

  return (
    <Dialog open onClose={onClose} aria-labelledby="reference-dialog-title" fullWidth maxWidth="sm">
      <Box
        component="form"
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          const empty = new Set<'code' | 'name'>();
          if (!name.trim()) {
            empty.add('name');
          }
          if (!definition && codeRequired && !code.trim()) {
            empty.add('code');
          }
          setMissing(empty);
          if (empty.size === 0) {
            save.mutate();
          }
        }}
      >
        <DialogTitle id="reference-dialog-title">
          {definition
            ? t('reference.renameTitle')
            : t('reference.addTitle', {
                type: t(`reference.type.${type}`).toLocaleLowerCase('tr-TR'),
              })}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {definition ? (
              <Typography variant="body2" color="text.secondary">
                {t('reference.renameText', { code: definition.code })}
              </Typography>
            ) : (
              <TextField
                label={t('reference.columns.code')}
                value={code}
                onChange={(event) => {
                  setCode(event.target.value);
                  setMissing((current) => new Set([...current].filter((f) => f !== 'code')));
                }}
                required={codeRequired}
                error={Boolean(fieldError('code'))}
                helperText={
                  fieldError('code') ??
                  (codeRequired ? t('reference.codeHintTitle') : t('reference.codeHintAuto'))
                }
                slotProps={{ htmlInput: { maxLength: MAX_CODE, autoComplete: 'off' } }}
              />
            )}

            <TextField
              label={t('reference.columns.name')}
              value={name}
              onChange={(event) => {
                setName(event.target.value);
                setMissing((current) => new Set([...current].filter((f) => f !== 'name')));
              }}
              required
              autoFocus
              error={Boolean(fieldError('name'))}
              helperText={fieldError('name')}
              slotProps={{ htmlInput: { maxLength: MAX_NAME, autoComplete: 'off' } }}
            />

            {generalError ? (
              <Alert severity="error" role="alert">
                {generalError}
              </Alert>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>{t('confirm.cancel')}</Button>
          <Button type="submit" variant="contained" disabled={save.isPending}>
            {t('reference.save')}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}

interface StatusButtonProps {
  definition: ReferenceDefinition;
  onClick: (action: StatusAction) => void;
}

/** Aktif tanimda "Pasife al", pasif tanimda "Yeniden aktif et" (`KR-136`). */
function StatusButton({ definition, onClick }: StatusButtonProps) {
  const { t } = useTranslation();
  const action: StatusAction = definition.isActive ? 'deactivate' : 'reactivate';

  return (
    <Tooltip title={t(`reference.${action}`)}>
      <IconButton
        size="small"
        aria-label={t(`reference.${action}Named`, { name: definition.name })}
        onClick={() => onClick(action)}
      >
        {action === 'deactivate' ? (
          <BlockRounded fontSize="small" />
        ) : (
          <RestoreRounded fontSize="small" />
        )}
      </IconButton>
    </Tooltip>
  );
}
