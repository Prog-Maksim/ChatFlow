import { useMutation } from '@tanstack/react-query';
import { registerUser  } from '../api/registerApi';
import { createAndStoreKeys } from '../../../shared/crypto/createAndStoreKeys';
import type { RegistrationRequest } from '../types/register.types';

export const useRegister = () => {
	return useMutation({
		mutationFn: async (variables: RegistrationRequest) => {
			const response = await registerUser({
				login: variables.login,
				password: variables.password,
				surname: variables.surname,
				name: variables.name,
			});

			await createAndStoreKeys(variables.login, variables.password);

			return response;
		},
	});
};
