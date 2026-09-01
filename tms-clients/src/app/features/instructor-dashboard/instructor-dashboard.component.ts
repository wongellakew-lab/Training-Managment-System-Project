import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { CourseService } from '../../services/course.service';
import { EnrollmentService } from '../../services/enrollment.service';
import { GradeService } from '../../services/grade.service';
import { CertificateService } from '../../services/certificate.service';
import { DashboardService, InstructorDashboard } from '../../services/dashboard.service';
import { AuthService } from '../../services/auth.service';
import { Course } from '../../models/course.model';
import { Enrollment } from '../../models/enrollment.model';
import { BulkGradeItem } from '../../models/grade.model';

export interface GradeRow {
  enrollment: Enrollment;
  letterGrade: string;
  numericScore: number;
  isSaving: boolean;
  isIssued: boolean;
}

@Component({
  selector: 'app-instructor-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LayoutComponent,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatTableModule
  ],
  templateUrl: './instructor-dashboard.component.html',
  styleUrls: ['./instructor-dashboard.component.scss']
})
export class InstructorDashboardComponent implements OnInit {
  authService = inject(AuthService);
  courseService = inject(CourseService);
  enrollmentService = inject(EnrollmentService);
  gradeService = inject(GradeService);
  certificateService = inject(CertificateService);
  dashboardService = inject(DashboardService);

  dashboardData = signal<InstructorDashboard | null>(null);
  courses = signal<Course[]>([]);
  selectedCourseId = signal<number | null>(null);
  gradeRows = signal<GradeRow[]>([]);
  isLoading = signal(false);
  isSubmittingBulk = signal(false);
  feedbackMessage = signal<string | null>(null);
  isFeedbackSuccess = signal(true);

  displayedColumns = ['student', 'status', 'numericScore', 'letterGrade', 'certificate', 'actions'];

  readonly pendingCount = computed(() =>
    this.gradeRows().filter((r) => !r.enrollment.grade && (r.enrollment.status === 'Active' || r.enrollment.status === 'Approved')).length
  );

  readonly issuedCount = computed(() =>
    this.gradeRows().filter((r) => r.isIssued || r.enrollment.status === 'Completed').length
  );

  ngOnInit(): void {
    const instructorId = this.authService.currentUser()?.instructorId || 1;
    this.loadDashboardData(instructorId);
    this.loadInstructorCourses(instructorId);
  }

  loadDashboardData(instructorId: number): void {
    this.dashboardService.getInstructorDashboard(instructorId).subscribe({
      next: (data) => this.dashboardData.set(data),
      error: () => {}
    });
  }

  loadInstructorCourses(instructorId: number): void {
    this.courseService.getCourses({ pageSize: 50 }).subscribe({
      next: (res) => {
        // If instructor has specific assigned courses, filter; otherwise keep available
        const assigned = res.items.filter(c => !c.instructorId || c.instructorId === instructorId);
        const listToUse = assigned.length > 0 ? assigned : res.items;
        this.courses.set(listToUse);
        if (listToUse.length > 0) {
          this.selectedCourseId.set(listToUse[0].id);
          this.loadRoster(listToUse[0].id);
        }
      }
    });
  }

  onCourseSelect(courseId: number): void {
    this.selectedCourseId.set(courseId);
    this.loadRoster(courseId);
  }

  loadRoster(courseId: number): void {
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

  onScoreChange(row: GradeRow): void {
    row.letterGrade = this.calculateGrade(row.numericScore);
  }

  calculateGrade(score: number): string {
    if (score >= 90) return 'A';
    if (score >= 80) return 'B';
    if (score >= 70) return 'C';
    if (score >= 60) return 'D';
    return 'F';
  }

  approveEnrollment(row: GradeRow): void {
    this.enrollmentService.approveEnrollment(row.enrollment.id).subscribe({
      next: () => {
        row.enrollment.status = 'Active';
        this.isFeedbackSuccess.set(true);
        this.feedbackMessage.set(`Enrollment approved for ${row.enrollment.studentName || 'Student'}!`);
      },
      error: (err) => {
        this.isFeedbackSuccess.set(false);
        this.feedbackMessage.set(err.message || 'Failed to approve enrollment.');
      }
    });
  }

  rejectEnrollment(row: GradeRow): void {
    this.enrollmentService.rejectEnrollment(row.enrollment.id).subscribe({
      next: () => {
        row.enrollment.status = 'Rejected';
        this.isFeedbackSuccess.set(true);
        this.feedbackMessage.set(`Enrollment rejected for ${row.enrollment.studentName || 'Student'}.`);
      },
      error: (err) => {
        this.isFeedbackSuccess.set(false);
        this.feedbackMessage.set(err.message || 'Failed to reject enrollment.');
      }
    });
  }

  saveSingleGrade(row: GradeRow): void {
    row.isSaving = true;
    this.gradeService.assignGrade({
      enrollmentId: row.enrollment.id,
      letterGrade: row.letterGrade,
      numericScore: row.numericScore
    }).subscribe({
      next: () => {
        row.isSaving = false;
        row.enrollment.grade = row.letterGrade;
        row.enrollment.numericScore = row.numericScore;
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

  submitAllGrades(): void {
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

  issueCertificate(row: GradeRow): void {
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
