import { Link } from '@tanstack/react-router';
import { useForm, Controller, type SubmitHandler } from 'react-hook-form';
import { IMaskInput } from 'react-imask';
import { Button } from '../../../../shared/ui/Button/Button';

interface ILoginInput {
	login: string;
	phone: string;
}

export function LoginForm() {
	const {
		register,
		handleSubmit,
		control,
		formState: { errors },
	} = useForm<ILoginInput>();

	const onSubmit: SubmitHandler<ILoginInput> = (data) => {
		if (data.phone.startsWith('9')) data.phone = '8' + data.phone.slice(0);
		console.log('Вход:', { ...data });
	};

	return (
		<form onSubmit={handleSubmit(onSubmit)} className="flex flex-col justify-center gap-5 w-[400px] rounded-2xl shadow-[0_0_19.7px_0_rgba(0,0,0,0.25)] px-7 py-5">
			<p className='text-2xl font-extrabold text-center'>Авторизация</p>
			<div className='mt-3'>
				<input
					{...register('login', { required: 'Логин обязателен' })}
					placeholder="Логин"
					className='input_border'
				/>
				{errors.login && (
					<span className="text-red-500 text-sm">{errors.login.message}</span>
				)}
			</div>

			<div className='mt-0.5'>
				<Controller
					name="phone"
					control={control}
					defaultValue=""
					rules={{
						required: 'Телефон обязателен',
						pattern: {
							value: /^\d{11}$/,
							message: 'Введите корректный номер из 11 цифр',
						},
					}}
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

			<div className='flex items-center justify-center mt-4'>
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
