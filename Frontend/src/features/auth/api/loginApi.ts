import { api } from '../../../shared/api/baseQuery';
import type { LoginRequest, LoginResponse, ILoginInput } from '../types/login.types';
import { generateKeyPairSPKI } from '../../../shared/crypto/webcrypto';
import { createAndStoreKeys } from '../../../shared/crypto/createAndStoreKeys';

export const loginUser = async (data: LoginRequest): Promise<LoginResponse> => {
	await createAndStoreKeys(data.login, data.password);

	const { publicKeySPKI } = await generateKeyPairSPKI();

	const payload: ILoginInput = {
		...data,
		publicKey: publicKeySPKI,
	};

	console.log('Отправка данных при loginUser:', JSON.stringify(payload, null, 2));

	const response = await api.post<LoginResponse>('/auth/authorization', payload);
	console.log('Ответ от сервера (loginUser):', response.data);
	
	return response.data;
};

export const loginUserWithKeys = async (data: ILoginInput): Promise<LoginResponse> => {
	console.log('Отправка данных при loginUserWithKeys:', JSON.stringify(data, null, 2));

	const response = await api.post<LoginResponse>('/auth/authorization', data);
	console.log('Ответ от сервера (loginUserWithKeys):', response.data);
	return response.data;
};
