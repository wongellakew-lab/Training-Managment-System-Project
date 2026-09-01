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
  });

  it('should be created with initial unauthenticated state', () => {
    expect(service).toBeTruthy();
    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
  });

  it('should handle successful login and store tokens', () => {
    const mockResponse = {
      token: 'jwt-access-token',
      refreshToken: 'jwt-refresh-token',
      expiration: '2026-12-31T00:00:00Z',
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
      expect(res.token).toBe('jwt-access-token');
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
    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });
});
