import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { tanstackRouter } from '@tanstack/router-plugin/vite';
import tailwindcss from '@tailwindcss/vite';
import svgr from 'vite-plugin-svgr';

export default defineConfig({
	plugins: [
		tanstackRouter({
			target: 'react',
			autoCodeSplitting: true,
			routesDirectory: 'src/app/routes',
		}),
		tailwindcss(),
		react(),
		svgr(),
	],
	server: {
		host: '0.0.0.0',          // слушать все интерфейсы внутри контейнера
		allowedHosts: [
			'chatflowonline.ru',    // разрешаем твой домен
			'www.chatflowonline.ru' // если будет использоваться
		],
		port: 5173
	}
});
