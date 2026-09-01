import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NavbarComponent } from '../../ui/navbar/navbar.component';
import { DashboardService, AdminDashboard } from '../../services/dashboard.service';
import { EnrollmentService } from '../../services/enrollment.service';
import { LiveSyncService } from '../../services/live-sync.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    NavbarComponent,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTableModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private enrollmentService = inject(EnrollmentService);
  private liveSync = inject(LiveSyncService);

  readonly dashboard = signal<AdminDashboard | null>(null);
  readonly isLoading = signal(false);

  displayedColumns = ['student', 'course', 'date', 'actions'];

  ngOnInit(): void {
    this.loadDashboard();
    // Listen for live enrollment events to refresh admin dashboard in real-time
    this.liveSync.enrollmentUpdated$.subscribe(() => {
      this.loadDashboard();
    });
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.dashboardService.getAdminDashboard().subscribe({
      next: (data) => {
        this.dashboard.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  approveEnrollment(id: number): void {
    this.enrollmentService.approveEnrollment(id).subscribe({
      next: () => this.loadDashboard()
    });
  }

  rejectEnrollment(id: number): void {
    this.enrollmentService.rejectEnrollment(id).subscribe({
      next: () => this.loadDashboard()
    });
  }
}
