import { useMutation } from '@tanstack/react-query';
import { registerUser, type RegistrationDto } from '../api/registerApi';
import { createAndStoreKeys } from '../../../shared/crypto/createAndStoreKeys';

export const useRegister = () => {
	return useMutation({
		mutationFn: async (variables: RegistrationDto) => {
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
