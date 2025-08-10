import { useMutation } from '@tanstack/react-query';

interface RegistrationDto {
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

const registerUser = async (data: RegistrationDto): Promise<RegistrationResponse> => {
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

export const useRegister = () => {
	return useMutation({
		mutationFn: registerUser,
	});
};
