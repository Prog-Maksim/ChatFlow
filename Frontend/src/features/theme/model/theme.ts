import { createEvent, createStore } from 'effector';

export type Theme = 'light' | 'dark';

export const themeToggled = createEvent();
export const themeSet = createEvent<Theme>();

export const $theme = createStore<Theme>('light')
	.on(themeToggled, (state) => (state === 'light' ? 'dark' : 'light'))
	.on(themeSet, (_, theme) => theme);
