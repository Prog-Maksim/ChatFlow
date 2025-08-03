import { Link } from '@tanstack/react-router';
import { useForm, Controller, type SubmitHandler } from 'react-hook-form';
import { IMaskInput } from 'react-imask';
import { Button } from '../../../../shared/ui/Button/Button';

interface IFormInput {
	lastName: string;
	firstName: string;
	phone: string;
	password: string;
	confirmPassword: string;
}

export function RegisterForm() {
	const {
		register,
		handleSubmit,
		watch,
		control,
		formState: { errors },
	} = useForm<IFormInput>();

	const password = watch('password');

	const onSubmit: SubmitHandler<IFormInput> = (data) => {
		if (data.phone.startsWith('9')) data.phone = '8' + data.phone.slice(0);
		console.log('Регистрация:', data);
	};

	return (
		<form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4 max-w-md">
			<div>
				<label>Фамилия</label>
				<input
					{...register('lastName', { required: 'Фамилия обязательна' })}
					className="border px-2 py-1 rounded w-full"
				/>
				{errors.lastName && (
					<span className="text-red-500 text-sm">{errors.lastName.message}</span>
				)}
			</div>

			<div>
				<label>Имя</label>
				<input
					{...register('firstName', { required: 'Имя обязательно' })}
					className="border px-2 py-1 rounded w-full"
				/>
				{errors.firstName && (
					<span className="text-red-500 text-sm">{errors.firstName.message}</span>
				)}
			</div>

			<div>
				<label>Телефон</label>
				<Controller
					name="phone"
					control={control}
					rules={{
						required: 'Телефон обязателен',
						pattern: {
							value: /^\d{10}$/,
							message: 'Проверьте вводимый телефон',
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
							placeholder="+7 (___) ___-__-__"
							className="border px-2 py-1 rounded w-full"
						/>
					)}
				/>
				{errors.phone && (
					<span className="text-red-500 text-sm">{errors.phone.message}</span>
				)}
			</div>

			<div>
				<label>Пароль</label>
				<input
					type="password"
					{...register('password', {
						required: 'Пароль обязателен',
						minLength: { value: 6, message: 'Минимум 6 символов' },
					})}
					className="border px-2 py-1 rounded w-full"
				/>
				{errors.password && (
					<span className="text-red-500 text-sm">{errors.password.message}</span>
				)}
			</div>

			<div>
				<label>Повтор пароля</label>
				<input
					type="password"
					{...register('confirmPassword', {
						required: 'Повтор пароля обязателен',
						validate: (value) => value === password || 'Пароли не совпадают',
					})}
					className="border px-2 py-1 rounded w-full"
				/>
				{errors.confirmPassword && (
					<span className="text-red-500 text-sm">{errors.confirmPassword.message}</span>
				)}
			</div>

			<Button variant="primary">Зарегистрироваться</Button>

			<div className="flex justify-between text-sm text-blue-600">
				<Link to="/login" className="hover:underline">
					Есть аккаунт?
				</Link>
				<p>Вход по Qr-code</p>
			</div>
		</form>
	);
}
