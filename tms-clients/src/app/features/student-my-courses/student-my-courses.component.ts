import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { StudentCourseService } from '../../services/student-course.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { StudentEnrolledCourse, StudentCoursesStatistics } from '../../models/student-my-courses.model';

@Component({
  selector: 'app-student-my-courses',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatIconModule,
    MatButtonModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatSnackBarModule,
    LayoutComponent
  ],
  templateUrl: './student-my-courses.component.html',
  styleUrls: ['./student-my-courses.component.scss']
})
export class StudentMyCoursesComponent implements OnInit, OnDestroy {
  private studentCourseService = inject(StudentCourseService);
  private liveSyncService = inject(LiveSyncService);
  private snackBar = inject(MatSnackBar);
  private router = inject(Router);

  readonly courses = signal<StudentEnrolledCourse[]>([]);
  readonly statistics = signal<StudentCoursesStatistics | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly activeFilter = signal<'All' | 'Active' | 'Upcoming' | 'Completed'>('All');

  // Withdrawal modal state
  readonly confirmWithdrawCourse = signal<StudentEnrolledCourse | null>(null);
  readonly isSubmittingWithdrawal = signal(false);

  readonly filteredCourses = computed(() => {
    const filter = this.activeFilter();
    const list = this.courses();
    if (filter === 'All') return list;
    return list.filter(c => c.academicStatus === filter);
  });

  private syncSub?: Subscription;

  ngOnInit(): void {
    this.loadData();

    // Real-time synchronization via SignalR
    this.syncSub = this.liveSyncService.scheduleUpdated$.subscribe(() => {
      this.loadData(false);
    });

    this.syncSub.add(
      this.liveSyncService.enrollmentUpdated$.subscribe(() => {
        this.loadData(false);
      })
    );
  }

  ngOnDestroy(): void {
    this.syncSub?.unsubscribe();
  }

  loadData(showSpinner = true): void {
    if (showSpinner) {
      this.isLoading.set(true);
    }
    this.errorMessage.set(null);

    this.studentCourseService.getMyCourses().subscribe({
      next: (courses) => {
        this.courses.set(courses);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load student courses', err);
        this.errorMessage.set('Failed to load your enrolled courses. Please try again.');
        this.isLoading.set(false);
      }
    });

    this.studentCourseService.getMyCoursesStatistics().subscribe({
      next: (stats) => {
        this.statistics.set(stats);
      },
      error: (err) => {
        console.error('Failed to load course statistics', err);
      }
    });
  }

  setFilter(filter: 'All' | 'Active' | 'Upcoming' | 'Completed'): void {
    this.activeFilter.set(filter);
  }

  viewCourse(courseId: number): void {
    this.router.navigate(['/student/courses', courseId]);
  }

  browseCatalog(): void {
    this.router.navigate(['/student/catalog']);
  }

  /**
   * Students can only withdraw from a course before it starts.
   */
  canWithdraw(course: StudentEnrolledCourse): boolean {
    if (course.academicStatus !== 'Upcoming' || course.enrollmentStatus === 'Dropped') {
      return false;
    }
    if (!course.startDate) return false;
    const courseStart = new Date(course.startDate).setHours(0, 0, 0, 0);
    const now = new Date().setHours(0, 0, 0, 0);
    return courseStart > now;
  }

  openWithdrawConfirmation(course: StudentEnrolledCourse): void {
    this.confirmWithdrawCourse.set(course);
  }

  cancelWithdrawal(): void {
    if (!this.isSubmittingWithdrawal()) {
      this.confirmWithdrawCourse.set(null);
    }
  }

  executeWithdrawal(): void {
    const course = this.confirmWithdrawCourse();
    if (!course) return;

    this.isSubmittingWithdrawal.set(true);
    this.studentCourseService.withdrawFromCourse(course.courseId).subscribe({
      next: (res) => {
        this.isSubmittingWithdrawal.set(false);
        this.confirmWithdrawCourse.set(null);

        // Immediately remove course from local reactive signals without waiting or reloading
        this.courses.update(list => list.filter(c => c.courseId !== course.courseId));
        this.statistics.update(s => s ? {
          ...s,
          totalEnrolledCourses: Math.max(0, s.totalEnrolledCourses - 1),
          upcomingCourses: Math.max(0, s.upcomingCourses - 1)
        } : null);

        this.snackBar.open(res.message || `Successfully withdrawn from ${course.title}.`, 'Close', {
          duration: 5000,
          horizontalPosition: 'end',
          verticalPosition: 'top'
        });
        this.loadData(false);
      },
      error: (err) => {
        this.isSubmittingWithdrawal.set(false);
        const errorMsg = err.error?.message || 'Failed to withdraw from course. Please try again.';
        this.snackBar.open(errorMsg, 'Close', {
          duration: 6000,
          horizontalPosition: 'end',
          verticalPosition: 'top'
        });
      }
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Active': return 'status-active';
      case 'Upcoming': return 'status-upcoming';
      case 'Completed': return 'status-completed';
      case 'Withdrawn': return 'status-withdrawn';
      default: return 'status-default';
    }
  }

  getLevelClass(level: string): string {
    switch (level?.toLowerCase()) {
      case 'beginner': return 'badge-level-beginner';
      case 'intermediate': return 'badge-level-intermediate';
      case 'advanced': return 'badge-level-advanced';
      default: return 'badge-level-default';
    }
  }
}
