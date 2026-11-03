import { useState, type ReactNode } from 'react';
import { Box, Chip, Grid, Paper, Stack, Tab, Tabs, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useTranslation } from 'react-i18next';
import type { EmploymentCard, PersonCard } from '../api/personsApi';
import { displayDate, joinParts, orderEmployments } from '../display';
import { PersonAvatar } from '@/shared/components/PersonAvatar';
import { SyncFreshness } from './SyncFreshness';

/**
 * Kisi karti gorunumu: "Profilim" ve listeden acilan kart ayni gorunumu kullanir (SYG-PERS-034,
 * 037, 038, 044). Gorunurluk sunucudadir: gorulemeyen bolum (ornegin adres) yanitta hic yoktur
 * ve ekranda da hic gosterilmez; "kayitli degil" yalnizca gorulebilen ama bos alanda yazar.
 */
export function PersonProfile({ person }: { person: PersonCard }) {
  const { t } = useTranslation();
  const employments = orderEmployments(person.employments);
  const primary = employments[0];

  return (
    <Stack spacing={2.5}>
      <ProfileHeader person={person} primary={primary} />

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, md: 5 }}>
          <Stack spacing={2.5}>
            <Section title={t('personnel.card.sections.general')}>
              <GeneralInfo person={person} />
            </Section>
            <Section title={t('personnel.card.sections.contact')}>
              <ContactInfo person={person} />
            </Section>
          </Stack>
        </Grid>
        <Grid size={{ xs: 12, md: 7 }}>
          <Employments employments={employments} />
        </Grid>
      </Grid>
    </Stack>
  );
}

function ProfileHeader({
  person,
  primary,
}: {
  person: PersonCard;
  primary: EmploymentCard | undefined;
}) {
  const { t } = useTranslation();
  const name = `${person.firstName} ${person.lastName}`;
  const active = person.employments.some((employment) => employment.isActive);
  const work = primary?.workLife;
  const summary = [
    joinParts(work?.titleName ?? null, primary?.duty?.name ?? null),
    joinParts(work?.branchName ?? null, work?.departmentName ?? null),
  ].filter((part) => part !== '—');

  return (
    <Paper
      variant="outlined"
      sx={(theme) => ({
        p: { xs: 2.5, sm: 3 },
        background: `linear-gradient(135deg, ${alpha(theme.palette.primary.main, 0.06)}, ${theme.palette.background.paper} 60%)`,
      })}
    >
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={{ xs: 2, sm: 3 }}
        sx={{ alignItems: 'center', textAlign: { xs: 'center', sm: 'left' } }}
      >
        <Box
          sx={{
            p: '4px',
            borderRadius: '50%',
            border: 2,
            borderColor: active ? 'primary.light' : 'warning.light',
          }}
        >
          {/* Kendi karti "me" ucundan: gorunur kalma suresi kisinin kendisine uygulanmaz. */}
          <PersonAvatar
            source={person.isSelf ? 'me' : { personId: person.personId }}
            hasPhoto={person.hasPhoto}
            firstName={person.firstName}
            lastName={person.lastName}
            size={96}
          />
        </Box>
        <Box sx={{ minWidth: 0 }}>
          <Typography variant="h5" component="h1" sx={{ fontWeight: 700 }}>
            {name}
          </Typography>
          {summary.length > 0 ? (
            <Typography color="text.secondary">{summary.join(' · ')}</Typography>
          ) : null}
          <Stack
            direction="row"
            spacing={1}
            useFlexGap
            sx={{ mt: 1, flexWrap: 'wrap', justifyContent: { xs: 'center', sm: 'flex-start' } }}
          >
            <Chip
              size="small"
              color={active ? 'success' : 'warning'}
              label={active ? t('personnel.card.active') : t('personnel.card.terminated')}
            />
            {primary ? <Chip size="small" variant="outlined" label={primary.registryCode} /> : null}
            {primary ? <Chip size="small" variant="outlined" label={primary.companyName} /> : null}
          </Stack>
        </Box>
        <Box
          sx={{
            flexGrow: 1,
            display: 'flex',
            justifyContent: { xs: 'center', sm: 'flex-end' },
            alignSelf: { sm: 'flex-start' },
          }}
        >
          <SyncFreshness />
        </Box>
      </Stack>
    </Paper>
  );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <Paper variant="outlined" component="section" aria-label={title} sx={{ p: 2.5 }}>
      <Typography variant="subtitle1" component="h2" sx={{ fontWeight: 700, mb: 2 }}>
        {title}
      </Typography>
      {children}
    </Paper>
  );
}

/**
 * Etiket ve deger cifti. Bos deger iletisim bilgilerinde "Kayitli degil" (SYG-PERS-037), calisma
 * hayatinda bos isaretidir (SYG-PERS-038: karsiligi olmayan kod bos gorunur).
 */
function Field({
  label,
  value,
  wide,
  emptyText,
}: {
  label: string;
  value: ReactNode;
  wide?: boolean;
  emptyText?: string;
}) {
  const { t } = useTranslation();
  const empty = value === null || value === undefined || value === '';

  return (
    <Grid size={{ xs: 12, sm: wide ? 12 : 6 }}>
      <Typography variant="caption" color="text.secondary" component="dt">
        {label}
      </Typography>
      <Typography
        component="dd"
        sx={{ m: 0, fontWeight: 500, color: empty ? 'text.disabled' : 'text.primary' }}
      >
        {empty ? (emptyText ?? t('personnel.card.notRegistered')) : value}
      </Typography>
    </Grid>
  );
}

function Fields({ children }: { children: ReactNode }) {
  return (
    <Grid container spacing={2} component="dl" sx={{ m: 0 }}>
      {children}
    </Grid>
  );
}

