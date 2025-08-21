import { attachLogger } from 'effector-logger';
import { StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider, createRouter } from '@tanstack/react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { routeTree } from '../../routeTree.gen';
import '../styles/index.css';
import { loadRefreshToken } from '../../shared/storage/tokenStore';
import { patchAuthData } from '../../features/auth/model/auth';


const router = createRouter({ routeTree });
const queryClient = new QueryClient();

declare module '@tanstack/react-router' {
	interface Register {
		router: typeof router;
	}
}

attachLogger();


async function initApp() {
	const refreshToken = await loadRefreshToken();
	if (refreshToken) {
		patchAuthData({ refreshToken });
	}

	const rootElement = document.getElementById('root')!;
	if (!rootElement.innerHTML) {
		const root = ReactDOM.createRoot(rootElement);
		root.render(
			<StrictMode>
				<QueryClientProvider client={queryClient}>
					<RouterProvider router={router} />
				</QueryClientProvider>
			</StrictMode>,
		);
	}
}

initApp();