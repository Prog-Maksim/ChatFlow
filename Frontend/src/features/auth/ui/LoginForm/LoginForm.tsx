import { Link } from '@tanstack/react-router';
import { useForm, type SubmitHandler } from 'react-hook-form';
import { Button } from '../../../../shared/ui/Button/Button';
import type { ILoginInput } from '../../types/login.types';
import { useLoginMutation } from '../../model/useLoginMutation';
import { getItem } from '../../../../shared/storage/secureStore';

export function LoginForm() {
	const {
		register,
		handleSubmit,
		formState: { errors },
	} = useForm<ILoginInput>();

	const loginMutation = useLoginMutation();

	const onSubmit: SubmitHandler<ILoginInput> = async (data) => {
		try {
			const publicKey = await getItem(`publicKey:${data.login}`);


			if (!publicKey || typeof publicKey !== 'string') {
				console.error('Public key not found in IndexedDB');
				return;
			}

			loginMutation.mutate({
				...data,
				publicKey,
			});
		} catch (err) {
			console.error('Failed to get publicKey from IndexedDB', err);
		}
	};

	return (
		<form
			onSubmit={handleSubmit(onSubmit)}
			className="flex flex-col justify-center gap-5 w-[400px] rounded-2xl shadow-[0_0_19.7px_0_rgba(0,0,0,0.25)] px-7 py-5"
		>
			<p className="text-2xl font-extrabold text-center">Авторизация</p>

			<div className="mt-3">
				<input
					{...register('login', { required: 'Логин обязателен' })}
					placeholder="Логин"
					className="input_border"
				/>
				{errors.login && (
					<span className="text-red-500 text-sm">{errors.login.message}</span>
				)}
			</div>

			<div className="mt-0.5">
				<input
					{...register('password', {
						required: 'Пароль обязателен',
						minLength: { value: 6, message: 'Минимум 6 символов' },
					})}
					type="password"
					placeholder="Пароль"
					className="input_border"
				/>
				{errors.password && (
					<span className="text-red-500 text-sm">{errors.password.message}</span>
				)}
			</div>

			<div className="flex items-center justify-center mt-4">
				<Button variant="primary">Войти</Button>
			</div>

			<div className="flex justify-between text-sm text-blue-600 mt-1">
				<Link to="/registration" className="hover:underline">
					Нет аккаунта?
				</Link>
				<span className="cursor-pointer hover:underline">Вход по QR-коду</span>
			</div>
		</form>
	);
}
