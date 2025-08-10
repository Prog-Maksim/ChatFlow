import axios from 'axios';

export const api = axios.create({
	baseURL: 'https://api.chatflowonline.ru/v1',
	headers: {
		'Content-Type': 'application/json',
	},
});
