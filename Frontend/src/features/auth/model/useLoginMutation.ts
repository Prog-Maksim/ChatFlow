import { useMutation } from '@tanstack/react-query';
import { getItem } from '../../../shared/storage/secureStore';
import { loginUserWithKeys } from '../api/loginApi';
import { createAndStoreKeys } from '../../../shared/crypto/createAndStoreKeys';
import { setAuthData } from '../model/auth';
import type { ILoginInput, LoginResponse } from '../types/login.types';
import { loadRefreshToken, saveRefreshToken } from '../../../shared/storage/tokenStore';

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

			return loginUserWithKeys({
				login: data.login,
				password: data.password,
				publicKey,
			});
		},

		onSuccess: async (response) => {
			console.log('🔑 Login response:', response);

			const { data } = response;
			console.log('📦 Refresh from backend:', data.refreshToken);

			setAuthData({
				personId: data.personId,
				deviceId: data.deviceId,
				accessToken: data.accessToken,
				refreshToken: data.refreshToken,
				accessExpiresAt: data['access-expires-at'],
			});

			await saveRefreshToken(data.refreshToken);

			const check = await loadRefreshToken();
			console.log('✅ Проверка сохранения:', check);
		},


		onError: (error) => {
			console.error('❌ Ошибка входа:', error.message);
		},
	});
};
