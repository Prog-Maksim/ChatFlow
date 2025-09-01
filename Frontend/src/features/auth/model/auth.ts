import { createStore, createEvent } from 'effector';
import type { AuthData } from './types';

export const setAuthData = createEvent<AuthData>();
export const patchAuthData = createEvent<Partial<AuthData>>();
export const clearAuthData = createEvent();

export const $auth = createStore<AuthData | null>(null)
	.on(setAuthData, (_, payload) => payload)
	.on(patchAuthData, (state, patch) => ({ ...state!, ...patch }))
	.reset(clearAuthData);
