import { BurgerMenu, Logo, SearchIcon } from '../../../shared/assets';

export function Sidebar () {
	return (
		<div className='bg-[#E9EEF5] h-[100vh] w-[100wv]'>
			<div className="flex justify-between items-center p-3">
				<BurgerMenu className="cursor-pointer" />
				<Logo className="w-[153px] h-[37px]" />
				<SearchIcon className="size-[25px] cursor-pointer" />
			</div>

		</div>
	);
}