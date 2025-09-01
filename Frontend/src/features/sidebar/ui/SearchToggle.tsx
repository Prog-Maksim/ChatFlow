import { motion, AnimatePresence } from 'framer-motion';
import { useUnit } from 'effector-react';
import { CloseIcon, SearchIcon } from '../../../shared/assets';
import { $isSearchSidebarOpen, searchSidebarToggled } from '../model/sidebar';

export function SearchToggle() {
	const [isOpen, toggle] = useUnit([$isSearchSidebarOpen, searchSidebarToggled]);

	return (
		<div className="cursor-pointer size-[20px]" onClick={toggle}>
			<AnimatePresence mode="wait" initial={false}>
				{isOpen ? (
					<motion.div
						key="close"
						initial={{ rotate: -90, opacity: 0 }}
						animate={{ rotate: 0, opacity: 1 }}
						exit={{ rotate: 90, opacity: 0 }}
						transition={{ duration: 0.2 }}
					>
						<CloseIcon className="size-[20px]" />
					</motion.div>
				) : (
					<motion.div
						key="search"
						initial={{ rotate: 90, opacity: 0 }}
						animate={{ rotate: 0, opacity: 1 }}
						exit={{ rotate: -90, opacity: 0 }}
						transition={{ duration: 0.2 }}
					>
						<SearchIcon className="h-[20px] w-[25px]" />
					</motion.div>
				)}
			</AnimatePresence>
		</div>
	);
}
