import { createFileRoute } from '@tanstack/react-router';
import RegisterPage from '../../pages/register/RegisterPage';

export const Route = createFileRoute('/registration')({
	component: () => <RegisterPage />,
});
