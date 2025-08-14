import { useMutation } from '@tanstack/react-query';
import { loginApi } from '../api/loginApi';
import type { ILoginInput } from '../types/login.types';
import { getItem } from '../../../shared/storage/secureStore';
import { decryptWithPassword } from '../../../shared/crypto/webcrypto';


export const useLoginMutation = () => {
	return useMutation({
		mutationFn: async (data: ILoginInput) => {
			// 1. Достаём зашифрованный приватный ключ
			const encryptedPriv = await getItem<{ cipherBase64: string; ivBase64: string; saltBase64: string }>(
				`privateKey:${data.login}`
			);
			if (!encryptedPriv) {
				throw new Error('Приватный ключ не найден. Нужно заново зарегистрироваться на этом устройстве.');
			}

			// 2. Дешифруем приватный ключ
			const privRaw = await decryptWithPassword(
				encryptedPriv.cipherBase64,
				encryptedPriv.ivBase64,
				encryptedPriv.saltBase64,
				data.password
			);

			// 3. Импортируем приватный ключ
			const privateKey = await crypto.subtle.importKey(
				'pkcs8',
				privRaw,
				{
					name: 'RSA-OAEP',
					hash: 'SHA-256',
				},
				true,
				['decrypt']
			);

			// 4. Достаём public key (PEM) для отправки на сервер
			const publicKeyPEM = await getItem<string>(`publicKey:${data.login}`);
			if (!publicKeyPEM) {
				throw new Error('Публичный ключ не найден. Нужно заново зарегистрироваться.');
			}

			// 5. Вызываем API логина
			const res = await loginApi({
				login: data.login,
				password: data.password,
				publicKey: publicKeyPEM
			});

			console.log('Приватный ключ успешно расшифрован и импортирован:', privateKey);

			return res;
		},
		onSuccess: (data) => {
			console.log('✅ Успешный вход, код:', data.data);
		},
		onError: (error: any) => {
			console.error('Ошибка входа:', error.message);
		},
	});
};
