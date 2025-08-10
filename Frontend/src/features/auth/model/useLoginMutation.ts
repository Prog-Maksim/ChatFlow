import { useMutation } from '@tanstack/react-query';
import { loginApi } from '../api/loginApi';
import type { ILoginInput } from '../types/login.types';

export const useLoginMutation = () => {
	return useMutation({
		mutationFn: (data: ILoginInput) => loginApi(data),
		onSuccess: (data) => {
			console.log('Успешный вход, код:', data.data.code);
		},
		onError: (error: any) => {
			console.error('Ошибка входа:', error.message);
		},
	});
};
