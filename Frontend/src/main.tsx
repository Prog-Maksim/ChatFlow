import ReactDOM from 'react-dom/client';
import { RouterProvider } from '@tanstack/react-router';
import { router } from './routes/__root';
import { RootProvider } from'./app/providers/RootProvider';
import './index.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <RootProvider>
    <RouterProvider router={router} />
  </RootProvider>
);