import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/course.model';

export interface InstructorItem {
  id: number;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  department: string;
  hireDate: string;
  coursesCount: number;
  studentsCount: number;
  rating: number;
  avatar: string;
}

export interface InstructorStats {
  totalInstructors: number;
  activeMentors: number;
  averageRating: number;
}

@Injectable({
  providedIn: 'root'
})
export class InstructorService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/instructors';

  getInstructors(params: { page?: number; pageSize?: number; search?: string } = {}): Observable<PagedResult<InstructorItem>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    if (params.search) httpParams = httpParams.set('searchTerm', params.search);

    return this.http.get<PagedResult<InstructorItem>>(this.baseUrl, { params: httpParams });
  }

  getInstructorStats(): Observable<InstructorStats> {
    return this.http.get<InstructorStats>(`${this.baseUrl}/stats`);
  }
}
