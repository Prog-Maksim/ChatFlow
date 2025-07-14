import {
  createRootRoute,
  createRoute,
  createRouter,
} from '@tanstack/react-router';
import { Layout } from '../../pages/layout/Layout';
import { MainPage } from '../../pages/MainPage/MainPage';

const rootRoute = createRootRoute();
const mainLayoutRoute = createRoute({ getParentRoute: () => rootRoute, path: '/', component: Layout });
const mainRoute = createRoute({
  getParentRoute: () => mainLayoutRoute,
  path: '/main',
  component: MainPage,
  loader: async () => {
    return { message: 'Hello from MainPage loader!' };
  },
});


const routeTree = rootRoute.addChildren([
  mainLayoutRoute.addChildren([mainRoute]),
]);

export const router = createRouter({ routeTree });
declare module '@tanstack/react-router' { interface Register { router: typeof router } }
