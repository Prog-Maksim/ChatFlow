import { redirect } from '@tanstack/react-router';
import { $auth } from './auth';

export const requireAuth = (redirectTo = '/login') => {
	return () => {
		const auth = $auth.getState();
		if (!auth?.accessToken) {
			throw redirect({ to: redirectTo });
		}
	};
};
