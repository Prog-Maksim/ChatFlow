// import { Provider } from 'mobx-react';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from './QueryClient';
// import { authStore } from '../../features/auth/model/auth.store';

export function RootProvider({ children }: { children: React.ReactNode }) {
	return (
	// <Provider authStore={authStore}>
		<QueryClientProvider client={queryClient}>
			{children}
		</QueryClientProvider>
	// </Provider>
	);
}
