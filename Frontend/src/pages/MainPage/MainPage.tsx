
import { useLoaderData } from '@tanstack/react-router';

export function MainPage() {
	const data = useLoaderData({ from: '/main' }); 

	return (
		<div>
			<h2>Main Page</h2>
			<p>Data: {data.message}</p>
		</div>
	);
}
