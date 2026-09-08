export class LoginModel {
  emailOrUserName = '';
  password = '';
}

export interface LoginResponseModel {
  token: string;
  refreshToken: string;
  refreshTokenExpires: string;
}

export class UserModel {
  id = '';
  name = '';
  email = '';
  userName = '';
}

/** Sunucunun ResultKit/TS.Result sarmalayıcısı. */
export interface ResultModel<T> {
  data?: T;
  errorMessages?: string[];
  isSuccessful: boolean;
  statusCode?: number;
}
