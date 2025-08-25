import { BurgerMenu, Logo, SearchIcon } from '../../../shared/assets';
import { useUnit } from 'effector-react';
import { sidebarToggled } from '../model/sidebar';
import { SidebarMenu } from './SidebarMenu';

export function Sidebar() {
	const toggle = useUnit(sidebarToggled);

	return (
		<div className="bg-[#E9EEF5] h-[100vh] ">
			<div className="flex justify-between items-center p-3">
				<BurgerMenu className="cursor-pointer" onClick={toggle} />
				<Logo className="w-[153px] h-[37px] mx-2" />
				<SearchIcon className="size-[25px] cursor-pointer" />
			</div>

			<SidebarMenu />
		</div>
	);
}
