import { api } from '../../../shared/api/baseQuery';
import { createAndStoreKeys } from '../../../shared/crypto/createAndStoreKeys';
import type { RegistrationRequest, RegistrationResponse } from '../types/register.types';

export const registerUser = async (
	data: RegistrationRequest
): Promise<RegistrationResponse> => {
	await createAndStoreKeys(data.login, data.password);

	const response = await api.post<RegistrationResponse>(
		'/auth/registration',
		data
	);

	return response.data; 
};
