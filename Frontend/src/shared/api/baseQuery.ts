import axios, { AxiosError } from 'axios';
import { $auth, patchAuthData, clearAuthData } from '../../features/auth/model/auth';
import { saveRefreshToken, clearRefreshToken } from '../../shared/storage/tokenStore';
import { refreshTokenApi } from '../../features/auth/api/authApi';
import { redirect } from '@tanstack/react-router';
import type { AxiosRequestConfig } from 'axios';

interface CustomAxiosRequestConfig extends AxiosRequestConfig {
	_retry?: boolean;
}

const BASE_URL = 'https://api.chatflowonline.ru/v1';

export const api = axios.create({
	baseURL: BASE_URL,
	headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use(config => {
	const token = $auth.getState()?.accessToken;
	if (token) config.headers.Authorization = `Bearer ${token}`;
	return config;
});


api.interceptors.response.use(
	res => res,
	async (error: AxiosError) => {
		const originalRequest = error.config as CustomAxiosRequestConfig;

		if (error.response?.status === 401 && !originalRequest._retry) {
			originalRequest._retry = true;

			const auth = $auth.getState();
			if (!auth?.refreshToken) {
				clearAuthData();
				await clearRefreshToken();
				throw redirect({ to: '/login' });
			}

			try {
				const newData = await refreshTokenApi(auth.refreshToken);

				patchAuthData({
					accessToken: newData.accessToken,
					refreshToken: newData.refreshToken,
					accessExpiresAt: newData.accessExpiresAt,
				});
				await saveRefreshToken(newData.refreshToken);

				originalRequest.headers = {
					...originalRequest.headers,
					Authorization: `Bearer ${newData.accessToken}`,
				};

				return api(originalRequest);
			} catch {
				clearAuthData();
				await clearRefreshToken();
				throw redirect({ to: '/login' });
			}
		}

		return Promise.reject(error);
	}
);
