import { api } from '../../../shared/api/baseQuery';
import type { ILoginInput, LoginResponse } from '../types/login.types';
import { generateKeyPairSPKI } from '../../../shared/crypto/webcrypto';

export const loginApi = async (data: ILoginInput) => {
	const { publicKeySPKI } = await generateKeyPairSPKI();
	
	const payload = {
		...data,
		publicKey: publicKeySPKI,
	};
	
	console.log('Отправка на сервер:', payload);
	
	return api.post<LoginResponse>('/auth/authorization', payload, {
		headers: {
			'User-Agent': navigator.userAgent,
		},
	});
};
