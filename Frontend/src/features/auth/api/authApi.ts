import axios from 'axios';
import type { AuthData } from '../model/types';

const BASE_URL = 'https://api.chatflowonline.ru/v1';

export async function refreshTokenApi(refreshToken: string): Promise<AuthData> {
	const { data } = await axios.post(
		`${BASE_URL}/token/refresh`,
		{},
		{
			headers: {
				'Content-Type': 'application/json',
				Authorization: `Bearer ${refreshToken}`, 
			},
		}
	);

	return data.data;
}
