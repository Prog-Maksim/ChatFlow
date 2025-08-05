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
		<form
			onSubmit={handleSubmit(onSubmit)}
			className="flex flex-col justify-center gap-5 w-[400px] rounded-2xl shadow-[0_0_19.7px_0_rgba(0,0,0,0.25)] px-7 py-5"
		>
			<p className="text-2xl font-extrabold text-center">Регистрация</p>

			<div className='mt-4'>
				<input
					{...register('lastName', { required: 'Фамилия обязательна' })}
					placeholder="Фамилия"
					className='input_border'
				/>
				{errors.lastName && (
					<span className="text-red-500 text-sm">{errors.lastName.message}</span>
				)}
			</div>

			<div className='mt-0.5'>
				<input
					{...register('firstName', { required: 'Имя обязательно' })}
					placeholder="Имя"
					className='input_border'
				/>
				{errors.firstName && (
					<span className="text-red-500 text-sm">{errors.firstName.message}</span>
				)}
			</div>

			<div className='mt-0.5'>
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
							placeholder="+7 (900) 000-00-00"
							className='input_border'
						/>
					)}
				/>
				{errors.phone && (
					<span className="text-red-500 text-sm">{errors.phone.message}</span>
				)}
			</div>

			<div className='mt-0.5'>
				<input
					type="password"
					{...register('password', {
						required: 'Пароль обязателен',
						minLength: { value: 6, message: 'Минимум 6 символов' },
					})}
					placeholder="Пароль"
					className='input_border'
				/>
				{errors.password && (
					<span className="text-red-500 text-sm">{errors.password.message}</span>
				)}
			</div>

			<div className='mt-0.5'>
				<input
					type="password"
					{...register('confirmPassword', {
						required: 'Повтор пароля обязателен',
						validate: (value) => value === password || 'Пароли не совпадают',
					})}
					placeholder="Повтор пароля"
					className='input_border'
				/>
				{errors.confirmPassword && (
					<span className="text-red-500 text-sm">{errors.confirmPassword.message}</span>
				)}
			</div>

			<div className="flex items-center justify-center mt-4">
				<Button variant="primary">Зарегистрироваться</Button>
			</div>

			<div className="flex justify-between text-sm text-blue-600 mt-1">
				<Link to="/login" className="hover:underline">
					Есть аккаунт?
				</Link>
				<span className="cursor-pointer hover:underline">Вход по QR-коду</span>
			</div>
		</form>
	);
}
