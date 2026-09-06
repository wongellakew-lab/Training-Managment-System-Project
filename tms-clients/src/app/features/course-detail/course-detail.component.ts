import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { CourseService } from '../../services/course.service';
import { EnrollmentService } from '../../services/enrollment.service';
import { AuthService } from '../../services/auth.service';
import { Course } from '../../models/course.model';
import { Enrollment } from '../../models/enrollment.model';

@Component({
  selector: 'app-course-detail',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    LayoutComponent,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressBarModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './course-detail.component.html',
  styleUrls: ['./course-detail.component.scss']
})
export class CourseDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private courseService = inject(CourseService);
  private enrollmentService = inject(EnrollmentService);
  authService = inject(AuthService);

  readonly course = signal<Course | null>(null);
  readonly userEnrollment = signal<Enrollment | null>(null);
  readonly isLoading = signal(true);
  readonly isEnrolling = signal(false);
  readonly enrollmentMessage = signal<string | null>(null);
  readonly isEnrollSuccess = signal(true);

  // Set of expanded module IDs for the collapsible accordion
  readonly expandedModules = signal<Set<number>>(new Set<number>());

  readonly isStudent = computed(() => this.authService.userRole() === 'Student');
  readonly isInstructor = computed(() => this.authService.userRole() === 'Instructor');
  readonly isAdmin = computed(() => this.authService.userRole() === 'Admin');
  readonly isInstructorOrAdmin = computed(() => this.authService.userRole() === 'Instructor' || this.authService.userRole() === 'Admin');

  readonly isFull = computed(() => {
    const c = this.course();
    if (!c) return false;
    return (c.enrolledCount || 0) >= (c.maxCapacity || 0);
  });

  readonly availableSeats = computed(() => {
    const c = this.course();
    if (!c) return 0;
    if (c.availableSeats !== undefined) return c.availableSeats;
    return Math.max(0, (c.maxCapacity || 0) - (c.enrolledCount || 0));
  });

  readonly capacityPercentage = computed(() => {
    const c = this.course();
    if (!c || !c.maxCapacity || c.maxCapacity === 0) return 0;
    return Math.min(100, Math.round(((c.enrolledCount || 0) / c.maxCapacity) * 100));
  });

  readonly hasCertificate = computed(() => {
    const c = this.course();
    if (!c || !c.featuresJson) return false;
    try {
      const parsed = typeof c.featuresJson === 'string' ? JSON.parse(c.featuresJson) : c.featuresJson;
      if (Array.isArray(parsed)) {
        return parsed.some((f: any) => typeof f === 'string' && f.toLowerCase().includes('certificate'));
      }
      if (typeof parsed === 'object' && parsed !== null) {
        return !!(parsed.certificate ?? parsed.hasCertificate ?? parsed.completionCertificate ?? parsed['Completion Certificate']);
      }
    } catch {
      return typeof c.featuresJson === 'string' && c.featuresJson.toLowerCase().includes('certificate');
    }
    return false;
  });

  toggleModule(moduleId: number): void {
    const current = new Set(this.expandedModules());
    if (current.has(moduleId)) {
      current.delete(moduleId);
    } else {
      current.add(moduleId);
    }
    this.expandedModules.set(current);
  }

  isModuleExpanded(moduleId: number): boolean {
    return this.expandedModules().has(moduleId);
  }

  getFileIcon(fileType: string): string {
    const type = (fileType || '').toLowerCase();
    if (type.includes('pdf')) return 'picture_as_pdf';
    if (type.includes('doc')) return 'description';
    if (type.includes('xls') || type.includes('sheet')) return 'table_chart';
    if (type.includes('ppt')) return 'slideshow';
    return 'insert_drive_file';
  }

  isApproving = signal<boolean>(false);
  isRejecting = signal<boolean>(false);
  showRejectModal = signal<boolean>(false);
  rejectionReasonInput = signal<string>('');
  rejectError = signal<string | null>(null);
  actionMessage = signal<{ text: string; isSuccess: boolean } | null>(null);

  openBuilder(): void {
    const c = this.course();
    if (c) {
      this.router.navigate(['/instructor/courses', c.id, 'builder']);
    }
  }

  approveCourse(): void {
    const c = this.course();
    if (!c) return;

    if (!confirm(`Are you sure you want to approve "${c.title}"? Once approved, it will be published and visible to students in the Course Catalog.`)) {
      return;
    }

    this.isApproving.set(true);
    this.actionMessage.set(null);

    this.courseService.approveCourse(c.id).subscribe({
      next: (updated) => {
        this.isApproving.set(false);
        this.course.update(curr => curr ? { ...curr, status: 'Active', rejectionReason: undefined } : null);
        this.actionMessage.set({
          text: 'Course approved successfully! It is now active and published in the student course catalog.',
          isSuccess: true
        });
      },
      error: (err) => {
        this.isApproving.set(false);
        const msg = err?.error?.message || err?.message || 'Failed to approve course. Ensure all required content and schedule entries are present.';
        this.actionMessage.set({ text: msg, isSuccess: false });
      }
    });
  }

  openRejectModal(): void {
    this.rejectionReasonInput.set('');
    this.rejectError.set(null);
    this.showRejectModal.set(true);
  }

  closeRejectModal(): void {
    this.showRejectModal.set(false);
  }

  onRejectionReasonInput(val: string): void {
    this.rejectionReasonInput.set(val);
  }

  confirmReject(): void {
    const c = this.course();
    const reason = this.rejectionReasonInput().trim();
    if (!reason || reason.length < 5) {
      this.rejectError.set('Please provide a specific rejection reason explaining what needs to be changed (min 5 characters).');
      return;
    }
    if (!c) return;

    this.isRejecting.set(true);
    this.rejectError.set(null);

    this.courseService.rejectCourse(c.id, reason).subscribe({
      next: () => {
        this.isRejecting.set(false);
        this.showRejectModal.set(false);
        this.course.update(curr => curr ? { ...curr, status: 'Rejected', rejectionReason: reason } : null);
        this.actionMessage.set({
          text: `Course "${c.title}" has been rejected. The instructor has been notified with the feedback provided.`,
          isSuccess: true
        });
      },
      error: (err) => {
        this.isRejecting.set(false);
        const msg = err?.error?.message || err?.message || 'Failed to reject course.';
        this.rejectError.set(msg);
      }
    });
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      const id = parseInt(idParam, 10);
      this.loadCourse(id);
    }
  }

  loadCourse(id: number): void {
    this.isLoading.set(true);
    this.courseService.getCourseById(id).subscribe({
      next: (c) => {
        this.course.set(c);
        if (c.modules && c.modules.length > 0) {
          this.expandedModules.set(new Set([c.modules[0].id]));
        }
        this.checkExistingEnrollment(id);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  checkExistingEnrollment(courseId: number): void {
    const studentId = this.authService.currentUser()?.studentId;
    if (!studentId) return;

    this.enrollmentService.getEnrollments(studentId, courseId).subscribe({
      next: (list) => {
        if (list && list.length > 0) {
          this.userEnrollment.set(list[0]);
        }
      },
      error: () => {}
    });
  }

  enroll(): void {
    const c = this.course();
    if (!c) return;

    const studentId = this.authService.currentUser()?.studentId || 0;
    if (studentId === 0) {
      this.enrollmentMessage.set('You must be logged in as a student to enroll.');
      this.isEnrollSuccess.set(false);
      return;
    }
    this.isEnrolling.set(true);
    this.enrollmentMessage.set(null);

    this.enrollmentService.enrollStudent({
      studentId: studentId,
      courseId: c.id,
      batch: 'Batch 1'
    }).subscribe({
      next: (res) => {
        this.userEnrollment.set(res);
        this.isEnrolling.set(false);
        this.isEnrollSuccess.set(true);
        this.enrollmentMessage.set('Enrollment submitted successfully! Awaiting instructor approval.');
      },
      error: (err) => {
        this.isEnrolling.set(false);
        this.isEnrollSuccess.set(false);
        this.enrollmentMessage.set(err.error?.message || err.message || 'Enrollment failed. You might already be enrolled.');
      }
    });
  }

  goBack(): void {
    const role = this.authService.userRole();
    if (role === 'Instructor') {
      this.router.navigate(['/instructor/courses']);
    } else if (role === 'Admin') {
      this.router.navigate(['/admin/courses']);
    } else {
      this.router.navigate(['/student/courses']);
    }
  }
}
