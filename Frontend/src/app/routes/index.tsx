import { createFileRoute } from '@tanstack/react-router';
import { MainPage } from '../../pages/main/MainPage';
import { requireAuth } from '../../features/auth/model/authGuard';

export const Route = createFileRoute('/')({
	beforeLoad: requireAuth('/login'),
	component: () => <MainPage />,
});
