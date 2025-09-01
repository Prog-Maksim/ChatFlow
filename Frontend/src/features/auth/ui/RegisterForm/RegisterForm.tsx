import { useForm, Controller, type SubmitHandler } from 'react-hook-form';
import { IMaskInput } from 'react-imask';
import { Button } from '../../../../shared/ui/Button/Button';
import { useRegister } from '../../model/useRegister';
import type { IRegistrationInput } from '../../types/register.types';
import { Link, useNavigate } from '@tanstack/react-router';
import { ROUTES } from '../../../../shared/config/routes';
import { useState } from 'react';

export function RegisterForm() {
	const {
		register,
		handleSubmit,
		watch,
		control,
		formState: { errors },
		reset,
	} = useForm<IRegistrationInput>();

	const password = watch('password');
	const { mutate, isPending, error } = useRegister();
	const [isSuccess, setIsSuccess] = useState(false);
	const navigate = useNavigate();

	const onSubmit: SubmitHandler<IRegistrationInput> = ({ ...rest }) => {
		let formattedLogin = rest.login;
		if (formattedLogin.startsWith('9')) {
			formattedLogin = '8' + formattedLogin;
		}

		mutate(
			{ ...rest, login: formattedLogin },
			{
				onSuccess: () => {
					reset();
					setIsSuccess(true);
					setTimeout(() => navigate({ to: ROUTES.login }), 1500);
				},
			}
		);
	};

	return (
		<form
			onSubmit={handleSubmit(onSubmit)}
			className={`flex flex-col justify-center gap-5 w-[400px] rounded-2xl shadow-[0_0_19.7px_0_rgba(0,0,0,0.25)] px-7 py-5 ${
				isPending ? 'opacity-70 pointer-events-none' : ''
			}`}
		>
			<p className="text-2xl font-extrabold text-center">Регистрация</p>

			<div className="mt-4">
				<input
					{...register('surname', { required: 'Фамилия обязательна' })}
					placeholder="Фамилия"
					className="input_border"
					autoComplete="family-name"
					disabled={isPending}
				/>
				{errors.surname && (
					<span className="text-red-500 text-sm">{errors.surname.message}</span>
				)}
			</div>

			<div className="mt-0.5">
				<input
					{...register('name', { required: 'Имя обязательно' })}
					placeholder="Имя"
					className="input_border"
					autoComplete="given-name"
					disabled={isPending}
				/>
				{errors.name && (
					<span className="text-red-500 text-sm">{errors.name.message}</span>
				)}
			</div>

			<div className="mt-0.5">
				<Controller
					name="login"
					control={control}
					rules={{
						required: 'Телефон обязателен',
						pattern: {
							value: /^\d{10}$/,
							message: 'Неверный формат телефона',
						},
					}}
					defaultValue=""
					render={({ field: { onChange, onBlur, value, ref } }) => (
						<IMaskInput
							mask="+7 (000) 000-00-00"
							unmask="typed"
							value={value}
							onAccept={onChange}
							onBlur={onBlur}
							inputRef={ref}
							placeholder="+7 (900) 000-00-00"
							className="input_border"
							autoComplete="tel"
							disabled={isPending}
						/>
					)}
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
						minLength: { value: 10, message: 'Минимум 10 символов' },
					})}
					placeholder="Пароль"
					className="input_border"
					autoComplete="new-password"
					disabled={isPending}
				/>
				{errors.password && (
					<span className="text-red-500 text-sm">{errors.password.message}</span>
				)}
			</div>

			<div className="mt-0.5">
				<input
					type="password"
					{...register('confirmPassword', {
						required: 'Повтор пароля обязателен',
						validate: (value) => value === password || 'Пароли не совпадают',
					})}
					placeholder="Повтор пароля"
					className="input_border"
					autoComplete="new-password"
					disabled={isPending}
				/>
				{errors.confirmPassword && (
					<span className="text-red-500 text-sm">{errors.confirmPassword.message}</span>
				)}
			</div>

			<div className="flex items-center justify-center mt-4">
				<Button variant="primary" disabled={isPending}>
					{isPending ? 'Загрузка...' : 'Зарегистрироваться'}
				</Button>
			</div>

			{isSuccess && (
				<p className="font-semibold text-sm text-center">
					Регистрация успешна!
				</p>
			)}

			{error instanceof Error && (
				<p className="text-red-500 text-sm text-center">❌ {error.message}</p>
			)}
			<div className="flex justify-between text-sm text-blue-600 mt-1">
				<Link to={ROUTES.login} className="hover:underline">
								Есть аккаунт?
				</Link>
				<span className="cursor-pointer hover:underline">Вход по QR-коду</span>
			</div>
		</form>
		
	);
}
