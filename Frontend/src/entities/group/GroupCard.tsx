type GroupCardProps = {
  title: string;
  membersCount: number;
};

export function GroupCard({ title, membersCount }: GroupCardProps) {
	return (
		<div className="p-2 rounded-lg hover:bg-gray-100 cursor-pointer">
			<div className="font-medium">{title}</div>
			<div className="text-sm text-gray-500">{membersCount} участников</div>
		</div>
	);
}
