import { ChatArea } from '../../features/chatarea/ChatArea';
import { Sidebar } from '../../features/sidebar/ui/Sidebar';


export function MainPage() {
	return (
		<div className='flex h-[100vh]'>
			<div className='w-[20vw]'>
				<Sidebar/>
			</div>
			<div className='w-[80vw]'>
				<ChatArea/>
			</div>
		</div>
	);
}
