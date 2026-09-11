import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Button } from '@mui/material';
import { EmptyState } from '../EmptyState';

describe('EmptyState', () => {
  it('varsayilan olarak anlasilir bir mesaj gosterir', () => {
    render(<EmptyState />);

    expect(screen.getByText('Henüz kayıt yok')).toBeInTheDocument();
  });

  it('kullaniciya bir sonraki adimi onerir', () => {
    // "Kayit yok" demek yetmez: kullanici bunun hata mi, yetki sorunu mu, yoksa
    // gercekten bos bir liste mi oldugunu ayirt edemez (ADR-0015 §6).
    render(
      <EmptyState
        title="Henüz izin talebiniz yok"
        description="Yıllık izin talebinizi buradan oluşturabilirsiniz."
        action={<Button>Yeni talep oluştur</Button>}
      />,
    );

    expect(screen.getByText('Henüz izin talebiniz yok')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Yeni talep oluştur' })).toBeInTheDocument();
  });
});
