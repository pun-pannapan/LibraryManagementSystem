export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string | null;
  lastName: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface UserDto {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  roles: string[];
}

export interface AuthResponse {
  token: string;
  expiresAtUtc: string;
  user: UserDto;
}
