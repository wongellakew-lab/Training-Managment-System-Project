import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { NavbarComponent } from '../../ui/navbar/navbar.component';
import { AnalyticsChartComponent, StatMetric } from '../../ui/analytics-chart/analytics-chart.component';
import { CourseCardComponent } from '../../ui/course-card/course-card.component';
import { EnrollmentStore } from '../../store/enrollment.store';
import { CourseStore } from '../../store/course.store';
import { AuthService } from '../../services/auth.service';
import { GradeService } from '../../services/grade.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { StudentAcademicRecord } from '../../models/grade.model';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, NavbarComponent, AnalyticsChartComponent],
  template: `
    <div class="min-h-screen bg-slate-950 px-4 sm:px-8 py-6 max-w-7xl mx-auto">
      <app-navbar></app-navbar>

      <!-- Welcome Banner -->
      <div class="glass-panel p-6 sm:p-8 mb-8 relative overflow-hidden">
        <div class="absolute right-0 top-0 w-96 h-96 bg-indigo-500/10 rounded-full blur-3xl pointer-events-none"></div>
        <div class="relative z-10 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <div class="flex items-center gap-2 mb-2">
              <span class="badge badge-primary text-[10px]">Student Workspace</span>
              <span class="text-xs text-slate-400">Welcome back,</span>
            </div>
            <h1 class="text-3xl font-black text-white tracking-tight">
              {{ authService.currentUser()?.firstName }} {{ authService.currentUser()?.lastName }}
            </h1>
            <p class="text-xs text-slate-400 mt-1 max-w-xl">
              Track your enrolled courses, view real-time grade averages, and manage your academic progression.
            </p>
          </div>

          <div class="flex items-center gap-3">
            <a routerLink="/student/courses" class="btn-primary text-xs py-2.5 px-4">
              <span class="material-icons text-sm">search</span>
              <span>Browse Catalog</span>
            </a>
            <a routerLink="/student/certificates" class="btn-secondary text-xs py-2.5 px-4">
              <span class="material-icons text-sm">workspace_premium</span>
              <span>Transcripts</span>
            </a>
          </div>
        </div>
      </div>

      <!-- KPI Metrics -->
      <app-analytics-chart [metrics]="dashboardMetrics()"></app-analytics-chart>

      <!-- Current Enrollments Section -->
      <div class="glass-panel p-6 sm:p-8 mb-8">
        <div class="flex items-center justify-between pb-4 border-b border-white/10 mb-6">
          <div>
            <h2 class="text-xl font-bold text-white">My Course Registrations</h2>
            <p class="text-xs text-slate-400 mt-0.5">Active semester schedules and assigned grades</p>
          </div>
          <div class="flex items-center gap-2">
            <button
              (click)="enrollmentStore.setStatusFilter('All')"
              class="px-3 py-1 text-xs rounded-lg border transition-colors"
              [ngClass]="enrollmentStore.statusFilter() === 'All' ? 'bg-indigo-600 border-indigo-500 text-white' : 'border-white/5 text-slate-400 hover:text-white'"
            >
              All
            </button>
            <button
              (click)="enrollmentStore.setStatusFilter('Active')"
              class="px-3 py-1 text-xs rounded-lg border transition-colors"
              [ngClass]="enrollmentStore.statusFilter() === 'Active' ? 'bg-indigo-600 border-indigo-500 text-white' : 'border-white/5 text-slate-400 hover:text-white'"
            >
              Active
            </button>
            <button
              (click)="enrollmentStore.setStatusFilter('Completed')"
              class="px-3 py-1 text-xs rounded-lg border transition-colors"
              [ngClass]="enrollmentStore.statusFilter() === 'Completed' ? 'bg-indigo-600 border-indigo-500 text-white' : 'border-white/5 text-slate-400 hover:text-white'"
            >
              Completed
            </button>
          </div>
        </div>

        @if (enrollmentStore.isLoading()) {
          <div class="py-12 text-center text-slate-400 text-sm">
            <span class="inline-block animate-spin w-6 h-6 border-2 border-indigo-500 border-t-transparent rounded-full mb-2"></span>
            <div>Loading enrollments...</div>
          </div>
        } @else if (enrollmentStore.filteredEnrollments().length === 0) {
          <div class="py-12 text-center">
            <div class="w-12 h-12 rounded-2xl bg-indigo-900/30 text-indigo-400 flex items-center justify-center mx-auto mb-3">
              <span class="material-icons">menu_book</span>
            </div>
            <h3 class="text-base font-semibold text-slate-300">No course enrollments found</h3>
            <p class="text-xs text-slate-500 max-w-sm mx-auto mt-1 mb-4">You have not registered for any courses matching this filter yet.</p>
            <a routerLink="/student/courses" class="btn-primary text-xs py-2 px-4">Find Courses to Enroll</a>
          </div>
        } @else {
          <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse">
              <thead>
                <tr class="border-b border-white/10 text-[11px] uppercase tracking-wider text-slate-400 font-semibold">
                  <th class="py-3 px-4">Course</th>
                  <th class="py-3 px-4">Status</th>
                  <th class="py-3 px-4">Enrolled Date</th>
                  <th class="py-3 px-4 text-center">Letter Grade</th>
                  <th class="py-3 px-4 text-center">Score</th>
                  <th class="py-3 px-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-white/5 text-xs text-slate-300">
                @for (item of enrollmentStore.filteredEnrollments(); track item.id) {
                  <tr class="hover:bg-slate-900/50 transition-colors">
                    <td class="py-3.5 px-4 font-semibold text-white">
                      {{ item.courseTitle || 'Course #' + item.courseId }}
                      <span class="block text-[11px] font-mono text-indigo-400 font-normal">{{ item.courseCode }}</span>
                    </td>
                    <td class="py-3.5 px-4">
                      <span
                        class="badge"
                        [ngClass]="{
                          'badge-success': item.status === 'Completed',
                          'badge-primary': item.status === 'Active',
                          'badge-warning': item.status === 'Pending',
                          'badge-danger': item.status === 'Dropped'
                        }"
                      >
                        {{ item.status }}
                      </span>
                    </td>
                    <td class="py-3.5 px-4 text-slate-400">{{ item.enrollmentDate | date:'mediumDate' }}</td>
                    <td class="py-3.5 px-4 text-center font-bold" [ngClass]="item.grade ? 'text-indigo-300' : 'text-slate-500'">
                      {{ item.grade || '—' }}
                    </td>
                    <td class="py-3.5 px-4 text-center font-mono" [ngClass]="item.numericScore ? 'text-slate-200' : 'text-slate-500'">
                      {{ item.numericScore !== undefined ? item.numericScore + '%' : '—' }}
                    </td>
                    <td class="py-3.5 px-4 text-right">
                      @if (item.status === 'Active') {
                        <button
                          (click)="dropCourse(item.id)"
                          class="text-xs text-rose-400 hover:text-rose-300 hover:underline transition-colors"
                        >
                          Drop
                        </button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    </div>
  `
})
export class StudentDashboardComponent implements OnInit {
  authService = inject(AuthService);
  enrollmentStore = inject(EnrollmentStore);
  courseStore = inject(CourseStore);
  gradeService = inject(GradeService);
  liveSync = inject(LiveSyncService);

