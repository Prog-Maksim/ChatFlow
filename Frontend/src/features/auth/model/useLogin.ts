import { useMutation } from '@tanstack/react-query';
import { loginUser } from '../api/loginApi';
import type { LoginRequest, LoginResponse } from '../types/login.types';

export const useLogin = () => {
	return useMutation<LoginResponse, Error, LoginRequest>({
		mutationFn: loginUser, 
	});
};
