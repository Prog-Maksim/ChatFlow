import { putItem, getItem, removeItem } from './secureStore';

const REFRESH_KEY = 'refreshToken';

export async function saveRefreshToken(token: string) {
	if (!token) return;
	await putItem(REFRESH_KEY, token);
	console.log('✅ refreshToken сохранён в IndexedDB:', token);
}

export async function loadRefreshToken(): Promise<string | null> {
	const token = await getItem<string>(REFRESH_KEY);
	console.log('📦 refreshToken из IndexedDB:', token);
	return token ?? null;
}

export async function clearRefreshToken() {
	await removeItem(REFRESH_KEY);
	console.log('🗑 refreshToken удалён из IndexedDB');
}
