import { motion, AnimatePresence } from 'framer-motion';
import { useUnit } from 'effector-react';
import { $isSidebarOpen, sidebarClosed } from '../model/sidebar';
import { Link } from '@tanstack/react-router';
import { ROUTES } from '../../../shared/config/routes';
import { Switch } from '../../../shared/ui/Switch/Switch';

export function SidebarMenu() {
	const [isOpen, close] = useUnit([$isSidebarOpen, sidebarClosed]);

	return (
		<AnimatePresence>
			{isOpen && (
				<>
					{/* Overlay */}
					<motion.div
						className="fixed inset-0 bg-black/40 z-40"
						initial={{ opacity: 0 }}
						animate={{ opacity: 1 }}
						exit={{ opacity: 0 }}
						onClick={close}
					/>

					{/* Sidebar menu */}
					<motion.aside
						className="fixed top-0 left-0 h-full w-[280px] bg-white shadow-lg z-50 flex flex-col"
						initial={{ x: -300 }}
						animate={{ x: 0 }}
						exit={{ x: -300 }}
						transition={{ type: 'tween', duration: 0.3 }}
					>
						<div className="p-4 border-b">
							<div className="w-12 h-12 bg-blue-500 rounded-full mb-2" />
							<p className="font-semibold">Матвей Гончаров</p>
						</div>

						<nav className="flex flex-col gap-4 p-4">
							<Link to={ROUTES.profile} onClick={close} className="flex items-center gap-2">
                Профиль
							</Link>
							<Link to={ROUTES.group_create} onClick={close} className="flex items-center gap-2">
                Создать группу
							</Link>
							<Link to={ROUTES.calls} onClick={close} className="flex items-center gap-2">
                Звонки
							</Link>
							<Link to={ROUTES.favorites} onClick={close} className="flex items-center gap-2">
                Избранное
							</Link>
							<Link to={ROUTES.settings} onClick={close} className="flex items-center gap-2">
                Настройки
							</Link>
						</nav>

						<div className="mt-auto p-4 border-t flex items-center justify-between">
							<span>Ночной режим</span>
							<Switch />
						</div>

					</motion.aside>
				</>
			)}
		</AnimatePresence>
	);
}
