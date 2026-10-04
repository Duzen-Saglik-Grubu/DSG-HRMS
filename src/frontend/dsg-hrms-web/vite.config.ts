import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';

/**
 * Vite yapilandirmasi (ADR-0015).
 */
export default defineConfig({
  plugins: [react()],

  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },

  server: {
    port: 5173,
    // API cagrilari gelistirmede backend'e yonlendirilir; boylece tarayici
    // acisindan ayni kokenden gelirler ve CORS yapilandirmasina gerek kalmaz.
    //
    // "/health" AYRICA yazilmalidir: saglik uclari surumlu API yolunun disindadir
    // (sozlesmenin parcasi degillerdir), bu yuzden "/api" kurali onlari kapsamaz.
    proxy: {
      '/api': {
        target: 'http://localhost:5199',
        changeOrigin: true,
      },
      '/health': {
        target: 'http://localhost:5199',
        changeOrigin: true,
      },
    },
  },

  build: {
    // Kaynak haritalari uretilir: uretimde olusan bir hatanin yigin izi
    // okunabilir olmalidir.
    sourcemap: true,
    // Rota bazli kod bolme sonrasi parcalarin buyumesi fark edilmeli (ADR-0015 §11).
    chunkSizeWarningLimit: 600,
  },

  test: {
    // Uctan uca testler (e2e/) Playwright ile ayri calisir; Vitest yalnizca src/ altini alir.
    include: ['src/**/*.{test,spec}.{ts,tsx}'],
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
    coverage: {
      provider: 'v8',
      // Hic test edilmemis dosyalar da olcume GIRER. Aksi hâlde test yazilmayan
      // bir dosya kapsam raporunda hic gorunmez ve oran yaniltici sekilde
      // yuksek cikar.
      include: ['src/**/*.{ts,tsx}'],
      reporter: ['text-summary', 'cobertura'],
      reportsDirectory: './coverage',
      // Uretilen tipler ve saf yapilandirma dosyalari olcume girmez;
      // bizim yazmadigimiz kodun kapsami kalite olcumu degildir.
      exclude: [
        'src/shared/api/generated/**',
        'src/main.tsx',
        'src/test/**',
        '**/*.config.{ts,js}',
        '**/*.d.ts',
      ],
      thresholds: {
        // ADR-0011 §1 - frontend hedefi %70.
        lines: 70,
        statements: 70,
        branches: 70,
        functions: 70,
      },
    },
  },
});
