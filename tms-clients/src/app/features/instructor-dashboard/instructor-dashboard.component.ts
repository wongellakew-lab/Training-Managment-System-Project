import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavbarComponent } from '../../ui/navbar/navbar.component';
import { AnalyticsChartComponent, StatMetric } from '../../ui/analytics-chart/analytics-chart.component';
import { CourseService } from '../../services/course.service';
import { EnrollmentService } from '../../services/enrollment.service';
import { GradeService } from '../../services/grade.service';
import { CertificateService } from '../../services/certificate.service';
import { AuthService } from '../../services/auth.service';
import { Course } from '../../models/course.model';
import { Enrollment } from '../../models/enrollment.model';
import { BulkGradeItem } from '../../models/grade.model';

interface GradeRow {
  enrollment: Enrollment;
  letterGrade: string;
  numericScore: number;
  isSaving: boolean;
  isIssued: boolean;
}

@Component({
  selector: 'app-instructor-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, AnalyticsChartComponent],
  template: `
    <div class="min-h-screen bg-slate-950 px-4 sm:px-8 py-6 max-w-7xl mx-auto">
      <app-navbar></app-navbar>

      <!-- Welcome Banner -->
      <div class="glass-panel p-6 sm:p-8 mb-8 relative overflow-hidden">
        <div class="absolute right-0 top-0 w-96 h-96 bg-cyan-500/10 rounded-full blur-3xl pointer-events-none"></div>
        <div class="relative z-10 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <div class="flex items-center gap-2 mb-2">
              <span class="badge badge-info text-[10px]">Instructor Workspace</span>
              <span class="text-xs text-slate-400">Welcome, Professor</span>
            </div>
            <h1 class="text-3xl font-black text-white tracking-tight">
              {{ authService.currentUser()?.firstName }} {{ authService.currentUser()?.lastName }}
            </h1>
            <p class="text-xs text-slate-400 mt-1">Manage course rosters, submit letter grades, and award course certificates.</p>
          </div>

          <div class="flex items-center gap-3">
            <button (click)="submitAllGrades()" [disabled]="isSubmittingBulk() || gradeRows().length === 0" class="btn-primary text-xs py-2.5 px-4">
              <span class="material-icons text-sm">cloud_upload</span>
              <span>Submit All Grades</span>
            </button>
          </div>
        </div>
      </div>

      <!-- KPI Metrics -->
      <app-analytics-chart [metrics]="dashboardMetrics()"></app-analytics-chart>

      <!-- Course Roster & Grade Submission Panel -->
      <div class="glass-panel p-6 sm:p-8 mb-8">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between pb-6 border-b border-white/10 mb-6 gap-4">
          <div>
            <h2 class="text-xl font-bold text-white">Course Gradebook & Roster</h2>
            <p class="text-xs text-slate-400">Select an active course section to view students</p>
          </div>

          <!-- Course Selector Dropdown -->
          <div class="w-full sm:w-72">
            <select
              [ngModel]="selectedCourseId()"
              (ngModelChange)="onCourseSelect($event)"
              class="form-select text-xs font-semibold"
            >
              @for (c of courses(); track c.id) {
                <option [value]="c.id">{{ c.code }} - {{ c.title }}</option>
              }
            </select>
          </div>
        </div>

        @if (feedbackMessage()) {
          <div
            class="mb-6 p-4 rounded-xl flex items-center justify-between text-xs"
            [ngClass]="isFeedbackSuccess() ? 'bg-emerald-500/15 border border-emerald-500/30 text-emerald-300' : 'bg-rose-500/15 border border-rose-500/30 text-rose-300'"
          >
            <div class="flex items-center gap-2">
              <span class="material-icons text-base">{{ isFeedbackSuccess() ? 'check_circle' : 'error' }}</span>
              <span>{{ feedbackMessage() }}</span>
            </div>
            <button (click)="feedbackMessage.set(null)" class="material-icons text-sm hover:opacity-80">close</button>
          </div>
        }

        @if (isLoading()) {
          <div class="py-16 text-center text-slate-400">
            <span class="inline-block animate-spin w-8 h-8 border-2 border-cyan-500 border-t-transparent rounded-full mb-3"></span>
            <div class="text-sm">Loading course roster...</div>
          </div>
        } @else if (gradeRows().length === 0) {
          <div class="py-12 text-center">
            <span class="material-icons text-4xl text-slate-600 mb-2">group_off</span>
            <h3 class="text-base font-semibold text-slate-300">No students enrolled in this course yet</h3>
            <p class="text-xs text-slate-500 mt-1">Students will appear here once they register.</p>
          </div>
        } @else {
          <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse">
              <thead>
                <tr class="border-b border-white/10 text-[11px] uppercase tracking-wider text-slate-400 font-semibold">
                  <th class="py-3 px-4">Student</th>
                  <th class="py-3 px-4">Status</th>
                  <th class="py-3 px-4 w-32">Numeric (0-100)</th>
                  <th class="py-3 px-4 w-28 text-center">Letter Grade</th>
                  <th class="py-3 px-4 text-center">Certificate</th>
                  <th class="py-3 px-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-white/5 text-xs text-slate-300">
                @for (row of gradeRows(); track row.enrollment.id) {
                  <tr class="hover:bg-slate-900/50 transition-colors">
                    <td class="py-3.5 px-4 font-semibold text-white">
                      {{ row.enrollment.studentName || 'Student #' + row.enrollment.studentId }}
                      <span class="block text-[11px] font-mono text-slate-400 font-normal">ID: {{ row.enrollment.studentId }}</span>
                    </td>
                    <td class="py-3.5 px-4">
                      <span class="badge" [ngClass]="row.enrollment.status === 'Completed' ? 'badge-success' : 'badge-primary'">
                        {{ row.enrollment.status }}
                      </span>
                    </td>
                    <td class="py-3.5 px-4">
                      <input
                        type="number"
                        min="0"
                        max="100"
                        [(ngModel)]="row.numericScore"
                        (ngModelChange)="onScoreChange(row)"
                        class="form-input text-xs py-1 px-2 font-mono text-right"
                      />
                    </td>
                    <td class="py-3.5 px-4 text-center font-bold text-sm text-cyan-300">
                      {{ row.letterGrade || '—' }}
                    </td>
                    <td class="py-3.5 px-4 text-center">
                      @if (row.isIssued) {
                        <span class="badge badge-success text-[10px]">Issued</span>
                      } @else if (row.enrollment.status === 'Completed' || row.numericScore >= 60) {
                        <button
                          (click)="issueCertificate(row)"
                          class="btn-secondary text-[11px] py-1 px-2.5"
                        >
                          Award Cert
                        </button>
                      } @else {
                        <span class="text-slate-500 text-[11px]">Pending Grade</span>
                      }
                    </td>
                    <td class="py-3.5 px-4 text-right">
                      <button
                        (click)="saveSingleGrade(row)"
                        [disabled]="row.isSaving"
                        class="btn-primary text-[11px] py-1 px-3"
                      >
                        {{ row.isSaving ? 'Saving...' : 'Save Grade' }}
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
  `
})
export class InstructorDashboardComponent implements OnInit {
  authService = inject(AuthService);
  courseService = inject(CourseService);
  enrollmentService = inject(EnrollmentService);
  gradeService = inject(GradeService);
  certificateService = inject(CertificateService);

