import { describe, expect, it } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { toIsoDate } from '../birthDate';
import { classifyFlowError } from '../flowErrorKind';

describe('toIsoDate', () => {
  it('yerel takvim gununu YYYY-AA-GG bicimine cevirir', () => {
    expect(toIsoDate(new Date(1985, 3, 12))).toBe('1985-04-12');
    // Gece yarisina yakin saat UTC'ye cevrilseydi gun kayardi.
    expect(toIsoDate(new Date(1990, 0, 1, 0, 30))).toBe('1990-01-01');
    expect(toIsoDate(new Date(1990, 11, 31, 23, 59))).toBe('1990-12-31');
  });
});

describe('classifyFlowError', () => {
  const error = (status: number, errors?: Record<string, string[]>) =>
    new ApiError({ message: 'x', status, errors, isNetworkError: false });

  it('bilinen durumlari ayirir', () => {
    expect(classifyFlowError(error(429))).toBe('rateLimited');
    expect(classifyFlowError(error(422))).toBe('channelUnavailable');
    expect(classifyFlowError(error(400, { password: ['kisa'] }))).toBe('fields');
  });

  it('suresi doldu yalnizca uygulamanin "kayit yok" yanitinda ve islem basladiktan sonra soylenir', () => {
    const notFound = new ApiError({
      message: 'x',
      status: 404,
      isNetworkError: false,
      type: 'https://dsg-hrms/errors/not-found',
    });

    expect(classifyFlowError(notFound)).toBe('sessionExpired');
    // Ilk adimda islem yoktur; 404 baska bir soruna isaret eder.
    expect(classifyFlowError(notFound, { canExpire: false })).toBe('unexpected');
    // Turu olmayan 404: guncellenmemis sunucu veya yanlis adres.
    expect(classifyFlowError(error(404))).toBe('unexpected');
  });

  it('digerlerini beklenmeyen sayar', () => {
    expect(classifyFlowError(error(400))).toBe('unexpected');
    expect(classifyFlowError(error(500))).toBe('unexpected');
    expect(classifyFlowError(new Error('x'))).toBe('unexpected');
  });
});
