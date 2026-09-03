import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/course.model';

export interface StudentItem {
  id: number;
  registrationNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  enrollmentDate: string;
  gpa: number;
  coursesEnrolled: number;
  status: string;
  avatar: string;
}

export interface StudentStats {
  totalStudents: number;
  activeEnrolled: number;
  certificatesIssued: number;
}

@Injectable({
  providedIn: 'root'
})
export class StudentService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/students';

  getStudents(params: { page?: number; pageSize?: number; search?: string } = {}): Observable<PagedResult<StudentItem>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    if (params.search) httpParams = httpParams.set('searchTerm', params.search);

    return this.http.get<PagedResult<StudentItem>>(this.baseUrl, { params: httpParams });
  }

  getStudentStats(): Observable<StudentStats> {
    return this.http.get<StudentStats>(`${this.baseUrl}/stats`);
  }
}
