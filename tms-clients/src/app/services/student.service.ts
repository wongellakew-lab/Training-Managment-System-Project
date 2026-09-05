import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/course.model';

import { StudentCourseProgressDetail } from '../models/student-progress-detail.model';

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

export interface AdminStudentEnrolledCourse {
  courseId: number;
  code: string;
  title: string;
  category: string;
  level: string;
  imageUrl?: string;
  durationHours: number;
  lessonsCount: number;
  startDate: string;
  endDate: string;
  enrollmentStatus: string;
  enrolledAt: string;
  progressPercentage: number;
  finalScore?: number;
  instructorName?: string;
}

export interface AdminStudentDetail {
  id: number;
  registrationNumber: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  department?: string;
  bio?: string;
  enrollmentDate: string;
  gpa: number;
  status: string;
  avatar: string;
  enrolledCourses: AdminStudentEnrolledCourse[];
}

@Injectable({
  providedIn: 'root'
})
export class StudentService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/students';

  getStudents(params: {
    page?: number;
    pageSize?: number;
    search?: string;
    sortBy?: string;
    sortDescending?: boolean;
  } = {}): Observable<PagedResult<StudentItem>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    if (params.search) httpParams = httpParams.set('searchTerm', params.search);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDescending !== undefined) httpParams = httpParams.set('sortDescending', params.sortDescending.toString());

    return this.http.get<PagedResult<StudentItem>>(this.baseUrl, { params: httpParams });
  }

  getStudentStats(): Observable<StudentStats> {
    return this.http.get<StudentStats>(`${this.baseUrl}/stats`);
  }

  getStudentDetail(id: number): Observable<AdminStudentDetail> {
    return this.http.get<AdminStudentDetail>(`${this.baseUrl}/${id}/detail`);
  }

  suspendStudent(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${id}/suspend`, {});
  }

  placeOnProbation(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${id}/probation`, {});
  }

  activateStudent(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${id}/activate`, {});
  }

  getCourseProgressDetails(courseId: number): Observable<StudentCourseProgressDetail> {
    return this.http.get<StudentCourseProgressDetail>(`/api/v1/student/progress/${courseId}`);
  }
}
