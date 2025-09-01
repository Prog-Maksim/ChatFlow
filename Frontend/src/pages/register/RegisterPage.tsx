import { RegisterForm } from '../../features/auth/ui/RegisterForm/RegisterForm';
import { Logo } from '../../shared/assets';

export default function RegisterPage() {
	return (
		<div className="min-h-screen w-full flex flex-col">
			<header className="pt-5 pl-5">
				<Logo />
			</header>

			<main className="flex-1 flex justify-center items-center">
				<RegisterForm />
			</main>

			<footer />
		</div>
	);
}
