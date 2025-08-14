export interface LoginRequest {
	login: string;
	password: string;
	publicKey: string;
}

export interface LoginResponse {
	message: string;
	successfully: boolean;
	status: number;
	type: string;
	errors?: string;
	data: {
		code: string;
	};
}

export interface ILoginInput {
	login: string;
	password: string;
	publicKey: string;
}