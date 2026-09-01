import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Course, CreateCourseDto, UpdateCourseDto, PagedResult, CourseFilterParams } from '../models/course.model';

@Injectable({
  providedIn: 'root'
})
export class CourseService {
  private http = inject(HttpClient);
  private readonly baseUrlV1 = '/api/v1/courses';
  private readonly baseUrlV2 = '/api/v2/courses';

  getCourses(filter: CourseFilterParams = {}): Observable<PagedResult<Course>> {
    let params = new HttpParams();
    const page = filter.pageNumber || filter.page || 1;
    params = params.set('pageNumber', page.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    if (filter.search) params = params.set('search', filter.search);
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    if (filter.department) params = params.set('department', filter.department);
    if (filter.category && filter.category !== 'All' && filter.category !== 'All Category') {
      params = params.set('category', filter.category);
    }
    if (filter.status && filter.status !== 'All') {
      params = params.set('status', filter.status);
    }
    if (filter.sortBy) params = params.set('sortBy', filter.sortBy);
    if (filter.sortDescending !== undefined) params = params.set('sortDescending', filter.sortDescending.toString());
    if (filter.fields) params = params.set('fields', filter.fields);

    return this.http.get<PagedResult<Course>>(this.baseUrlV1, { params });
  }

  getCachedCourses(filter: CourseFilterParams = {}): Observable<PagedResult<Course>> {
    return this.getCourses(filter);
  }

  getCourseById(id: number): Observable<Course> {
    return this.http.get<Course>(`${this.baseUrlV1}/${id}`);
  }

  createCourse(course: CreateCourseDto): Observable<Course> {
    return this.http.post<Course>(this.baseUrlV1, course);
  }

  updateCourse(id: number, course: UpdateCourseDto): Observable<Course> {
    return this.http.put<Course>(`${this.baseUrlV1}/${id}`, course);
  }

  deleteCourse(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrlV1}/${id}`);
  }
}
