import { RegisterForm } from '../../features/auth/ui/RegisterForm/RegisterForm';

export default function RegisterPage() {
	return (
		<div className='h-[100vh] w-[100vw]  text-5xl flex justify-center text-center flex-col gap-20'>
			<RegisterForm/>
		</div>
	);
}
