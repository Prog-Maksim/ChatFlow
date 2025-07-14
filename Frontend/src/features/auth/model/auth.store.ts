// import { makeAutoObservable } from 'mobx';
// import { loginApi } from './api';

// class AuthStore {
//   user = null as null | { id: string; name: string };
//   isAuth = false;

//   constructor() { makeAutoObservable(this); }

//   async login(email: string, password: string) {
//     const res = await loginApi({ email, password });
//     this.user = res.user; this.isAuth = true;
//   }

//   logout() {
//     this.user = null; this.isAuth = false;
//   }
// }

// export const authStore = new AuthStore();
