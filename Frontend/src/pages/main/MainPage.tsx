import { Link } from '@tanstack/react-router';

export function MainPage() {
	return (
		<div className='h-[100vh] w-[100vw] bg-[black] text-[white] text-5xl flex justify-center text-center flex-col gap-20'>
			<Link to='/login'>login</Link>
			<Link to='/registration'>registration</Link>
		</div>
	);
}
