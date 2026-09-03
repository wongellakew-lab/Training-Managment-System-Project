import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { DashboardService, StudentDashboard, StudentCourseProgress } from '../../services/dashboard.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { Subscription } from 'rxjs';

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
export class StudentProgressComponent implements OnInit, OnDestroy {
  private dashboardService = inject(DashboardService);
  private liveSync = inject(LiveSyncService);

  readonly dashboardData = signal<StudentDashboard | null>(null);
  readonly isLoading = signal<boolean>(true);

  private sub?: Subscription;

  ngOnInit(): void {
    this.loadProgress();

    this.sub = this.liveSync.gradePublished$.subscribe(() => {
      this.loadProgress();
    });
    this.sub.add(this.liveSync.enrollmentUpdated$.subscribe(() => {
      this.loadProgress();
    }));
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  loadProgress(): void {
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