function GeneralInfo({ person }: { person: PersonCard }) {
  const { t } = useTranslation();
  const field = (name: string) => t(`personnel.card.fields.${name}`);

  return (
    <Fields>
      <Field label={field('firstName')} value={person.firstName} />
      <Field label={field('lastName')} value={person.lastName} />
      {/* Onceki soyadi yalnizca kadin personelde ve doluysa gosterilir (SYG-PERS-034). */}
      {person.previousSurname ? (
        <Field label={field('previousSurname')} value={person.previousSurname} />
      ) : null}
      <Field
        label={field('nationalId')}
        value={
          <Box component="span" sx={{ fontVariantNumeric: 'tabular-nums' }}>
            {person.nationalId}
          </Box>
        }
      />
      <Field
        label={field('gender')}
        value={person.gender ? t(`personnel.list.values.gender.${person.gender}`) : null}
      />
      <Field label={field('birthDate')} value={displayDate(person.birthDate)} />
      <Field
        label={field('age')}
        value={t('personnel.card.ageValue', { age: Number(person.age) })}
      />
      <Field label={field('education')} value={person.educationName} />
      <Field
        label={field('maritalStatus')}
        value={
          person.maritalStatus
            ? t(`personnel.list.values.maritalStatus.${person.maritalStatus}`)
            : null
        }
      />
      {/* Kan grubu yalnizca gorme hakki olana gelir (SYG-PERS-034, 052); digerinde alan yoktur. */}
      {person.sensitive ? (
        <Field label={field('bloodGroup')} value={person.sensitive.bloodGroup} />
      ) : null}
    </Fields>
  );
}

function ContactInfo({ person }: { person: PersonCard }) {
  const { t } = useTranslation();
  const field = (name: string) => t(`personnel.card.fields.${name}`);

  return (
    <Fields>
      <Field label={field('email')} value={person.email} wide />
      <Field label={field('phone')} value={person.mobilePhone} wide />
      {/* Adres gorunurluge tabidir; goremeyenin yanitinda bolum hic yoktur (SYG-PERS-052). */}
      {person.sensitive ? (
        <Field label={field('address')} value={person.sensitive.address} wide />
      ) : null}
    </Fields>
  );
}

/**
 * Her istihdam bir sekmedir; tek istihdamda sekme cubugu gosterilmez (SYG-PERS-017).
 */
function Employments({ employments }: { employments: EmploymentCard[] }) {
  const { t } = useTranslation();
  const [index, setIndex] = useState(0);
  const current = employments[Math.min(index, employments.length - 1)];

  return (
    <Paper variant="outlined" component="section" aria-label={t('personnel.card.sections.work')}>
      <Box sx={{ px: 2.5, pt: 2.5, pb: employments.length > 1 ? 0 : 2 }}>
        <Typography variant="subtitle1" component="h2" sx={{ fontWeight: 700 }}>
          {t('personnel.card.sections.work')}
        </Typography>
      </Box>
      {employments.length > 1 ? (
        <Tabs
          value={index}
          onChange={(_, value: number) => setIndex(value)}
          variant="scrollable"
          aria-label={t('personnel.card.employments')}
          sx={{ px: 1.5, borderBottom: 1, borderColor: 'divider' }}
        >
          {employments.map((employment) => (
            <Tab
              key={employment.registryCode}
              label={t('personnel.card.employmentTab', {
                registryCode: employment.registryCode,
                status: employment.isActive
                  ? t('personnel.card.active')
                  : t('personnel.card.terminated'),
              })}
            />
          ))}
        </Tabs>
      ) : null}
      <Box sx={{ p: 2.5 }} role={employments.length > 1 ? 'tabpanel' : undefined}>
        {current ? <WorkLife employment={current} /> : null}
      </Box>
    </Paper>
  );
}

/** "Calisma Hayati" (SYG-PERS-038, 039). */
function WorkLife({ employment }: { employment: EmploymentCard }) {
  const { t } = useTranslation();
  const field = (name: string) => t(`personnel.card.fields.${name}`);
  const work = employment.workLife;

  return (
    <Fields>
      <WorkField label={field('city')} value={work?.city} />
      <WorkField label={field('company')} value={employment.companyName} />
      <WorkField label={field('branch')} value={work?.branchName} />
      <WorkField label={field('department')} value={work?.departmentName} />
      {/* Unvan referans verideki karsiligiyla; karsiligi olmayan kod bos gorunur (SYG-PERS-038). */}
      <WorkField label={field('title')} value={work?.titleName} />
      <WorkField label={field('duty')} value={employment.duty?.name} />
      <WorkField
        label={field('status')}
        value={employment.isActive ? t('personnel.card.active') : t('personnel.card.terminated')}
      />
      <WorkField label={field('statusCode')} value={work?.statusCode} />
      <WorkField label={field('registryCode')} value={employment.registryCode} />
      <WorkField label={field('occupationCode')} value={work?.occupationCode} />
      <WorkField label={field('occupationName')} value={work?.occupationName} wide />
      <WorkField label={field('hireDate')} value={displayDate(employment.hireDate)} />
      <WorkField
        label={field('terminationDate')}
        value={employment.terminationDate ? displayDate(employment.terminationDate) : null}
      />
      <WorkField
        label={field('seniority')}
        // Metin sunucuda kuralla uretilir: sifir olan bas birimler yazilmaz (SYG-PERS-039).
        value={employment.seniority.text}
        wide
      />
    </Fields>
  );
}

function WorkField(props: { label: string; value: ReactNode; wide?: boolean }) {
  return <Field {...props} emptyText="—" />;
}
