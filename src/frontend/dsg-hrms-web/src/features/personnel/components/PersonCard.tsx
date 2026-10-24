import { Avatar, Card, CardContent, Chip, Stack, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import type { PersonListItem } from '../api/personsApi';
import { joinParts } from '../display';

/** Fotograf gelene kadar ad ve soyadin bas harfleri (SYG-PERS-040 sonraki is). */
function initials(person: PersonListItem): string {
  return `${person.firstName.charAt(0)}${person.lastName.charAt(0)}`;
}

/**
 * Liste karti (SYG-PERS-023): fotograf, ad soyad, sicil, sube / birim ve unvan / gorev. Adlar
 * LOGO'daki gibi gosterilir; gosterim icin buyuk-kucuk harf donusumu YAPILMAZ. Birden fazla
 * sicili olan kiside "X sicil" isareti bulunur (SYG-PERS-022).
 */
export function PersonCard({ person }: { person: PersonListItem }) {
  const { t } = useTranslation();
  const name = `${person.firstName} ${person.lastName}`;
  const employmentCount = Number(person.employmentCount);

  return (
    <Card variant="outlined" component="article" aria-label={name} sx={{ height: '100%' }}>
      <CardContent>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start' }}>
          <Avatar
            aria-label={t('personnel.list.photo', { name })}
            sx={{ width: 56, height: 56, bgcolor: 'primary.light', fontSize: '1.1rem' }}
          >
            {initials(person)}
          </Avatar>
          <Stack spacing={0.5} sx={{ minWidth: 0, flexGrow: 1 }}>
            <Typography variant="subtitle1" component="h3" sx={{ fontWeight: 600 }} noWrap>
              {name}
            </Typography>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
              <Typography variant="body2" color="text.secondary">
                {person.registryCode ?? '—'}
              </Typography>
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
            <Typography
              variant="body2"
              noWrap
              title={joinParts(person.branchName, person.departmentName)}
            >
              {joinParts(person.branchName, person.departmentName)}
            </Typography>
            <Typography
              variant="body2"
              color="text.secondary"
              noWrap
              title={joinParts(person.titleName, person.dutyName)}
            >
              {joinParts(person.titleName, person.dutyName)}
            </Typography>
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
}
