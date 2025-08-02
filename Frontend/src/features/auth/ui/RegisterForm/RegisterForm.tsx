import { useForm, type SubmitHandler } from 'react-hook-form';
interface IFormInput {
	lastName: string
	firstName: string
	password: string
}

export function RegisterForm() {
	const { register, handleSubmit } = useForm<IFormInput>();
	const onSubmit: SubmitHandler<IFormInput> = (data) => console.log(data);


	return (
		<form onSubmit={handleSubmit(onSubmit)}>
			<label>Фамилия</label>
			<input {...register('lastName')} />
			<label>Имя</label>
			<input {...register('firstName')} />
			<label>Пароль</label>
			<input {...register('password')}/>

			<button type="submit" className='cursor-pointer'> Зарегистрироваться</button>
		</form>
	);
}