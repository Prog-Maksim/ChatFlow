import { motion, AnimatePresence } from 'framer-motion';
import { useUnit } from 'effector-react';
import { $isSearchSidebarOpen, $query, $results, queryChanged } from '../model/sidebar';
import { ChannelCard, GroupCard, UserCard } from '../../../entities';

export function SearchPanel() {
	const [isOpen, query, results, setQuery] = useUnit([
		$isSearchSidebarOpen,
		$query,
		$results,
		queryChanged,
	]);

	const filters = [
		{ key: 'people', label: 'Люди' },
		{ key: 'groups', label: 'Группы' },
		{ key: 'channels', label: 'Каналы' },
	] as const;

	return (
		<AnimatePresence>
			{isOpen && (
				<motion.div
					initial={{ y: -30, opacity: 0 }}
					animate={{ y: 0, opacity: 1 }}
					exit={{ y: -30, opacity: 0 }}
					transition={{ duration: 0.25 }}
					className="absolute top-12 left-0 right-0 bg-[#E9EEF5] rounded-lg shadow-lg"
				>
					<input
						type="text"
						placeholder="Поиск..."
						value={query}
						onChange={(e) => setQuery(e.target.value)}
						className="w-full p-2 mt-3 rounded-lg border bg-[white] border-gray-300 outline-none h-[30px]"
					/>


					<div className="flex gap-2 mt-4 justify-center items-center">
						{filters.map((f) => {
							const disabled = results[f.key].length === 0;
							return (
								<button
									key={f.key}
									disabled={disabled}
									className={`px-3 py-1 rounded-full text-sm font-medium transition
                    ${disabled ? 'bg-gray-200 text-gray-400 cursor-not-allowed' : 'bg-white text-black hover:bg-gray-100'}
                  `}
								>
									{f.label}
								</button>
							);
						})}
					</div>

					<div className="mt-4 space-y-2">
						{results.people.map((name) => (
							<UserCard key={name} avatar="/mock.jpg" name={name} username="matvey_352" />
						))}
						{results.groups.map((g) => (
							<GroupCard key={g} title={g} membersCount={42} />
						))}
						{results.channels.map((c) => (
							<ChannelCard key={c} title={c} description="Описание канала" />
						))}
					</div>
				</motion.div>
			)}
		</AnimatePresence>
	);
}
