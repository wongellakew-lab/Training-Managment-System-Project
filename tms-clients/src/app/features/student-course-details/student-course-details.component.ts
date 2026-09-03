import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { StudentCourseService } from '../../services/student-course.service';
import { AuthService } from '../../services/auth.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { Course } from '../../models/course.model';

@Component({
  selector: 'app-student-course-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LayoutComponent,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './student-course-details.component.html',
  styleUrls: ['./student-course-details.component.scss']
})
export class StudentCourseDetailsComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private courseService = inject(StudentCourseService);
  private authService = inject(AuthService);
  private liveSync = inject(LiveSyncService);
  private snackBar = inject(MatSnackBar);

  readonly course = signal<Course | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly isNotFound = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEnrolling = signal<boolean>(false);

  // Set of expanded module IDs for the collapsible accordion
  readonly expandedModules = signal<Set<number>>(new Set<number>());

  private sub?: Subscription;

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

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam || isNaN(+idParam)) {
      this.isNotFound.set(true);
      this.isLoading.set(false);
      return;
    }

    const courseId = parseInt(idParam, 10);
    this.loadCourse(courseId);

    // Live SignalR synchronization
    this.sub = this.liveSync.courseCapacityChanged$.subscribe((event) => {
      if (this.course()?.id === event.courseId) {
        this.course.update(c => c ? {
          ...c,
          enrolledCount: event.enrolledCount,
          availableSeats: Math.max(0, event.maxCapacity - event.enrolledCount)
        } : null);
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  loadCourse(courseId: number): void {
    this.isLoading.set(true);
    this.isNotFound.set(false);
    this.errorMessage.set(null);

    this.courseService.getCourseDetails(courseId).subscribe({
      next: (c) => {
        this.course.set(c);
        this.isLoading.set(false);

        // Auto-expand the first module if available
        if (c.modules && c.modules.length > 0) {
          const firstModId = c.modules[0].id;
          this.expandedModules.set(new Set([firstModId]));
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        if (err.status === 404) {
          this.isNotFound.set(true);
        } else {
          this.errorMessage.set(err?.error?.message || 'Unable to load course details.');
        }
      }
    });
  }

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

  enroll(): void {
    const c = this.course();
    if (!c) return;

    const studentId = this.authService.currentUser()?.studentId || 0;
    if (!studentId) {
      this.snackBar.open('You must be logged in as an authenticated student to enroll.', 'Close', { duration: 4000 });
      return;
    }

    this.isEnrolling.set(true);

    this.courseService.enrollInCourse(c.id, studentId).subscribe({
      next: () => {
        this.isEnrolling.set(false);
        this.snackBar.open(`Successfully enrolled in ${c.title}!`, 'Close', { duration: 4000 });

        const newEnrolled = (c.enrolledCount || 0) + 1;
        const newAvailable = Math.max(0, c.maxCapacity - newEnrolled);

        this.course.update(cur => cur ? {
          ...cur,
          isStudentEnrolled: true,
          studentEnrollmentStatus: 'Active',
          enrolledCount: newEnrolled,
          availableSeats: newAvailable
        } : null);
      },
      error: (err) => {
        this.isEnrolling.set(false);
        let msg = err?.error?.message;
        if (!msg && err?.error?.errors) {
          const firstKey = Object.keys(err.error.errors)[0];
          msg = err.error.errors[firstKey]?.[0];
        }
        if (!msg && err?.error?.title) {
          msg = err.error.title;
        }
        if (!msg) {
          msg = 'Enrollment request failed. Course might be full or prerequisite missing.';
        }
        this.snackBar.open(msg, 'Close', { duration: 5000 });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/student/catalog']);
  }

  getFileIcon(fileType: string): string {
    const type = (fileType || '').toLowerCase();
    if (type.includes('pdf')) return 'picture_as_pdf';
    if (type.includes('doc')) return 'description';
    if (type.includes('xls') || type.includes('sheet')) return 'table_chart';
    if (type.includes('ppt')) return 'slideshow';
    return 'insert_drive_file';
  }
}
