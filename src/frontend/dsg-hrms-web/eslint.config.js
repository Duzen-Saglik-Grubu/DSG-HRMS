import js from '@eslint/js';
import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import tseslint from 'typescript-eslint';
import prettier from 'eslint-config-prettier';

/**
 * ESLint yapilandirmasi (ADR-0015).
 *
 * Kurallarin bir kismi "stil" degil MIMARI kuraldir: ozellikler arasi dogrudan
 * ice aktarma yasagi ve sabit metin yasagi gibi. Bunlar denetlenmezse zamanla
 * asinir ve geri donusu pahali hâle gelir.
 */
export default tseslint.config(
  { ignores: ['dist', 'coverage', 'src/shared/api/generated'] },

  {
    extends: [js.configs.recommended, ...tseslint.configs.recommendedTypeChecked],
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2022,
      globals: globals.browser,
      parserOptions: {
        project: ['./tsconfig.app.json', './tsconfig.node.json', './tsconfig.test.json'],
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],

      // Ozellikler arasi dogrudan ice aktarma YASAK (ADR-0015 §1).
      // Ortak ihtiyac shared/ altina tasinir; aksi hâlde moduller birbirine
      // gorunmez baglarla baglanir ve tek basina degistirilemez hâle gelir.
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['@/features/*/*', '../../features/*'],
              message:
                'Ozellikler birbirinin icine dogrudan erisemez (ADR-0015 §1). ' +
                'Ortak ihtiyaci shared/ altina tasiyin.',
            },
            {
              group: ['@/shared/api/generated/*'],
              message: 'Uretilen tipler dogrudan kullanilmaz; shared/api uzerinden erisin.',
            },
          ],
        },
      ],

      // Yanlislikla birakilan hata ayiklama ciktisi uretime gitmemeli.
      'no-console': ['error', { allow: ['warn', 'error'] }],

      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
      ],
    },
  },

  // Yonlendirme tanimi bir bilesen degil, veri yapisidir; "hizli yenileme"
  // (fast refresh) uyarisi burada anlamsizdir.
  {
    files: ['src/app/router.tsx'],
    rules: { 'react-refresh/only-export-components': 'off' },
  },

  // Testlerde tip denetimi gevsetilir: sahte nesneler kasitli olarak eksiktir.
  {
    files: ['**/*.test.{ts,tsx}', '**/test/**'],
    rules: {
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/no-explicit-any': 'off',
    },
  },

  // Bicimlendirme Prettier'in isidir; ESLint bicim kurallarini kapatir.
  prettier,
);
