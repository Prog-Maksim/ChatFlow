import ReactDOM from 'react-dom/client';
import { RouterProvider } from '@tanstack/react-router';
import { router } from './app/router/index';
import { RootProvider } from'./app/providers/RootProvider';
import './index.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <RootProvider>
    <RouterProvider router={router} />
  </RootProvider>
);