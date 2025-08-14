import { createAndStoreKeys } from '../../../shared/crypto/createAndStoreKeys';

export interface RegistrationDto {
	surname: string;
	name: string;
	login: string;
	password: string;
}

interface RegistrationResponse {
	message: string;
	successfully: boolean;
	status: number;
	type: string;
	data: {
		code: string;
	};
}

export const registerUser = async (
	data: RegistrationDto
): Promise<RegistrationResponse> => {
	await createAndStoreKeys(data.login, data.password);

	const response = await fetch('https://api.chatflowonline.ru/v1/auth/registration', {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json',
			'Accept': 'application/json',
			'User-Agent': navigator.userAgent,
		},
		body: JSON.stringify(data),
	});

	if (!response.ok) {
		throw new Error('Ошибка регистрации');
	}

	return response.json();
};