  academicRecord = signal<StudentAcademicRecord | null>(null);

  dashboardMetrics = signal<StatMetric[]>([
    { label: 'Cumulative GPA', value: '3.85', icon: 'auto_awesome', change: 'Top 10% of class', trend: 'up' },
    { label: 'Enrolled Courses', value: '3', icon: 'school', change: 'Active this term' },
    { label: 'Earned Credits', value: '18', icon: 'military_tech', change: 'Towards degree' },
    { label: 'Certificates', value: '2', icon: 'verified', change: 'Verified credentials', trend: 'up' }
  ]);

  ngOnInit() {
    const studentId = this.authService.currentUser()?.studentId || 1;
    this.enrollmentStore.loadStudentEnrollments(studentId);

    this.gradeService.getStudentAcademicRecord(studentId).subscribe({
      next: (rec) => {
        this.academicRecord.set(rec);
        if (rec) {
          this.dashboardMetrics.set([
            { label: 'Cumulative GPA', value: rec.gpa ? rec.gpa.toFixed(2) : '3.80', icon: 'auto_awesome', change: 'Calculated Grade Average', trend: 'up' },
            { label: 'Enrolled Courses', value: this.enrollmentStore.activeEnrollmentsCount().toString(), icon: 'school', change: 'Active this term' },
            { label: 'Earned Credits', value: rec.totalCredits.toString(), icon: 'military_tech', change: 'Credits completed' },
            { label: 'Certificates', value: '1', icon: 'verified', change: 'Verified credentials', trend: 'up' }
          ]);
        }
      },
      error: () => {}
    });

    // Listen for SignalR live enrollment events
    this.liveSync.enrollmentUpdated$.subscribe(() => {
      this.enrollmentStore.loadStudentEnrollments(studentId);
    });
  }

  dropCourse(enrollmentId: number) {
    if (confirm('Are you sure you want to drop this course?')) {
      this.enrollmentStore.drop(enrollmentId);
    }
  }
}
