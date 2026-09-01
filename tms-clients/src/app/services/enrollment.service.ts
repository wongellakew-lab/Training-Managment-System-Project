import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Enrollment, EnrollRequest, UpdateEnrollmentStatusRequest, EnrollmentStatus } from '../models/enrollment.model';

@Injectable({
  providedIn: 'root'
})
export class EnrollmentService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/enrollments';

  getEnrollments(studentId?: number, courseId?: number): Observable<Enrollment[]> {
    let params = new HttpParams();
    if (studentId) params = params.set('studentId', studentId.toString());
    if (courseId) params = params.set('courseId', courseId.toString());

    return this.http.get<Enrollment[]>(this.baseUrl, { params });
  }

  getStudentEnrollments(studentId: number): Observable<Enrollment[]> {
    return this.getEnrollments(studentId);
  }

  enrollStudent(request: EnrollRequest): Observable<Enrollment> {
    return this.http.post<Enrollment>(this.baseUrl, request);
  }

  updateEnrollmentStatus(enrollmentId: number, status: EnrollmentStatus): Observable<Enrollment> {
    return this.http.put<Enrollment>(`${this.baseUrl}/${enrollmentId}/status`, { status });
  }

  approveEnrollment(enrollmentId: number): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/${enrollmentId}/approve`, {});
  }

  rejectEnrollment(enrollmentId: number): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/${enrollmentId}/reject`, {});
  }

  dropEnrollment(enrollmentId: number): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/${enrollmentId}/drop`, {});
  }
}
