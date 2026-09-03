import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { DashboardService, StudentDashboard, StudentCourseProgress } from '../../services/dashboard.service';

@Component({
  selector: 'app-student-progress',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatIconModule,
    MatButtonModule,
    MatCardModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    LayoutComponent
  ],
  templateUrl: './student-progress.component.html',
  styleUrls: ['./student-progress.component.scss']
})
export class StudentProgressComponent implements OnInit {
  private dashboardService = inject(DashboardService);

  readonly dashboardData = signal<StudentDashboard | null>(null);
  readonly isLoading = signal<boolean>(true);

  ngOnInit(): void {
    this.dashboardService.getStudentDashboard().subscribe({
      next: (data) => {
        this.dashboardData.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  getOverallProgress(): number {
    const list = this.dashboardData()?.courseProgress;
    if (!list || list.length === 0) return 0;
    const total = list.reduce((acc, c) => acc + c.completionPercentage, 0);
    return Math.round(total / list.length);
  }
}