  courses = signal<Course[]>([]);
  selectedCourseId = signal<number | null>(null);
  gradeRows = signal<GradeRow[]>([]);
  isLoading = signal(false);
  isSubmittingBulk = signal(false);
  feedbackMessage = signal<string | null>(null);
  isFeedbackSuccess = signal(true);

  dashboardMetrics = signal<StatMetric[]>([
    { label: 'Assigned Courses', value: '4', icon: 'cast_for_education', change: 'Fall Semester' },
    { label: 'Active Students', value: '48', icon: 'groups', change: 'Across all sections' },
    { label: 'Grading Progress', value: '82%', icon: 'fact_check', change: 'Term submissions' },
    { label: 'Certificates Awarded', value: '14', icon: 'workspace_premium', change: 'Total Issued', trend: 'up' }
  ]);

  ngOnInit() {
    this.loadInstructorCourses();
  }

  loadInstructorCourses() {
    this.courseService.getCourses({ pageSize: 50 }).subscribe({
      next: (res) => {
        this.courses.set(res.items);
        if (res.items.length > 0) {
          this.selectedCourseId.set(res.items[0].id);
          this.loadRoster(res.items[0].id);
        }
      }
    });
  }

  onCourseSelect(courseId: number) {
    this.selectedCourseId.set(courseId);
    this.loadRoster(courseId);
  }

