import { Outlet, useMatches } from '@tanstack/react-router';

export function Layout() {
  const matches = useMatches();
  const mainMatch = matches.find((match) => match.routeId === '/main');
  const message = mainMatch?.loaderData?.message;

  return (
    <div className="flex flex-col min-h-screen">
      <header className="p-4 bg-gray-200 text-xl">
        Layout Header
        {message && <div className="mt-2 text-green-600">📩 {message}</div>}
      </header>
      <div className="flex-1 p-4">
        <Outlet />
      </div>
    </div>
  );
}
