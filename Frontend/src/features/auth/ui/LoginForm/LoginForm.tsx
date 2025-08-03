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
		<form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4 max-w-md">
			<div>
				<label>Логин</label>
				<input
					{...register('login', { required: 'Логин обязателен' })}
					className="border px-2 py-1 rounded w-full"
				/>
				{errors.login && (
					<span className="text-red-500 text-sm">{errors.login.message}</span>
				)}
			</div>

			<div>
				<label>Телефон</label>
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
							placeholder="+7 (___) ___-__-__"
							className="border px-2 py-1 rounded w-full"
						/>
					)}
				/>
				{errors.phone && (
					<span className="text-red-500 text-sm">{errors.phone.message}</span>
				)}
			</div>

			<Button variant="primary">Войти</Button>

			<div className="flex justify-between text-sm text-blue-600">
				<Link to="/registration" className="hover:underline">
					Нет аккаунта?
				</Link>
				<span className="cursor-pointer hover:underline">Вход по QR-коду</span>
			</div>
		</form>
	);
}
