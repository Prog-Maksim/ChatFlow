import { Logo } from '../../shared/assets';
import { LoginForm } from '../../features/auth/ui/LoginForm/LoginForm';

export function LoginPage() {
	return (
		<div className='h-[100vh] w-[100vw]'>
			<header>
				<Logo />
			</header>
			<main className='flex justify-center'>
				<LoginForm />
			</main>
			<footer></footer>
		</div>
	);
}
