import { createFileRoute } from '@tanstack/react-router';
import { requireAuth } from '../../features/auth/model/authGuard';

export const Route = createFileRoute('/protected')({
	beforeLoad: requireAuth('/login'),
	component: () => <div>🔒 Доступ разрешён</div>,
});
