type ChannelCardProps = {
  title: string;
  description?: string;
};

export function ChannelCard({ title, description }: ChannelCardProps) {
	return (
		<div className="p-2 rounded-lg hover:bg-gray-100 cursor-pointer">
			<div className="font-medium">{title}</div>
			{description && <div className="text-sm text-gray-500">{description}</div>}
		</div>
	);
}
