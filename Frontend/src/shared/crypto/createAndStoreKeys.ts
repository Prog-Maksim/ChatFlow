import { putItem } from '../storage/secureStore';
import { generateKeyPairSPKI, exportPrivateKeyRaw, encryptWithPassword } from './webcrypto';

export async function createAndStoreKeys(login: string, password: string) {
	const { publicKeySPKI, privateKey } = await generateKeyPairSPKI();

	const privRaw = await exportPrivateKeyRaw(privateKey);
	const encrypted = await encryptWithPassword(privRaw, password);

	await putItem(`publicKey:${login}`, publicKeySPKI); 
	await putItem(`privateKey:${login}`, encrypted);

	return publicKeySPKI;
}
