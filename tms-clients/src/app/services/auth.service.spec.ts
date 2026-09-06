import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let routerSpy: { navigate: any };

  beforeEach(() => {
    routerSpy = { navigate: vi.fn() };
    localStorage.clear();
    sessionStorage.clear();

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: Router, useValue: routerSpy }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('should be created with initial unauthenticated state', () => {
    expect(service).toBeTruthy();
    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
  });

  it('should handle successful login and store tokens', () => {
    const mockResponse = {
      token: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjI1MjQ2MDgwMDAsInN1YiI6IjEyMyJ9.sig',
      refreshToken: 'jwt-refresh-token',
      user: {
        id: 'user-1',
        email: 'student@tms.com',
        firstName: 'Jane',
        lastName: 'Doe',
        role: 'Student' as const,
        studentId: 1
      }
    };

    service.login({ email: 'student@tms.com', password: 'Password123!' }).subscribe((res) => {
      expect(res.token).toBe(mockResponse.token);
      expect(service.isAuthenticated()).toBe(true);
      expect(service.isStudent()).toBe(true);
      expect(service.currentUser()?.firstName).toBe('Jane');
    });

    const req = httpMock.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });

  it('should clear stored session on logout and navigate to login', () => {
    localStorage.setItem('tms_access_token', 'test-token');
    localStorage.setItem('tms_refresh_token', 'test-refresh-token');
    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(localStorage.getItem('tms_access_token')).toBeNull();
    expect(localStorage.getItem('tms_refresh_token')).toBeNull();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);

    // Best-effort revocation HTTP request is sent
    const req = httpMock.expectOne('/api/v1/auth/logout');
    expect(req.request.method).toBe('POST');
    req.flush({ message: 'Logged out successfully.' });
  });

  it('should navigate to login with sessionExpired queryParam when logout(true) is invoked', () => {
    service.logout(true);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { sessionExpired: 'true' } });
  });

  it('should prevent multiple concurrent refresh calls and return the same new token via mutex', () => {
    localStorage.setItem('tms_refresh_token', 'initial-refresh-token');

    const newAccessToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjI1MjQ2MDgwMDAsInN1YiI6IjEyMyJ9.sig2';
    const mockRefreshResponse = {
      token: newAccessToken,
      accessToken: newAccessToken,
      refreshToken: 'rotated-refresh-token',
      user: { id: '1', email: 'a@b.com', firstName: 'A', lastName: 'B', role: 'Student' as const }
    };

    let call1Result: any = null;
    let call2Result: any = null;

    // Trigger 2 simultaneous refresh calls
    service.refreshToken().subscribe((res) => (call1Result = res));
    service.refreshToken().subscribe((res) => (call2Result = res));

    // Only ONE HTTP request must be sent!
    const req = httpMock.expectOne('/api/v1/auth/refresh');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ refreshToken: 'initial-refresh-token' });
    req.flush(mockRefreshResponse);

    // Both subscriptions receive the refreshed token
    expect(call1Result).not.toBeNull();
    expect(call2Result).not.toBeNull();
    expect(call1Result.token).toBe(newAccessToken);
    expect(call2Result.token).toBe(newAccessToken);
  });

  it('should correctly detect expired vs non-expired JWT tokens', () => {
    const futureToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjI1MjQ2MDgwMDAsInN1YiI6IjEyMyJ9.sig';
    const pastToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1MDAwMDAwMDAsInN1YiI6IjEyMyJ9.sig';

    expect(service.isTokenExpired(futureToken)).toBe(false);
    expect(service.isTokenExpired(pastToken)).toBe(true);
    expect(service.isTokenExpired(null)).toBe(true);
  });
});
