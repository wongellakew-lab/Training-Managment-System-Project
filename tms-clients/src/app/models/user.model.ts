export type UserRole = 'Student' | 'Instructor' | 'Admin';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  token?: string;
  refreshToken?: string;
  studentId?: number;
  instructorId?: number;
  profilePictureUrl?: string;
}

export interface LoginRequest {
  email: string;
  password?: string;
}

export interface RegisterRequest {
  email: string;
  password?: string;
  firstName: string;
  lastName: string;
  role: UserRole;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiration: string;
  user: User;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}
