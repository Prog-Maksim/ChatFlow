const DB_NAME = 'chatflow_crypto';
const STORE = 'keys';

function openDB(): Promise<IDBDatabase> {
	return new Promise((res, rej) => {
		const req = indexedDB.open(DB_NAME, 1);
		req.onupgradeneeded = () => {
			const db = req.result;
			if (!db.objectStoreNames.contains(STORE)) db.createObjectStore(STORE);
		};
		req.onsuccess = () => res(req.result);
		req.onerror = () => rej(req.error);
	});
}

export async function putItem(key: string, value: unknown) {
	const db = await openDB();
	return new Promise<void>((res, rej) => {
		const tx = db.transaction(STORE, 'readwrite');
		tx.objectStore(STORE).put(value, key);
		tx.oncomplete = () => res();
		tx.onerror = () => rej(tx.error);
	});
}

export async function getItem<T = unknown>(key: string): Promise<T | undefined> {
	const db = await openDB();
	return new Promise((res, rej) => {
		const tx = db.transaction(STORE, 'readonly');
		const req = tx.objectStore(STORE).get(key);
		req.onsuccess = () => res(req.result as T);
		req.onerror = () => rej(req.error);
	});
}

export async function removeItem(key: string) {
	const db = await openDB();
	return new Promise<void>((res, rej) => {
		const tx = db.transaction(STORE, 'readwrite');
		tx.objectStore(STORE).delete(key);
		tx.oncomplete = () => res();
		tx.onerror = () => rej(tx.error);
	});
}
