import { useMutation } from '@tanstack/react-query';
import { getItem } from '../../../shared/storage/secureStore';
import { loginUserWithKeys } from '../api/loginApi';
import { createAndStoreKeys } from '../../../shared/crypto/createAndStoreKeys';
import type { ILoginInput, LoginResponse } from '../types/login.types';

type LoginFormInput = Omit<ILoginInput, 'publicKey'>;

export const useLoginMutation = () => {
	return useMutation<LoginResponse, Error, LoginFormInput>({
		mutationFn: async (data) => {
			let publicKey = await getItem<string>(`publicKey:${data.login}`);

			if (!publicKey) {
				console.warn('Ключи не найдены, создаём новые…');
				await createAndStoreKeys(data.login, data.password);
				publicKey = await getItem<string>(`publicKey:${data.login}`);
				if (!publicKey) {
					throw new Error('Не удалось создать публичный ключ');
				}
			}

			const res = await loginUserWithKeys({
				login: data.login,
				password: data.password,
				publicKey,
			});

			return res;
		},
		onSuccess: (data) => {
			console.log('✅ Успешный вход, код:', data.data);
		},
		onError: (error: Error) => {
			console.error('❌ Ошибка входа:', error.message);
		},

	});
};
