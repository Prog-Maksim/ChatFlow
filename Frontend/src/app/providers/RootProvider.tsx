import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from './QueryClient';


export function RootProvider({ children }: { children: React.ReactNode }) {
	return (
		<QueryClientProvider client={queryClient}>
			{children}
		</QueryClientProvider>
	);
}