  loadRoster(courseId: number) {
    this.isLoading.set(true);
    this.enrollmentService.getEnrollments(undefined, courseId).subscribe({
      next: (enrollments) => {
        const rows: GradeRow[] = enrollments.map((e) => ({
          enrollment: e,
          letterGrade: e.grade || this.calculateGrade(e.numericScore || 85),
          numericScore: e.numericScore !== undefined ? e.numericScore : 85,
          isSaving: false,
          isIssued: false
        }));
        this.gradeRows.set(rows);
        this.isLoading.set(false);
      },
      error: () => {
        this.gradeRows.set([]);
        this.isLoading.set(false);
      }
    });
  }

  onScoreChange(row: GradeRow) {
    row.letterGrade = this.calculateGrade(row.numericScore);
  }

  calculateGrade(score: number): string {
    if (score >= 90) return 'A';
    if (score >= 80) return 'B';
    if (score >= 70) return 'C';
    if (score >= 60) return 'D';
    return 'F';
  }

  saveSingleGrade(row: GradeRow) {
    row.isSaving = true;
    this.gradeService.assignGrade({
      enrollmentId: row.enrollment.id,
      letterGrade: row.letterGrade,
      numericScore: row.numericScore
    }).subscribe({
      next: () => {
        row.isSaving = false;
        this.isFeedbackSuccess.set(true);
        this.feedbackMessage.set(`Grade saved successfully for ${row.enrollment.studentName || 'Student'}!`);
      },
      error: (err) => {
        row.isSaving = false;
        this.isFeedbackSuccess.set(false);
        this.feedbackMessage.set(err.message || 'Failed to save grade.');
      }
    });
  }

  submitAllGrades() {
    this.isSubmittingBulk.set(true);
    const bulkItems: BulkGradeItem[] = this.gradeRows().map((r) => ({
      enrollmentId: r.enrollment.id,
      letterGrade: r.letterGrade,
      numericScore: r.numericScore
    }));

    this.gradeService.bulkAssignGrades({ grades: bulkItems }).subscribe({
      next: () => {
        this.isSubmittingBulk.set(false);
        this.isFeedbackSuccess.set(true);
        this.feedbackMessage.set('All roster grades successfully submitted and saved!');
      },
      error: (err) => {
        this.isSubmittingBulk.set(false);
        this.isFeedbackSuccess.set(false);
        this.feedbackMessage.set(err.message || 'Failed to submit bulk grades.');
      }
    });
  }

  issueCertificate(row: GradeRow) {
    this.certificateService.issueCertificate({
      studentId: row.enrollment.studentId,
      courseId: row.enrollment.courseId
    }).subscribe({
      next: (cert) => {
        row.isIssued = true;
        this.isFeedbackSuccess.set(true);
        this.feedbackMessage.set(`Certificate #${cert.certificateCode} issued for ${cert.studentName}!`);
      },
      error: (err) => {
        this.isFeedbackSuccess.set(false);
        this.feedbackMessage.set(err.message || 'Certificate issuance failed.');
      }
    });
  }
}
