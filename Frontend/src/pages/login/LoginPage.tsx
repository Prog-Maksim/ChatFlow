import { Logo } from '../../shared/assets';
import { LoginForm } from '../../features/auth/ui/LoginForm/LoginForm';

export function LoginPage() {
	return (
		<div className="min-h-screen w-full flex flex-col">
			<header className="pt-5 pl-5">
				<Logo />
			</header>

			<main className="flex-1 flex justify-center items-center">
				<LoginForm />
			</main>

			<footer />
		</div>
	);
}
