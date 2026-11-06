import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  List,
  ListItem,
  ListItemText,
  MenuItem,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import AddRounded from '@mui/icons-material/AddRounded';
import DeleteOutlineRounded from '@mui/icons-material/DeleteOutlineRounded';
import EditOutlined from '@mui/icons-material/EditOutlined';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/problemDetails';
import { queryKeys } from '@/shared/api/queryKeys';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import {
  emergencyContactsApi,
  type EmergencyContact,
  type EmergencyContactInput,
} from '../api/emergencyContactsApi';
import { displayMobilePhone } from '../display';

const TEXT_FIELDS = ['firstName', 'lastName', 'mobilePhone'] as const;
const FIELDS = ['relationshipId', ...TEXT_FIELDS] as const;
type FieldName = (typeof FIELDS)[number];

/** Metin alanlarinin en buyuk uzunlugu; sunucudaki sinirla aynidir (SYG-PERS-043). */
const MAX_LENGTH: Record<(typeof TEXT_FIELDS)[number], number> = {
  firstName: 100,
  lastName: 100,
  mobilePhone: 20,
};

const EMPTY: EmergencyContactInput = {
  relationshipId: '',
  firstName: '',
  lastName: '',
  mobilePhone: '',
};

interface EmergencyContactsProps {
  personId: string;
  contacts: EmergencyContact[];
  /** Ekleme, duzenleme ve silme (`personnel.person.update`); izni olan kendi kartinda da yapar (KR-137). */
  editable: boolean;
}

/**
 * Kartin "Acil Durum Kisileri" bolumu (PG-PERS-44; SYG-PERS-043). Bolum yalnizca gorme hakki
 * olana gelir (SYG-PERS-052). Giris izni olmayan kisi kendi kartinda gorur ama degistiremez.
 */
