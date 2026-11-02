import { Box, Card, CardContent, Chip, Stack, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import type { PersonListItem } from '../api/personsApi';
import { joinParts } from '../display';
import { PersonAvatar } from '@/shared/components/PersonAvatar';

/**
 * Liste karti (SYG-PERS-023): fotograf, ad soyad, sicil, sube / birim ve unvan / gorev. Fotograf
 * ustte, bilgiler ortali (ornek tasarim Personel.png, #306). Adlar LOGO'daki gibi gosterilir;
 * gosterim icin buyuk-kucuk harf donusumu YAPILMAZ. Birden fazla sicili olan kiside "X sicil"
 * isareti bulunur (SYG-PERS-022).
 */
export function PersonCard({ person }: { person: PersonListItem }) {
  const { t } = useTranslation();
  const name = `${person.firstName} ${person.lastName}`;
  const employmentCount = Number(person.employmentCount);
  const unit = joinParts(person.branchName, person.departmentName);
  const role = joinParts(person.titleName, person.dutyName);

  return (
    <Card
      variant="outlined"
      component="article"
      aria-label={name}
      sx={{
        height: '100%',
        transition: 'box-shadow 150ms ease, border-color 150ms ease',
        '&:hover': { boxShadow: 3, borderColor: 'primary.light' },
      }}
    >
      <CardContent sx={{ pt: 3, '&:last-child': { pb: 2.5 } }}>
        <Stack spacing={1} sx={{ alignItems: 'center', textAlign: 'center' }}>
          <Box
            sx={{
              p: '3px',
              borderRadius: '50%',
              border: 2,
              borderColor: person.isActive ? 'primary.light' : 'warning.light',
            }}
          >
            <PersonAvatar
              source={{ personId: person.personId }}
              hasPhoto={person.hasPhoto}
              firstName={person.firstName}
              lastName={person.lastName}
              size={72}
            />
          </Box>
          <Box sx={{ width: '100%', minWidth: 0 }}>
            <Typography
              variant="subtitle1"
              component="h3"
              sx={{ fontWeight: 700, lineHeight: 1.3 }}
              noWrap
              title={name}
            >
              {name}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              ({person.registryCode ?? '—'})
            </Typography>
          </Box>
          <Box sx={{ width: '100%', minWidth: 0 }}>
            <Typography variant="body2" noWrap title={unit}>
              {unit}
            </Typography>
            <Typography variant="body2" color="text.secondary" noWrap title={role}>
              {role}
            </Typography>
          </Box>
          {employmentCount > 1 || !person.isActive ? (
            <Stack direction="row" spacing={1} sx={{ justifyContent: 'center' }}>
              {employmentCount > 1 ? (
                <Chip
                  size="small"
                  variant="outlined"
                  label={t('personnel.list.employmentCount', { count: employmentCount })}
                />
              ) : null}
              {person.isActive ? null : (
                <Chip size="small" color="warning" label={t('personnel.list.terminated')} />
              )}
            </Stack>
          ) : null}
        </Stack>
      </CardContent>
    </Card>
  );
}
