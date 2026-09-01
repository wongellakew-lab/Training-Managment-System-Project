import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminDashboardMetrics } from '../models/dashboard.model';
import { Enrollment } from '../models/enrollment.model';

export interface StudentDashboard {
  cumulativeGpa: number;
  activeEnrollmentsCount: number;
  earnedCredits: number;
  certificatesCount: number;
  recentEnrollments: Enrollment[];
}

export interface InstructorDashboard {
  assignedCoursesCount: number;
  activeStudentsCount: number;
  pendingGradesCount: number;
  certificatesAwardedCount: number;
  recentEnrollments: Enrollment[];
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);

  getStudentDashboard(studentId?: number): Observable<StudentDashboard> {
    const params = studentId ? `?studentId=${studentId}` : '';
    return this.http.get<StudentDashboard>(`/api/v1/dashboard/student${params}`);
  }

  getInstructorDashboard(instructorId?: number): Observable<InstructorDashboard> {
    const params = instructorId ? `?instructorId=${instructorId}` : '';
    return this.http.get<InstructorDashboard>(`/api/v1/dashboard/instructor${params}`);
  }

  getAdminDashboard(): Observable<AdminDashboardMetrics> {
    return this.http.get<AdminDashboardMetrics>('/api/v1/dashboard/admin');
  }
}
