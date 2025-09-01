import { motion } from 'framer-motion';
import { useUnit } from 'effector-react';
import { $theme, themeToggled } from '../../../features/theme/model/theme';

export function Switch() {
	const [theme, toggle] = useUnit([$theme, themeToggled]);
	const isOn = theme === 'dark';

	return (
		<div
			onClick={toggle}
			className={`relative w-11 h-6 flex items-center rounded-full p-1 cursor-pointer transition-colors
        ${isOn ? 'bg-blue-500' : 'bg-gray-300'}`}
		>
			<motion.div
				layout
				transition={{ type: 'spring', stiffness: 500, damping: 30 }}
				whileTap={{ scale: 0.9 }}  
				className="w-4 h-4 bg-white rounded-full shadow-md"
				animate={{
					x: isOn ? 20 : 0,        
				}}
			/>
		</div>
	);
}
