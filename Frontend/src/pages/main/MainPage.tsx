import { ChatArea } from '../../features/chatarea/ChatArea';
import { Sidebar } from '../../widgets/sidebar/ui/Sidebar';


export function MainPage() {
	return (
		<div className='flex h-[100vh]'>
			<div className='w-[350px]'>
				<Sidebar/>
			</div>
			<div className='w-full'>
				<ChatArea/>
			</div>
		</div>
	);
}
