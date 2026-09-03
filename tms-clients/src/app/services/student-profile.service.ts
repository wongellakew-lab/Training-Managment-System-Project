import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface StudentProfile {
  id: string;
  studentId?: number;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  department?: string;
  bio?: string;
  profilePictureUrl?: string;
  registrationNumber?: string;
  gpa: number;
  enrollmentDate: string;
  roles: string[];
}

export interface UpdateProfilePayload {
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  department?: string;
  bio?: string;
}

export interface ChangePasswordPayload {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

@Injectable({
  providedIn: 'root'
})
export class StudentProfileService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/studentprofile';

  getProfile(): Observable<StudentProfile> {
    return this.http.get<StudentProfile>(this.baseUrl);
  }

  updateProfile(payload: UpdateProfilePayload): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(this.baseUrl, payload);
  }

  changePassword(payload: ChangePasswordPayload): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/change-password`, payload);
  }

  uploadAvatar(file: File): Observable<{ avatarUrl: string; message: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ avatarUrl: string; message: string }>(`${this.baseUrl}/avatar`, formData);
  }
}
