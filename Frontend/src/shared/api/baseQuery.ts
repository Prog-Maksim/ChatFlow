import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { $auth, setAuthData, clearAuthData } from '../../features/auth/model/auth';

export const api = axios.create({
	baseURL: 'https://api.chatflowonline.ru/v1',
	headers: {
		'Content-Type': 'application/json',
	},
});

// ===== REQUEST INTERCEPTOR =====
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
	const auth = $auth.getState();
	if (auth?.accessToken) {
		config.headers.Authorization = `Bearer ${auth.accessToken}`;
	}
	return config;
});

// ===== RESPONSE INTERCEPTOR =====
api.interceptors.response.use(
	(res) => res,
	async (error: AxiosError) => {
		const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

		if (error.response?.status === 401 && !originalRequest._retry) {
			originalRequest._retry = true;

			try {
				const auth = $auth.getState();
				if (!auth?.refreshToken) {
					clearAuthData();
					return Promise.reject(error);
				}

				// запрос на refresh
				const res = await axios.post(
					'https://api.chatflowonline.ru/v1/token/refresh',
					{ refreshToken: auth.refreshToken },
					{
						headers: {
							'Content-Type': 'application/json',
							'User-Agent': 'ChatFlow-Frontend',
						},
					}
				);

				const newData = res.data.data;

				// обновляем store
				setAuthData({
					...auth,
					accessToken: newData.accessToken,
					refreshToken: newData.refreshToken,
					accessExpiresAt: newData['access-expires-at'],
				});

				// повторяем запрос с новым accessToken
				originalRequest.headers.Authorization = `Bearer ${newData.accessToken}`;
				return api(originalRequest);
			} catch (refreshError) {
				clearAuthData();
				return Promise.reject(refreshError);
			}
		}

		return Promise.reject(error);
	}
);
