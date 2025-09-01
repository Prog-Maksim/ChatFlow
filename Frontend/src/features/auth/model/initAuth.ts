import { clearRefreshToken, loadRefreshToken, saveRefreshToken } from '../../../shared/storage/tokenStore';
import { refreshTokenApi } from '../api/authApi';
import { clearAuthData, setAuthData } from './auth';


export async function initAuth() {
	const refresh = await loadRefreshToken();

	if (!refresh) {
		clearAuthData();
		return;
	}

	try {
		const data = await refreshTokenApi(refresh);

		setAuthData({
			personId: data.personId,
			deviceId: data.deviceId,
			accessToken: data.accessToken,
			refreshToken: data.refreshToken,
			accessExpiresAt: data.accessExpiresAt,
		});

		await saveRefreshToken(data.refreshToken);
	} catch (err) {
		clearAuthData();
		await clearRefreshToken();
	}
}
