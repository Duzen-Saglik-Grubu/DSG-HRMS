import { describe, expect, it } from 'vitest';
import { ApiError } from '@/shared/api/problemDetails';
import { parseBirthDate } from '../birthDate';
import { classifyRegistrationError } from '../registrationErrorKind';

describe('parseBirthDate', () => {
  it('GG.AA.YYYY bicimini ISO tarihe cevirir', () => {
    expect(parseBirthDate('12.04.1985')).toBe('1985-04-12');
    expect(parseBirthDate(' 01.01.1990 ')).toBe('1990-01-01');
  });

  it.each(['31.02.1990', '1985-04-12', '12/04/1985', '12.4.1985', '01.01.1899', '01.01.2999', ''])(
    'gecersiz veya gelecekteki tarihi reddeder: %s',
    (value) => {
      expect(parseBirthDate(value)).toBeNull();
    },
  );
});

describe('classifyRegistrationError', () => {
  const error = (status: number, errors?: Record<string, string[]>) =>
    new ApiError({ message: 'x', status, errors, isNetworkError: false });

  it('bilinen durumlari ayirir', () => {
    expect(classifyRegistrationError(error(429))).toBe('rateLimited');
    expect(classifyRegistrationError(error(404))).toBe('sessionExpired');
    expect(classifyRegistrationError(error(422))).toBe('channelUnavailable');
    expect(classifyRegistrationError(error(400, { password: ['kisa'] }))).toBe('fields');
  });

  it('digerlerini beklenmeyen sayar', () => {
    expect(classifyRegistrationError(error(400))).toBe('unexpected');
    expect(classifyRegistrationError(error(500))).toBe('unexpected');
    expect(classifyRegistrationError(new Error('x'))).toBe('unexpected');
  });
});
