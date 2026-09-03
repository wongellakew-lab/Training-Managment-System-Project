import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { StudentScheduleSummary } from '../models/schedule.model';

@Injectable({
  providedIn: 'root'
})
export class StudentScheduleService {
  private http = inject(HttpClient);
  private baseUrl = '/api/v1/schedules';

  getMyScheduleOverview(year?: number, month?: number, courseId?: number): Observable<StudentScheduleSummary> {
    let params = new HttpParams();
    if (year !== undefined && year !== null) {
      params = params.set('year', year.toString());
    }
    if (month !== undefined && month !== null) {
      params = params.set('month', month.toString());
    }
    if (courseId !== undefined && courseId !== null && courseId > 0) {
      params = params.set('courseId', courseId.toString());
    }

    return this.http.get<StudentScheduleSummary>(`${this.baseUrl}/my-schedule`, { params });
  }
}
