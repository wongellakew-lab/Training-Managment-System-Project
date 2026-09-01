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
    if (filter.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    if (filter.search) params = params.set('search', filter.search);
    if (filter.department) params = params.set('department', filter.department);
    if (filter.fields) params = params.set('fields', filter.fields);

    return this.http.get<PagedResult<Course>>(this.baseUrlV1, { params });
  }

  getCachedCourses(filter: CourseFilterParams = {}): Observable<PagedResult<Course>> {
    let params = new HttpParams();
    if (filter.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    if (filter.search) params = params.set('search', filter.search);
    if (filter.department) params = params.set('department', filter.department);

    return this.http.get<PagedResult<Course>>(this.baseUrlV2, { params });
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
