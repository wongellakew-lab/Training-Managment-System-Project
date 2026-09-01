import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AssignGradeRequest, BulkAssignGradesRequest, StudentAcademicRecord } from '../models/grade.model';

@Injectable({
  providedIn: 'root'
})
export class GradeService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/grades';

  assignGrade(request: AssignGradeRequest): Observable<void> {
    return this.http.post<void>(this.baseUrl, request);
  }

  bulkAssignGrades(request: BulkAssignGradesRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/bulk`, request);
  }

  getStudentAcademicRecord(studentId: number): Observable<StudentAcademicRecord> {
    return this.http.get<StudentAcademicRecord>(`${this.baseUrl}/student/${studentId}`);
  }
}
