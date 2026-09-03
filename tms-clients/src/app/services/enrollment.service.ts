import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Enrollment, EnrollRequest, EnrollmentStatus, EnrollmentFilterParams } from '../models/enrollment.model';
import { PagedResult } from '../models/course.model';

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

  getEnrollmentsPaged(filter: EnrollmentFilterParams = {}): Observable<PagedResult<Enrollment>> {
    let params = new HttpParams();
    if (filter.page) params = params.set('page', filter.page.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    if (filter.search) params = params.set('search', filter.search);
    if (filter.status && filter.status !== 'All' && filter.status !== 'Any Status') {
      params = params.set('status', filter.status);
    }
    if (filter.category && filter.category !== 'All' && filter.category !== 'All Category') {
      params = params.set('category', filter.category);
    }
    if (filter.studentId) params = params.set('studentId', filter.studentId.toString());
    if (filter.courseId) params = params.set('courseId', filter.courseId.toString());

    return this.http.get<PagedResult<Enrollment>>(this.baseUrl, { params });
  }

  getStudentEnrollments(studentId: number): Observable<Enrollment[]> {
    return this.getEnrollments(studentId);
  }

  enrollStudent(request: EnrollRequest): Observable<Enrollment> {
    return this.http.post<Enrollment>(this.baseUrl, request);
  }

  updateEnrollmentStatus(enrollmentId: number, status: EnrollmentStatus): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${enrollmentId}/status`, { status });
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
