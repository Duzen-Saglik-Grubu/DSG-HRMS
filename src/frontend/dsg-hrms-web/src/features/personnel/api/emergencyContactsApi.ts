import { apiClient } from '@/shared/api/client';
import type { ApiSchemas } from '@/shared/api/schemas';

export type EmergencyContact = ApiSchemas['EmergencyContactResponse'];
export type RelationshipOption = ApiSchemas['RelationshipOptionResponse'];

/** Form girisi; yakinlik tanimin kimligidir (KR-135). */
export interface EmergencyContactInput {
  relationshipId: string;
  firstName: string;
  lastName: string;
  mobilePhone: string;
}

const path = (personId: string) =>
  `/personnel/persons/${encodeURIComponent(personId)}/emergency-contacts`;

/**
 * Acil durum kisileri (PG-PERS-44; SYG-PERS-043). Yalnizca `personnel.person.update` izni olan
 * kullanici yazar; okuma kartin korumali bolumundendir (SYG-PERS-052).
 */
export const emergencyContactsApi = {
  /** Yeni secim icin aktif yakinlik tanimlari (referans veri, KR-135). */
  relationships: async (): Promise<RelationshipOption[]> => {
    const response = await apiClient.get<RelationshipOption[]>(
      '/personnel/emergency-contact-relationships',
    );
    return response.data;
  },

  add: async (personId: string, input: EmergencyContactInput): Promise<EmergencyContact> => {
    const response = await apiClient.post<EmergencyContact>(path(personId), input);
    return response.data;
  },

  change: async (
    personId: string,
    contactId: string,
    input: EmergencyContactInput,
  ): Promise<EmergencyContact> => {
    const response = await apiClient.put<EmergencyContact>(
      `${path(personId)}/${encodeURIComponent(contactId)}`,
      input,
    );
    return response.data;
  },

  remove: async (personId: string, contactId: string): Promise<void> => {
    await apiClient.delete(`${path(personId)}/${encodeURIComponent(contactId)}`);
  },
};
