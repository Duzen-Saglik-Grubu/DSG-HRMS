import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import { Button } from '@mui/material';
import BadgeOutlined from '@mui/icons-material/BadgeOutlined';
import { renderWithProviders } from '@/test/render';
import { PageHeader } from '../PageHeader';

function renderHeader(ui: React.ReactElement) {
  // Serit tema belirteclerini kullanir; uygulamadaki saglayicilarla render edilir.
  return renderWithProviders(ui);
}

describe('PageHeader', () => {
  it('baslik ve aciklamayi gosterir', () => {
    renderHeader(<PageHeader title="İzin Talepleri" description="Bekleyen talepleriniz" />);

    expect(screen.getByRole('heading', { name: 'İzin Talepleri' })).toBeInTheDocument();
    expect(screen.getByText('Bekleyen talepleriniz')).toBeInTheDocument();
  });

  it('sayfa basligini h2 olarak verir', () => {
    // Sayfada tek bir h1 bulunur ve o uygulama kabugundadir (ADR-0015 §7).
    renderHeader(<PageHeader title="İzin Talepleri" />);

    expect(screen.getByRole('heading', { level: 2, name: 'İzin Talepleri' })).toBeInTheDocument();
  });

  it('kirinti yolunu baglantilariyla gosterir', () => {
    renderHeader(
      <PageHeader
        title="Yeni Talep"
        breadcrumbs={[{ label: 'İzin', to: '/izin' }, { label: 'Yeni Talep' }]}
      />,
    );

    expect(screen.getByRole('link', { name: 'İzin' })).toHaveAttribute('href', '/izin');
    // Bulunulan sayfa BAGLANTI DEGILDIR; kullanici zaten oradadir.
    expect(screen.queryByRole('link', { name: 'Yeni Talep' })).not.toBeInTheDocument();
  });

  it('eylem dugmelerini gosterir', () => {
    renderHeader(<PageHeader title="İzin Talepleri" actions={<Button>Yeni talep</Button>} />);

    expect(screen.getByRole('button', { name: 'Yeni talep' })).toBeInTheDocument();
  });

  it('simge ve dalga deseni ekran okuyucudan gizlidir', () => {
    // #322: simge menudeki simgenin tekraridir; anlam tasiyan tek icerik basliktir.
    const { container } = renderHeader(<PageHeader title="Personel" icon={BadgeOutlined} />);

    const hidden = container.querySelectorAll('[aria-hidden="true"]');
    expect(hidden.length).toBeGreaterThanOrEqual(2);
    hidden.forEach((element) => expect(element).not.toHaveTextContent(/\S/));
  });

  it('kabugu olmayan ekranda h1 olarak verilebilir', () => {
    renderHeader(<PageHeader title="Günaydın" headingLevel="h1" />);

    expect(screen.getByRole('heading', { level: 1, name: 'Günaydın' })).toBeInTheDocument();
  });
});
