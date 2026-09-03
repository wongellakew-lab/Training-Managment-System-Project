import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Course } from '../models/course.model';
import { Enrollment } from '../models/enrollment.model';
import { StudentEnrolledCourse, StudentCoursesStatistics } from '../models/student-my-courses.model';

@Injectable({
  providedIn: 'root'
})
export class StudentCourseService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/courses';
  private readonly enrollmentsUrl = '/api/v1/enrollments';

  getCourseDetails(id: number): Observable<Course> {
    return this.http.get<Course>(`${this.baseUrl}/${id}`);
  }

  getMyCourses(): Observable<StudentEnrolledCourse[]> {
    return this.http.get<StudentEnrolledCourse[]>(`${this.baseUrl}/my-courses`);
  }

  getMyCoursesStatistics(): Observable<StudentCoursesStatistics> {
    return this.http.get<StudentCoursesStatistics>(`${this.baseUrl}/my-courses/statistics`);
  }

  enrollInCourse(courseId: number, studentId: number): Observable<Enrollment> {
    return this.http.post<Enrollment>(this.enrollmentsUrl, {
      courseId,
      studentId,
      batch: 'Batch 1',
      status: 'Active'
    });
  }

  withdrawFromCourse(courseId: number): Observable<{ message: string; courseId: number; status: string }> {
    return this.http.post<{ message: string; courseId: number; status: string }>(`/api/v1/student/my-courses/${courseId}/withdraw`, {});
  }
}
