import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, throwError, map } from 'rxjs';
import { Router } from '@angular/router';
import { User, LoginRequest, RegisterRequest, AuthResponse, UserRole } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly TOKEN_KEY = 'tms_access_token';
  private readonly REFRESH_TOKEN_KEY = 'tms_refresh_token';
  private readonly USER_KEY = 'tms_user_profile';

  // State Signals
  private currentUserSignal = signal<User | null>(this.loadStoredUser());
  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.currentUserSignal() && !!this.getAccessToken());
  readonly userRole = computed<UserRole | null>(() => this.currentUserSignal()?.role ?? null);
  readonly isStudent = computed(() => this.userRole() === 'Student');
  readonly isInstructor = computed(() => this.userRole() === 'Instructor');
  readonly isAdmin = computed(() => this.userRole() === 'Admin');

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/v1/auth/login', credentials).pipe(
      tap((res) => this.handleAuthSuccess(res)),
      catchError((err) => throwError(() => err))
    );
  }

  register(payload: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/v1/auth/register', payload).pipe(
      tap((res) => this.handleAuthSuccess(res)),
      catchError((err) => throwError(() => err))
    );
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      this.logout();
      return throwError(() => new Error('No refresh token available'));
    }

    return this.http.post<AuthResponse>('/api/v1/auth/refresh', { refreshToken }).pipe(
      tap((res) => this.handleAuthSuccess(res)),
      catchError((err) => {
        this.logout();
        return throwError(() => err);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUserSignal.set(null);
    this.router.navigate(['/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  private handleAuthSuccess(res: AuthResponse): void {
    if (res.token) {
      localStorage.setItem(this.TOKEN_KEY, res.token);
    }
    if (res.refreshToken) {
      localStorage.setItem(this.REFRESH_TOKEN_KEY, res.refreshToken);
    }
    if (res.user) {
      localStorage.setItem(this.USER_KEY, JSON.stringify(res.user));
      this.currentUserSignal.set(res.user);
    }
  }

  private loadStoredUser(): User | null {
    const raw = localStorage.getItem(this.USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as User;
    } catch {
      return null;
    }
  }
}
