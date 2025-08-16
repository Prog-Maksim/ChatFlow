export interface LoginRequest {
	login: string;
	password: string;
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

export interface ILoginInput  extends LoginRequest {
  publicKey: string;
}