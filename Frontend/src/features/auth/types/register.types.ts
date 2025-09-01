export interface RegistrationRequest {
  surname: string;
  name: string;
  login: string;
  password: string;
}

export interface IRegistrationInput  extends RegistrationRequest {
  confirmPassword: string;
}

export interface RegistrationResponse {
  message: string;
  successfully: boolean;
  status: number;
  type: string;
  data: {
    code: string;
  };
}
