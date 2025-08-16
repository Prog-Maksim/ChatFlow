import { Link } from '@tanstack/react-router';
import { useForm, type SubmitHandler } from 'react-hook-form';
import { Button } from '../../../../shared/ui/Button/Button';
import type { ILoginInput } from '../../types/login.types';
import { useLoginMutation } from '../../model/useLoginMutation';
import { ROUTES } from '../../../../shared/config/routes';

export function LoginForm() {
	const {
		register,
		handleSubmit,
		formState: { errors },
		reset,
	} = useForm<ILoginInput>();

	const { mutate, isPending, isSuccess, error } = useLoginMutation();

	const onSubmit: SubmitHandler<ILoginInput> = (formData) => {
		mutate(formData, {
			onSuccess: () => reset(),
		});
	};

	return (
		<form
			onSubmit={handleSubmit(onSubmit)}
			className={`flex flex-col justify-center gap-5 w-[400px] rounded-2xl shadow-[0_0_19.7px_0_rgba(0,0,0,0.25)] px-7 py-5 ${
				isPending ? 'opacity-70 pointer-events-none' : ''
			}`}
		>
			<p className="text-2xl font-extrabold text-center">Авторизация</p>

			<div className="mt-3">
				<input
					{...register('login', { required: 'Логин обязателен' })}
					placeholder="Логин"
					className="input_border"
					autoComplete="username"
					disabled={isPending}
				/>
				{errors.login && (
					<span className="text-red-500 text-sm">{errors.login.message}</span>
				)}
			</div>

			<div className="mt-0.5">
				<input
					type="password"
					{...register('password', {
						required: 'Пароль обязателен',
						minLength: { value: 6, message: 'Минимум 6 символов' },
					})}
					placeholder="Пароль"
					className="input_border"
					autoComplete="current-password"
					disabled={isPending}
				/>
				{errors.password && (
					<span className="text-red-500 text-sm">{errors.password.message}</span>
				)}
			</div>

			<div className="flex items-center justify-center mt-4">
				<Button variant="primary" disabled={isPending}>
					{isPending ? 'Загрузка...' : 'Войти'}
				</Button>
			</div>

			{isSuccess && (
				<p className="font-semibold text-sm text-center">
					Успешный вход!
				</p>
			)}

			{error instanceof Error && (
				<p className="text-red-500 text-sm text-center">
					❌ {error.message}
				</p>
			)}

			<div className="flex justify-between text-sm text-blue-600 mt-1">
				<Link to={ROUTES.register} className="hover:underline">
					Нет аккаунта?
				</Link>
				<span className="cursor-pointer hover:underline">Вход по QR-коду</span>
			</div>
		</form>
	);
}
