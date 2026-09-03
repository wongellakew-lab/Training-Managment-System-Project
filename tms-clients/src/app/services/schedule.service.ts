import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Schedule, CreateScheduleDto, UpdateScheduleDto } from '../models/schedule.model';

@Injectable({
  providedIn: 'root'
})
export class ScheduleService {
  private http = inject(HttpClient);
  private baseUrl = '/api/v1/schedules';

  getSchedules(category?: string, month?: number, year?: number): Observable<Schedule[]> {
    let params = new HttpParams();
    if (category && category !== 'All') {
      params = params.set('category', category);
    }
    if (month !== undefined && month !== null) {
      params = params.set('month', month.toString());
    }
    if (year !== undefined && year !== null) {
      params = params.set('year', year.toString());
    }

    return this.http.get<Schedule[]>(this.baseUrl, { params });
  }

  getMySchedule(view?: string, date?: string): Observable<Schedule[]> {
    let params = new HttpParams();
    if (view) params = params.set('view', view);
    if (date) params = params.set('date', date);

    return this.http.get<Schedule[]>(`${this.baseUrl}/my-schedule`, { params });
  }

  getScheduleById(id: number): Observable<Schedule> {
    return this.http.get<Schedule>(`${this.baseUrl}/${id}`);
  }

  createSchedule(dto: CreateScheduleDto): Observable<Schedule> {
    return this.http.post<Schedule>(this.baseUrl, dto);
  }

  updateSchedule(id: number, dto: UpdateScheduleDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }

  deleteSchedule(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
