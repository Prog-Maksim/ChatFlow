import { makeAutoObservable } from 'mobx';

class AuthStore {
	isAuthenticated = false;
	user: string | null = null;

	constructor() {
		makeAutoObservable(this);
	}

	login(user: string) {
		this.user = user;
		this.isAuthenticated = true;
	}

	logout() {
		this.user = null;
		this.isAuthenticated = false;
	}
}

export const authStore = new AuthStore();