export function EmergencyContacts({ personId, contacts, editable }: EmergencyContactsProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<EmergencyContact | 'new' | null>(null);
  const [deleting, setDeleting] = useState<EmergencyContact | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const refresh = () =>
    queryClient.invalidateQueries({ queryKey: queryKeys.personnel.card(personId) });

  return (
    <Box>
      {contacts.length === 0 ? (
        <Typography sx={{ fontWeight: 500, color: 'text.disabled' }}>
          {t('personnel.card.notRegistered')}
        </Typography>
      ) : (
        <List disablePadding>
          {contacts.map((contact) => (
            <ListItem
              key={contact.id}
              disableGutters
              divider
              secondaryAction={
                editable ? (
                  <Stack direction="row" spacing={0.5}>
                    <Tooltip title={t('personnel.emergency.edit')}>
                      <IconButton
                        edge="end"
                        aria-label={t('personnel.emergency.editNamed', {
                          name: `${contact.firstName} ${contact.lastName}`,
                        })}
                        onClick={() => setEditing(contact)}
                      >
                        <EditOutlined fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title={t('personnel.emergency.delete')}>
                      <IconButton
                        edge="end"
                        aria-label={t('personnel.emergency.deleteNamed', {
                          name: `${contact.firstName} ${contact.lastName}`,
                        })}
                        onClick={() => {
                          setDeleteError(null);
                          setDeleting(contact);
                        }}
                      >
                        <DeleteOutlineRounded fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </Stack>
                ) : undefined
              }
              sx={{ pr: editable ? 10 : 0 }}
            >
              <ListItemText
                primary={`${contact.firstName} ${contact.lastName}`}
                secondary={`${contact.relationship} · ${displayMobilePhone(contact.mobilePhone)}`}
                slotProps={{ primary: { sx: { fontWeight: 500 } } }}
              />
            </ListItem>
          ))}
        </List>
      )}

      {deleteError ? (
        <Alert severity="error" role="alert" sx={{ mt: 1.5 }} onClose={() => setDeleteError(null)}>
          {deleteError}
        </Alert>
      ) : null}

      {editable ? (
        <Button
          startIcon={<AddRounded />}
          onClick={() => setEditing('new')}
          sx={{ mt: 1.5 }}
          size="small"
        >
          {t('personnel.emergency.add')}
        </Button>
      ) : null}

      {editing ? (
        <EmergencyContactDialog
          personId={personId}
          contact={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={async () => {
            setEditing(null);
            await refresh();
          }}
        />
      ) : null}

      <ConfirmDialog
        open={deleting !== null}
        title={t('personnel.emergency.deleteTitle')}
        description={
          deleting
            ? t('personnel.emergency.deleteText', {
                name: `${deleting.firstName} ${deleting.lastName}`,
              })
            : ''
        }
        destructive
        confirmLabel={t('personnel.emergency.delete')}
        onCancel={() => setDeleting(null)}
        onConfirm={async () => {
          if (!deleting) {
            return;
          }

          try {
            await emergencyContactsApi.remove(personId, deleting.id);
            setDeleting(null);
            await refresh();
          } catch (error) {
            setDeleting(null);
            setDeleteError(error instanceof ApiError ? error.message : t('error.generic'));
          }
        }}
      />
    </Box>
  );
}

interface DialogProps {
  personId: string;
  /** Duzenlenen kayit; yeni kayitta `null`. */
  contact: EmergencyContact | null;
  onClose: () => void;
  onSaved: () => void | Promise<void>;
}

/**
 * Ekleme ve duzenleme formu. Dort alan zorunludur; hata ilgili alanin altinda gorunur. Yakinlik
 * referans verideki aktif tanimlardan secilir (KR-135); duzenlenen kaydin tanimi sonradan pasife
 * alindiysa listede yine gorunur ve secili kalir.
 */
function EmergencyContactDialog({ personId, contact, onClose, onSaved }: DialogProps) {
  const { t } = useTranslation();
  const [values, setValues] = useState<EmergencyContactInput>(
    contact
      ? {
          relationshipId: contact.relationshipId,
          firstName: contact.firstName,
          lastName: contact.lastName,
          mobilePhone: displayMobilePhone(contact.mobilePhone),
        }
      : EMPTY,
  );
  const [missing, setMissing] = useState<Set<FieldName>>(new Set());

  const relationships = useQuery({
    queryKey: queryKeys.personnel.emergencyRelationships,
    queryFn: emergencyContactsApi.relationships,
  });
  const options = [...(relationships.data ?? [])];
  if (contact && !options.some((option) => option.id === contact.relationshipId)) {
    options.push({ id: contact.relationshipId, name: contact.relationship });
  }

  const save = useMutation({
    mutationFn: () =>
      contact
        ? emergencyContactsApi.change(personId, contact.id, values)
        : emergencyContactsApi.add(personId, values),
    onSuccess: onSaved,
  });

  const serverErrors = save.error instanceof ApiError ? save.error.errors : undefined;
  const fieldError = (field: FieldName): string | undefined =>
    missing.has(field) ? t('personnel.emergency.required') : serverErrors?.[field]?.[0];
  const generalError =
    save.error && !FIELDS.some((field) => serverErrors?.[field])
      ? save.error instanceof ApiError
        ? save.error.message
        : t('error.generic')
      : null;

  const change = (field: FieldName, value: string) => {
    setValues((current) => ({ ...current, [field]: value }));
    if (missing.has(field)) {
      setMissing((current) => {
        const next = new Set(current);
        next.delete(field);
        return next;
      });
    }
  };

  return (
    <Dialog open onClose={onClose} aria-labelledby="emergency-dialog-title" fullWidth maxWidth="sm">
      <Box
        component="form"
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          const empty = new Set(FIELDS.filter((field) => !values[field].trim()));
          setMissing(empty);
          if (empty.size === 0) {
            save.mutate();
          }
        }}
      >
        <DialogTitle id="emergency-dialog-title">
          {contact ? t('personnel.emergency.editTitle') : t('personnel.emergency.addTitle')}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            <TextField
              select
              label={t('personnel.emergency.fields.relationshipId')}
              value={relationships.isPending ? '' : values.relationshipId}
              onChange={(event) => change('relationshipId', event.target.value)}
              required
              autoFocus
              error={Boolean(fieldError('relationshipId')) || relationships.isError}
              helperText={
                fieldError('relationshipId') ??
                (relationships.isError ? t('personnel.emergency.relationshipsFailed') : undefined)
              }
            >
              {options.map((option) => (
                <MenuItem key={option.id} value={option.id}>
                  {option.name}
                </MenuItem>
              ))}
            </TextField>

            {TEXT_FIELDS.map((field) => (
              <TextField
                key={field}
                label={t(`personnel.emergency.fields.${field}`)}
                value={values[field]}
                onChange={(event) => change(field, event.target.value)}
                required
                type={field === 'mobilePhone' ? 'tel' : 'text'}
                error={Boolean(fieldError(field))}
                helperText={
                  fieldError(field) ??
                  (field === 'mobilePhone' ? t('personnel.emergency.phoneHint') : undefined)
                }
                slotProps={{
                  htmlInput: { maxLength: MAX_LENGTH[field], autoComplete: 'off' },
                }}
              />
            ))}

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
            {t('personnel.emergency.save')}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}
