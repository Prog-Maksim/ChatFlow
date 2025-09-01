import { sidebarToggled } from '../../../features/sidebar/model/sidebar';
import { SearchPanel } from '../../../features/sidebar/ui/SearchPanel';
import { SearchToggle } from '../../../features/sidebar/ui/SearchToggle';
import { SidebarMenu } from '../../../features/sidebar/ui/SidebarMenu';
import { BurgerMenu, Logo } from '../../../shared/assets';
import { useUnit } from 'effector-react';

export function Sidebar() {
	const toggle = useUnit(sidebarToggled);

	return (
		<div className="bg-[#E9EEF5] h-[100vh] relative">
			<div className="flex justify-between items-center p-3">
				<BurgerMenu className="cursor-pointer" onClick={toggle} />
				<Logo className="w-[153px] h-[37px] mx-2" />
				<SearchToggle />
			</div>

			<SidebarMenu />
			<SearchPanel />
		</div>
	);
}
