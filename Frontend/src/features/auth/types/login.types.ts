export interface LoginRequest {
	login: string;
	password: string;
}

export interface LoginResponse {
  message: string;
  successfully: boolean;
  status: number;
  type: string;
  data: AuthDataResponse;
}

export interface AuthDataResponse {
  personId: string;
  deviceId: string;
  accessToken: string;
  refreshToken: string;
  'access-expires-at': string;
}

export interface ILoginInput  extends LoginRequest {
  publicKey: string;
}