import { createStore, createEvent } from 'effector';
import type { AuthData } from './types';

export const setAuthData = createEvent<AuthData>();
export const clearAuthData = createEvent();

export const $auth = createStore<AuthData | null>(null)
	.on(setAuthData, (_, data) => data)
	.reset(clearAuthData);
