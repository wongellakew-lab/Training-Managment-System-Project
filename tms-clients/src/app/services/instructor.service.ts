import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/course.model';
import { Schedule } from '../models/schedule.model';

export interface InstructorItem {
  id: number;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  department: string;
  hireDate: string;
  status: string;
  coursesCount: number;
  studentsCount: number;
  rating: number;
  avatar: string;
  phoneNumber?: string;
  courseTitles?: string[];
}

export interface InstructorCourseItem {
  id: number;
  code: string;
  title: string;
  category: string;
  level: string;
  status: string;
  enrolledStudentsCount: number;
  lessonsCount: number;
  rating: number;
  imageUrl?: string;
  startDate: string;
  endDate: string;
}

export interface InstructorDetailItem extends InstructorItem {
  phoneNumber?: string;
  bio?: string;
  courses: InstructorCourseItem[];
}

export interface CourseReassignmentItem {
  courseId: number;
  replacementInstructorId: number;
}

export interface SuspendInstructorRequest {
  courseReassignments: CourseReassignmentItem[];
  reason?: string;
}

export interface InstructorStats {
  totalInstructors: number;
  activeMentors: number;
  averageRating: number;
}

export interface CreateInstructorDto {
  firstName: string;
  lastName: string;
  email: string;
  employeeNumber: string;
  department: string;
}

@Injectable({
  providedIn: 'root'
})
export class InstructorService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/instructors';

  getInstructors(params: {
    page?: number;
    pageSize?: number;
    search?: string;
    department?: string;
  } = {}): Observable<PagedResult<InstructorItem>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    if (params.search) httpParams = httpParams.set('searchTerm', params.search);
    if (params.department && params.department !== 'All') {
      httpParams = httpParams.set('department', params.department);
    }

    return this.http.get<PagedResult<InstructorItem>>(this.baseUrl, { params: httpParams });
  }

  getInstructorById(id: number): Observable<InstructorDetailItem> {
    return this.http.get<InstructorDetailItem>(`${this.baseUrl}/${id}`);
  }

  suspendInstructor(id: number, request: SuspendInstructorRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${id}/suspend`, request);
  }

  activateInstructor(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${id}/activate`, {});
  }

  getInstructorSchedules(id: number, year?: number, month?: number, date?: string): Observable<Schedule[]> {
    let httpParams = new HttpParams();
    if (year) httpParams = httpParams.set('year', year.toString());
    if (month) httpParams = httpParams.set('month', month.toString());
    if (date) httpParams = httpParams.set('date', date);

    return this.http.get<Schedule[]>(`${this.baseUrl}/${id}/schedules`, { params: httpParams });
  }

  createInstructor(data: CreateInstructorDto): Observable<InstructorItem> {
    return this.http.post<InstructorItem>(this.baseUrl, data);
  }

  getInstructorStats(): Observable<InstructorStats> {
    return this.http.get<InstructorStats>(`${this.baseUrl}/stats`);
  }
}
