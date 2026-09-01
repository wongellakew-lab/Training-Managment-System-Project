import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DashboardService, AdminDashboard } from '../../services/dashboard.service';
import { EnrollmentService } from '../../services/enrollment.service';
import { LiveSyncService } from '../../services/live-sync.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="space-y-8 animate-fade-in">
      <!-- Welcome Header -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 class="text-3xl font-black tracking-tight text-white flex items-center gap-3">
            <span class="p-2.5 rounded-xl bg-gradient-to-tr from-indigo-600 to-indigo-800 text-white shadow-lg shadow-indigo-500/20">
              <span class="material-icons text-2xl">admin_panel_settings</span>
            </span>
            System Administration
          </h1>
          <p class="text-sm text-slate-400 mt-1">
            Global overview of enterprise courses, user metrics, and pending enrollment requests.
          </p>
        </div>
        <div class="flex items-center gap-3">
          <button (click)="loadDashboard()" [disabled]="isLoading()" class="btn-secondary text-xs flex items-center gap-2">
            <span class="material-icons text-sm" [ngClass]="{'animate-spin': isLoading()}">refresh</span>
            Sync Metrics
          </button>
          <a routerLink="/admin/courses" class="btn-primary text-xs flex items-center gap-2">
            <span class="material-icons text-sm">library_books</span>
            Manage Courses
          </a>
        </div>
      </div>

      <!-- KPI Metrics Grid -->
      @if (dashboard()) {
        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4">
          <div class="glass-panel p-5 border border-white/10 hover:border-indigo-500/40 transition-colors">
            <span class="text-xs font-semibold text-slate-400 uppercase tracking-wider">Students</span>
            <div class="text-2xl font-black text-white mt-1">{{ dashboard()!.totalStudents }}</div>
            <div class="text-[11px] text-slate-500 mt-1">Registered users</div>
          </div>

          <div class="glass-panel p-5 border border-white/10 hover:border-indigo-500/40 transition-colors">
            <span class="text-xs font-semibold text-slate-400 uppercase tracking-wider">Instructors</span>
            <div class="text-2xl font-black text-indigo-400 mt-1">{{ dashboard()!.totalInstructors }}</div>
            <div class="text-[11px] text-slate-500 mt-1">Faculty staff</div>
          </div>

          <div class="glass-panel p-5 border border-white/10 hover:border-indigo-500/40 transition-colors">
            <span class="text-xs font-semibold text-slate-400 uppercase tracking-wider">Courses</span>
            <div class="text-2xl font-black text-cyan-400 mt-1">{{ dashboard()!.totalCourses }}</div>
            <div class="text-[11px] text-slate-500 mt-1">Active catalog</div>
          </div>

          <div class="glass-panel p-5 border border-white/10 hover:border-indigo-500/40 transition-colors">
            <span class="text-xs font-semibold text-slate-400 uppercase tracking-wider">Active</span>
            <div class="text-2xl font-black text-emerald-400 mt-1">{{ dashboard()!.activeEnrollments }}</div>
            <div class="text-[11px] text-slate-500 mt-1">In training</div>
          </div>

          <div class="glass-panel p-5 border border-white/10 hover:border-indigo-500/40 transition-colors">
            <span class="text-xs font-semibold text-slate-400 uppercase tracking-wider">Pending</span>
            <div class="text-2xl font-black text-amber-400 mt-1">{{ dashboard()!.pendingEnrollments }}</div>
            <div class="text-[11px] text-slate-500 mt-1">Awaiting review</div>
          </div>

          <div class="glass-panel p-5 border border-white/10 hover:border-indigo-500/40 transition-colors">
            <span class="text-xs font-semibold text-slate-400 uppercase tracking-wider">Certificates</span>
            <div class="text-2xl font-black text-purple-400 mt-1">{{ dashboard()!.totalCertificates }}</div>
            <div class="text-[11px] text-slate-500 mt-1">Awarded to date</div>
          </div>
        </div>
      }

      <!-- Main Content Split Grid -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <!-- Pending Approvals Queue -->
        <div class="lg:col-span-2 space-y-4">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-bold text-white flex items-center gap-2">
              <span class="material-icons text-amber-400 text-xl">pending_actions</span>
              Pending Enrollment Requests
            </h2>
            @if (dashboard()?.pendingApprovalList?.length) {
              <span class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-500/20 text-amber-300 border border-amber-500/30">
                {{ dashboard()!.pendingApprovalList.length }} Requests
              </span>
            }
          </div>

          <div class="glass-panel border border-white/10 overflow-hidden">
            @if (isLoading()) {
              <div class="p-8 text-center text-slate-400 text-sm flex items-center justify-center gap-2">
                <span class="material-icons animate-spin">refresh</span>
                Loading approvals queue...
              </div>
            } @else if (!dashboard()?.pendingApprovalList?.length) {
              <div class="p-12 text-center">
                <span class="material-icons text-4xl text-emerald-400 mb-2">task_alt</span>
                <p class="text-slate-200 font-semibold text-sm">All Caught Up</p>
                <p class="text-xs text-slate-400 mt-1">There are no pending enrollment applications requiring approval.</p>
              </div>
            } @else {
              <div class="overflow-x-auto">
                <table class="w-full text-left text-xs text-slate-300">
                  <thead class="bg-slate-900/60 text-slate-400 uppercase font-semibold text-[11px] border-b border-white/10">
                    <tr>
                      <th class="p-4">Student</th>
                      <th class="p-4">Course</th>
                      <th class="p-4">Submitted</th>
                      <th class="p-4 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-white/5">
                    @for (enrollment of dashboard()!.pendingApprovalList; track enrollment.id) {
                      <tr class="hover:bg-white/[0.02] transition-colors">
                        <td class="p-4 font-semibold text-white">
                          {{ enrollment.studentName || 'Student #' + enrollment.studentId }}
                        </td>
                        <td class="p-4">
                          <span class="px-2 py-0.5 rounded bg-indigo-500/20 text-indigo-300 font-mono text-[11px] mr-1.5">
                            {{ enrollment.courseCode || 'CS' }}
                          </span>
                          <span>{{ enrollment.courseTitle }}</span>
                        </td>
                        <td class="p-4 text-slate-400">
                          {{ enrollment.enrolledAt | date:'mediumDate' }}
                        </td>
                        <td class="p-4 text-right space-x-2">
                          <button
                            (click)="approveEnrollment(enrollment.id)"
                            class="px-2.5 py-1 rounded bg-emerald-600/80 hover:bg-emerald-600 text-white font-medium text-xs transition-colors"
                          >
                            Approve
                          </button>
                          <button
                            (click)="rejectEnrollment(enrollment.id)"
                            class="px-2.5 py-1 rounded bg-rose-600/80 hover:bg-rose-600 text-white font-medium text-xs transition-colors"
                          >
                            Reject
                          </button>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
          </div>
        </div>

        <!-- Real-time Activity Feed -->
        <div class="space-y-4">
          <h2 class="text-lg font-bold text-white flex items-center gap-2">
            <span class="material-icons text-indigo-400 text-xl">history</span>
            System Activity Log
          </h2>

          <div class="glass-panel p-5 border border-white/10 space-y-4">
            @if (!dashboard()?.recentActivity?.length) {
              <p class="text-xs text-slate-400 text-center py-6">No recent audit logs available.</p>
            } @else {
              @for (activity of dashboard()!.recentActivity; track activity.id) {
                <div class="flex items-start gap-3 p-3 rounded-lg bg-slate-900/40 border border-white/5">
                  <div class="p-2 rounded-lg bg-indigo-500/10 text-indigo-400">
                    <span class="material-icons text-sm">notifications</span>
                  </div>
                  <div class="flex-1 min-w-0">
                    <p class="text-xs font-semibold text-slate-200 truncate">{{ activity.title }}</p>
                    <p class="text-[11px] text-slate-400 line-clamp-2 mt-0.5">{{ activity.description }}</p>
                    <span class="text-[10px] text-slate-500 mt-1 block">{{ activity.timestamp | date:'short' }}</span>
                  </div>
                </div>
              }
            }
          </div>
        </div>
      </div>
    </div>
  `
})
export class AdminDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private enrollmentService = inject(EnrollmentService);
  private liveSync = inject(LiveSyncService);

  readonly dashboard = signal<AdminDashboard | null>(null);
  readonly isLoading = signal(false);

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
