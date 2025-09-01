type UserCardProps = {
  avatar: string;
  name: string;
  username: string;
};

export function UserCard({ avatar, name, username }: UserCardProps) {
	return (
		<div className="flex items-center gap-3 p-2 rounded-lg hover:bg-gray-100 cursor-pointer bg-[white]">
			<img src={avatar} alt={name} className="w-10 h-10 rounded-full" />
			<div>
				<div className="font-semibold">{name}</div>
				<div className="text-xs text-[#808080] font-semibold">@{username}</div>
			</div>
		</div>
	);
}
