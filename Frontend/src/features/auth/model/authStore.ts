import { makeAutoObservable } from 'mobx';

class AuthStore {
	isAuthenticated = false;
	user: string | null = null;
	token: string | null = null;

	privateKey: CryptoKey | null = null;

	constructor() { makeAutoObservable(this); }

	setToken(t: string) { this.token = t; }
	login(user: string) { this.user = user; this.isAuthenticated = true; }
	logout() { this.user = null; this.isAuthenticated = false; this.token = null; this.privateKey = null; }
	setPrivateKey(k: CryptoKey) { this.privateKey = k; }
}

export const authStore = new AuthStore();
