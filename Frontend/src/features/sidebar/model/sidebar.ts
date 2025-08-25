import { createEvent, createStore } from 'effector';

export const sidebarToggled = createEvent();
export const sidebarClosed = createEvent();

export const $isSidebarOpen = createStore(false)
	.on(sidebarToggled, (state) => !state)
	.on(sidebarClosed, () => false);
