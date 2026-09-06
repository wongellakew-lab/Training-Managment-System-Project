import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, throwError, map, BehaviorSubject, filter, take } from 'rxjs';
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
  readonly isAuthenticated = computed(() => !!this.currentUserSignal() && !!this.getAccessToken() && !this.isTokenExpired());
  readonly userRole = computed<UserRole | null>(() => this.currentUserSignal()?.role ?? null);
  readonly isStudent = computed(() => this.userRole() === 'Student');
  readonly isInstructor = computed(() => this.userRole() === 'Instructor');
  readonly isAdmin = computed(() => this.userRole() === 'Admin');

  // Token refresh concurrency control (mutex / queue)
  private isRefreshing = false;
  private refreshTokenSubject = new BehaviorSubject<string | null>(null);

  // Proactive session / expiration timer
  private sessionTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    const token = this.getAccessToken();
    if (token) {
      if (this.isTokenExpired(token)) {
        if (this.getRefreshToken()) {
          this.refreshToken().subscribe({
            error: () => this.logout(true)
          });
        } else {
          this.logout(true);
        }
      } else {
        this.scheduleTokenExpiration(token);
      }
    }
  }

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

  /**
   * Refreshes the JWT token.
   * Uses an in-flight mutex queue to prevent multiple simultaneous requests from
   * triggering duplicate refresh attempts, which would invalidate single-use refresh tokens.
   */
  refreshToken(): Observable<AuthResponse> {
    if (this.isRefreshing) {
      return this.refreshTokenSubject.pipe(
        filter((token): token is string => token !== null),
        take(1),
        map((token) => ({
          token,
          accessToken: token,
          refreshToken: this.getRefreshToken() || '',
          expiration: '',
          user: this.currentUser()!
        }))
      );
    }

    const currentRefreshToken = this.getRefreshToken();
    if (!currentRefreshToken) {
      this.logout(true);
      return throwError(() => new Error('No refresh token available'));
    }

    this.isRefreshing = true;
    this.refreshTokenSubject.next(null);

    return this.http.post<AuthResponse>('/api/v1/auth/refresh', { refreshToken: currentRefreshToken }).pipe(
      tap((res) => {
        this.isRefreshing = false;
        this.handleAuthSuccess(res);
        this.refreshTokenSubject.next(res.token || res.accessToken || null);
      }),
      catchError((err) => {
        this.isRefreshing = false;
        this.refreshTokenSubject.next(null);
        this.logout(true);
        return throwError(() => err);
      })
    );
  }

  /**
   * Completely logs out the user:
   * - Informs the backend to invalidate refresh token in the database
   * - Purges all tokens, user profiles, and storage
   * - Clears proactive session timers
   * - Resets reactive state signals
   * - Cleanly redirects to the login screen
   */
  logout(sessionExpired: boolean = false): void {
    const refreshToken = this.getRefreshToken();
    if (refreshToken) {
      // Best-effort server-side revocation (fire-and-forget)
      this.http.post('/api/v1/auth/logout', { refreshToken }).subscribe({
        next: () => {},
        error: () => {}
      });
    }

    this.clearSessionTimer();

    // Completely clear all stored auth state
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    sessionStorage.clear();

    this.currentUserSignal.set(null);
    this.isRefreshing = false;
    this.refreshTokenSubject.next(null);

    if (sessionExpired) {
      this.router.navigate(['/login'], { queryParams: { sessionExpired: 'true' } });
    } else {
      this.router.navigate(['/login']);
    }
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  updateUserProfile(partial: Partial<User>): void {
    const current = this.currentUserSignal();
    if (!current) return;
    const updated = { ...current, ...partial };
    localStorage.setItem(this.USER_KEY, JSON.stringify(updated));
    this.currentUserSignal.set(updated);
  }

  /**
   * Decodes JWT exp claim to check if the given or stored token is expired.
   */
  isTokenExpired(token?: string | null): boolean {
    const jwt = token ?? this.getAccessToken();
    if (!jwt) return true;
    try {
      const parts = jwt.split('.');
      if (parts.length < 2) return false;
      const base64Url = parts[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      const decoded = JSON.parse(jsonPayload);
      if (!decoded.exp) return false;
      return Date.now() >= decoded.exp * 1000;
    } catch {
      return true;
    }
  }

  private handleAuthSuccess(res: AuthResponse): void {
    const token = res.token || res.accessToken;
    if (token) {
      localStorage.setItem(this.TOKEN_KEY, token);
      this.scheduleTokenExpiration(token);
    }
    if (res.refreshToken) {
      localStorage.setItem(this.REFRESH_TOKEN_KEY, res.refreshToken);
    }
    if (res.user) {
      localStorage.setItem(this.USER_KEY, JSON.stringify(res.user));
      this.currentUserSignal.set(res.user);
    }
  }

  private scheduleTokenExpiration(token: string): void {
    this.clearSessionTimer();
    try {
      const parts = token.split('.');
      if (parts.length >= 2) {
        const decoded = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
        if (decoded.exp) {
          const msUntilExpiry = decoded.exp * 1000 - Date.now();
          // Proactively refresh 60 seconds before token expires (clamped to 32-bit signed int for setTimeout)
          const refreshLeadTimeMs = 60 * 1000;
          const MAX_TIMEOUT_MS = 2147483647;
          const delay = Math.min(Math.max(msUntilExpiry - refreshLeadTimeMs, 5000), MAX_TIMEOUT_MS);

          if (msUntilExpiry > 0) {
            this.sessionTimer = setTimeout(() => {
              if (this.currentUserSignal()) {
                this.refreshToken().subscribe({
                  error: () => this.logout(true)
                });
              }
            }, delay);
          } else {
            // Token is already expired
            this.logout(true);
          }
        }
      }
    } catch {
      // Ignored
    }
  }

  private clearSessionTimer(): void {
    if (this.sessionTimer) {
      clearTimeout(this.sessionTimer);
      this.sessionTimer = null;
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
