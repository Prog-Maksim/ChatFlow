export const RSA_ALGO = {
	name: 'RSA-OAEP',
	modulusLength: 2048,
	publicExponent: new Uint8Array([1, 0, 1]), // 65537
	hash: 'SHA-256',
};

// Генерация пары RSA ключей
export async function generateRSAKeyPair(): Promise<CryptoKeyPair> {
	return crypto.subtle.generateKey(
		RSA_ALGO,
		true,
		['encrypt', 'decrypt']
	);
}

// ArrayBuffer -> Base64
async function arrayBufferToBase64(buffer: ArrayBuffer): Promise<string> {
	const bytes = new Uint8Array(buffer);
	let binary = '';
	for (let i = 0; i < bytes.byteLength; i++) {
		binary += String.fromCharCode(bytes[i]);
	}
	return btoa(binary);
}

// Base64 -> ArrayBuffer
async function base64ToArrayBuffer(b64: string): Promise<ArrayBuffer> {
	const binary = atob(b64);
	const len = binary.length;
	const bytes = new Uint8Array(len);
	for (let i = 0; i < len; i++) bytes[i] = binary.charCodeAt(i);
	return bytes.buffer;
}

// Экспорт публичного ключа в SPKI Base64 без PEM
export async function exportPublicKeySPKI(publicKey: CryptoKey): Promise<string> {
	const spki = await crypto.subtle.exportKey('spki', publicKey);
	return arrayBufferToBase64(spki);
}

// Экспорт приватного ключа в ArrayBuffer (PKCS8)
export async function exportPrivateKeyRaw(privateKey: CryptoKey): Promise<ArrayBuffer> {
	return crypto.subtle.exportKey('pkcs8', privateKey);
}

// Генерация AES ключа из пароля
export async function deriveKeyFromPassword(password: string, salt: Uint8Array, iterations = 200_000) {
	const enc = new TextEncoder();
	const pwKey = await crypto.subtle.importKey(
		'raw',
		enc.encode(password),
		'PBKDF2',
		false,
		['deriveKey']
	);
	return crypto.subtle.deriveKey(
		{
			name: 'PBKDF2',
			salt,
			iterations,
			hash: 'SHA-256',
		},
		pwKey,
		{ name: 'AES-GCM', length: 256 },
		true,
		['encrypt', 'decrypt']
	);
}

// Шифрование ArrayBuffer паролем пользователя
export async function encryptWithPassword(aData: ArrayBuffer, password: string) {
	const salt = crypto.getRandomValues(new Uint8Array(16));
	const iv = crypto.getRandomValues(new Uint8Array(12));
	const aesKey = await deriveKeyFromPassword(password, salt);
	const cipher = await crypto.subtle.encrypt({ name: 'AES-GCM', iv }, aesKey, aData);
	return {
		cipherBase64: await arrayBufferToBase64(cipher),
		ivBase64: await arrayBufferToBase64(iv.buffer),
		saltBase64: await arrayBufferToBase64(salt.buffer),
	};
}

// Расшифровка
export async function decryptWithPassword(cipherBase64: string, ivBase64: string, saltBase64: string, password: string) {
	const cipher = await base64ToArrayBuffer(cipherBase64);
	const iv = new Uint8Array(await base64ToArrayBuffer(ivBase64));
	const salt = new Uint8Array(await base64ToArrayBuffer(saltBase64));
	const aesKey = await deriveKeyFromPassword(password, salt);
	return crypto.subtle.decrypt({ name: 'AES-GCM', iv }, aesKey, cipher); 
}

// Генерация пары ключей и экспорт публичного ключа в SPKI Base64
export async function generateKeyPairSPKI() {
	const keyPair = await generateRSAKeyPair();
	const publicKeySPKI = await exportPublicKeySPKI(keyPair.publicKey);
	return {
		publicKeySPKI,
		privateKey: keyPair.privateKey,
	};
}
