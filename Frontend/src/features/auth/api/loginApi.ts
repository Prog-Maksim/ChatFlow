
import { api } from '../../../shared/api/baseQuery';
import type { ILoginInput } from '../types/login.types';

export const loginApi = (data: ILoginInput) => {
	return api.post('/auth/authorization', data); 
};
