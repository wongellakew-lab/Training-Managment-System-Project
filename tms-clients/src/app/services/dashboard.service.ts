import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdminDashboardMetrics } from '../models/dashboard.model';
import { Enrollment } from '../models/enrollment.model';

export interface StudentUpcomingClass {
  scheduleId: number;
  courseId?: number;
  courseTitle: string;
  courseCode: string;
  title: string;
  category: string;
  scheduleType: string;
  date: string;
  startTime: string;
  endTime: string;
  location: string;
  instructorName: string;
  notes: string;
}

export interface StudentPendingAssignment {
  id: number;
  courseId: number;
  courseTitle: string;
  courseCode: string;
  title: string;
  maxScore: number;
  dueDate: string;
  status: string;
}

export interface StudentRecentGrade {
  assessmentId: number;
  courseId: number;
  courseTitle: string;
  courseCode: string;
  assessmentTitle: string;
  scoredPoints: number;
  maxScore: number;
  letterGrade: string;
  feedback?: string;
  gradedAt?: string;
}

export interface StudentCourseProgress {
  courseId: number;
  courseTitle: string;
  courseCode: string;
  completedAssignments: number;
  totalAssignments: number;
  completedModules?: number;
  totalModules?: number;
  completionPercentage: number;
  status: string;
  currentGrade?: number;
}

export interface StudentDashboard {
  cumulativeGpa: number;
  earnedCredits: number;
  currentSemester: string;
  activeEnrollmentsCount: number;
  completedCoursesCount: number;
  certificatesCount: number;
  totalEnrollmentsCount: number;
  unreadNotificationCount: number;
  upcomingClasses: StudentUpcomingClass[];
  pendingAssignments: StudentPendingAssignment[];
  recentGrades: StudentRecentGrade[];
  courseProgress: StudentCourseProgress[];
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
