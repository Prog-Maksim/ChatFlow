import { createEvent, createStore } from 'effector';

// Для бургер-меню
export const sidebarToggled = createEvent();
export const sidebarClosed = createEvent();

export const $isSidebarOpen = createStore(false)
	.on(sidebarToggled, (state) => !state)
	.on(sidebarClosed, () => false);

// Для иконки поиска
export const searchSidebarToggled = createEvent();
export const searchSidebarClosed = createEvent();

export const $isSearchSidebarOpen = createStore(false)
	.on(searchSidebarToggled, (state) => !state)
	.on(searchSidebarClosed, () => false);

	
// Для поиска
export const queryChanged = createEvent<string>();
export const filterChanged = createEvent<'people' | 'groups' | 'channels'>();

export const $query = createStore('').on(queryChanged, (_, q) => q);
export const $activeFilter = createStore<'people' | 'groups' | 'channels'>('people')
	.on(filterChanged, (_, f) => f);

export const $results = createStore({
	people: ['Матвей Гончаров'],
	groups: [] as string[],
	channels: [] as string[],
});