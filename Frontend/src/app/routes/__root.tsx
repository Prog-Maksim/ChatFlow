import { Outlet, createRootRoute } from '@tanstack/react-router';
import { initAuth } from '../../features/auth/model/initAuth';

export const Route = createRootRoute({
	beforeLoad: async () => {
		await initAuth();
	},
	component: () => <Outlet />,
});
