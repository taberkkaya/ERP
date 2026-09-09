export class LoginModel {
  emailOrUserName = '';
  password = '';
}

export interface LoginResponseModel {
  token: string;
  refreshToken: string;
  refreshTokenExpires: string;
}

/** Kullanici yonetimi ekranindaki hesap kaydi. */
export class AppUserModel {
  id = '';
  firstName = '';
  lastName = '';
  fullName = '';
  userName = '';
  email = '';
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
