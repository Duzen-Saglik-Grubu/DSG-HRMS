import { ApiError } from '@/shared/api/problemDetails';

/** Uyelik akisinda bir hatanin kullaniciya nasil gosterilecegi. */
export type RegistrationErrorKind =
  'rateLimited' | 'sessionExpired' | 'channelUnavailable' | 'fields' | 'unexpected';

/** Hatayi turune ayirir. Alan hatalari formun kendisine baglanir. */
export function classifyRegistrationError(error: unknown): RegistrationErrorKind {
  if (!(error instanceof ApiError)) {
    return 'unexpected';
  }

  switch (error.status) {
    case 429:
      return 'rateLimited';
    case 404:
      return 'sessionExpired';
    case 422:
      return 'channelUnavailable';
    case 400:
      return error.errors ? 'fields' : 'unexpected';
    default:
      return 'unexpected';
  }
}
