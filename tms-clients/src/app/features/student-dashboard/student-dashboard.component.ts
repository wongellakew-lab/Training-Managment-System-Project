import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { AuthService } from '../../services/auth.service';
import { DashboardService, StudentDashboard } from '../../services/dashboard.service';
import { GradeService } from '../../services/grade.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { NotificationService } from '../../services/notification.service';
import { StudentAcademicRecord } from '../../models/grade.model';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LayoutComponent,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './student-dashboard.component.html',
  styleUrls: ['./student-dashboard.component.scss']
})
export class StudentDashboardComponent implements OnInit {
  authService = inject(AuthService);
  dashboardService = inject(DashboardService);
  gradeService = inject(GradeService);
  liveSync = inject(LiveSyncService);
  notificationService = inject(NotificationService);

  dashboardData = signal<StudentDashboard | null>(null);
  academicRecord = signal<StudentAcademicRecord | null>(null);
  isLoading = signal(true);

  readonly gpaDisplay = computed(() => {
    const dash = this.dashboardData();
    let rawGpa = (dash && dash.cumulativeGpa) ? dash.cumulativeGpa : (this.academicRecord()?.gpa ?? 0);
    // Safety check: if raw percentage > 4.0, convert to 4.00 scale
    if (rawGpa > 4.0) {
      rawGpa = rawGpa >= 90 ? 4.0 : rawGpa >= 80 ? 3.0 : rawGpa >= 70 ? 2.0 : rawGpa >= 60 ? 1.0 : 0.0;
    }
    return Math.min(4.0, Math.max(0, rawGpa)).toFixed(2);
  });

  readonly gpaPercent = computed(() => {
    const gpaNum = parseFloat(this.gpaDisplay());
    return Math.min((gpaNum / 4.0) * 100, 100);
  });

  ngOnInit(): void {
    this.loadDashboardData();

    // Listen for SignalR live events and reload
    this.liveSync.enrollmentUpdated$.subscribe(() => {
      this.loadDashboardData();
    });

    this.liveSync.gradePublished$.subscribe(() => {
      this.loadDashboardData();
    });

    this.liveSync.scheduleUpdated$.subscribe(() => {
      this.loadDashboardData();
    });
  }

  loadDashboardData(): void {
    const user = this.authService.currentUser();
    const studentId = user?.studentId;

    this.isLoading.set(true);

    // Load dashboard data from real API
    this.dashboardService.getStudentDashboard(studentId).subscribe({
      next: (data) => {
        this.dashboardData.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });

    // Load academic record
    if (studentId) {
      this.gradeService.getStudentAcademicRecord(studentId).subscribe({
        next: (rec) => this.academicRecord.set(rec),
        error: () => {}
      });
    }
  }
}
