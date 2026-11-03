import { Box, Card, CardContent, Chip, Stack, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
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
      sx={(theme) => ({
        height: '100%',
        borderColor: 'transparent',
        transition: theme.transitions.create(['box-shadow', 'transform', 'border-color'], {
          duration: 150,
        }),
        '&:hover': {
          boxShadow: theme.palette.surface.shadowHover,
          borderColor: alpha(theme.palette.surface.accent, 0.3),
          transform: 'translateY(-2px)',
        },
        '@media (prefers-reduced-motion: reduce)': { '&:hover': { transform: 'none' } },
      })}
    >
      <CardContent sx={{ pt: 3, '&:last-child': { pb: 2.5 } }}>
        <Stack spacing={1} sx={{ alignItems: 'center', textAlign: 'center' }}>
          <Box
            sx={{
              position: 'relative',
              p: '3px',
              borderRadius: '50%',
              border: 2,
              borderColor: (theme) =>
                person.isActive
                  ? alpha(theme.palette.surface.accent, 0.45)
                  : theme.palette.warning.light,
            }}
          >
            <PersonAvatar
              source={{ personId: person.personId }}
              hasPhoto={person.hasPhoto}
              firstName={person.firstName}
              lastName={person.lastName}
              size={80}
            />
            {/* Aktiflik noktasi susleme amaclidir: ayrilan kiside "Ayrildi" etiketi metin olarak var. */}
            {person.isActive ? (
              <Box
                aria-hidden
                sx={{
                  position: 'absolute',
                  top: 4,
                  right: 4,
                  width: 14,
                  height: 14,
                  borderRadius: '50%',
                  bgcolor: 'success.light',
                  border: 2,
                  borderColor: 'background.paper',
                }}
              />
            ) : null}
          </Box>
          <Box sx={{ width: '100%', minWidth: 0 }}>
            <Typography
              variant="subtitle1"
              component="h3"
              sx={{ fontWeight: 700, lineHeight: 1.3, color: 'primary.main' }}
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
