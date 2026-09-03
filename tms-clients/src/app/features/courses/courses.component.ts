import { Component, inject, OnInit, signal, computed, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { CourseService } from '../../services/course.service';
import { AuthService } from '../../services/auth.service';
import { EnrollmentService } from '../../services/enrollment.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { Course } from '../../models/course.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-courses-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    LayoutComponent
  ],
  templateUrl: './courses.component.html',
  styleUrls: ['./courses.component.scss']
})
export class CoursesComponent implements OnInit {
  private courseService = inject(CourseService);
  private authService = inject(AuthService);
  private enrollmentService = inject(EnrollmentService);
  private snackBar = inject(MatSnackBar);
  private liveSync = inject(LiveSyncService);

  readonly isStudent = computed(() => this.authService.userRole() === 'Student');
  readonly courses = signal<Course[]>([]);
  readonly selectedStatus = signal<string>('All');
  readonly selectedCategory = signal<string>('All Category');
  readonly viewMode = signal<'list' | 'grid'>('grid');
  readonly selectedCourse = signal<Course | null>(null);
  readonly showDetailsDrawer = signal<boolean>(false);
  readonly isLoading = signal<boolean>(false);
  readonly isEnrolling = signal<boolean>(false);

  // Pagination
  currentPage = 1;
  pageSize = 8;
  totalCount = 320;

  private sub?: Subscription;

  ngOnInit(): void {
    this.loadCourses();

    // Subscribe to live course capacity changes
    this.sub = this.liveSync.courseCapacityChanged$.subscribe((event) => {
      this.courses.update(list => list.map(c => {
        if (c.id === event.courseId) {
          return {
            ...c,
            enrolledCount: event.enrolledCount,
            availableSeats: Math.max(0, event.maxCapacity - event.enrolledCount)
          };
        }
        return c;
      }));

      if (this.selectedCourse()?.id === event.courseId) {
        this.selectedCourse.update(c => c ? {
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

  loadCourses(): void {
    this.isLoading.set(true);
    const filter = {
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
      status: this.selectedStatus() === 'All' ? undefined : this.selectedStatus(),
      category: this.selectedCategory() === 'All Category' ? undefined : this.selectedCategory()
    };

    const call$ = this.isStudent()
      ? this.courseService.getCatalog(filter)
      : this.courseService.getCourses(filter);

    call$.subscribe({
      next: (res) => {
        this.courses.set(res.items);
        if (res.totalCount) this.totalCount = res.totalCount;
        this.isLoading.set(false);

        if (!this.selectedCourse() && res.items.length > 0) {
          const python = res.items.find(c => c.title.includes('Python')) || res.items[0];
          this.selectedCourse.set(python);
        }
      },
      error: () => this.isLoading.set(false)
    });
  }

  enrollInCourse(course: Course): void {
    const studentId = this.authService.currentUser()?.studentId || 1;
    this.isEnrolling.set(true);

    this.enrollmentService.enrollStudent({
      studentId: studentId,
      courseId: course.id,
      fee: course.price,
      batch: 'Batch 1'
    }).subscribe({
      next: () => {
        this.isEnrolling.set(false);
        this.snackBar.open(`Successfully registered for ${course.title}!`, 'Close', { duration: 4000 });
        course.isStudentEnrolled = true;
        course.studentEnrollmentStatus = 'Active';
        this.selectedCourse.set({ ...course });
        this.loadCourses();
      },
      error: (err) => {
        this.isEnrolling.set(false);
        const msg = err.error?.message || 'Enrollment request failed.';
        this.snackBar.open(msg, 'Close', { duration: 4000 });
      }
    });
  }

  filterStatus(status: string): void {
    this.selectedStatus.set(status);
    this.currentPage = 1;
    this.loadCourses();
  }

  filterCategory(cat: string): void {
    this.selectedCategory.set(cat);
    this.currentPage = 1;
    this.loadCourses();
  }

  toggleViewMode(mode: 'list' | 'grid'): void {
    this.viewMode.set(mode);
  }

  openCourseDetails(course: Course): void {
    this.selectedCourse.set(course);
    this.showDetailsDrawer.set(true);
  }

  closeCourseDetails(): void {
    this.showDetailsDrawer.set(false);
  }

  setPage(p: number): void {
    if (p < 1) return;
    this.currentPage = p;
    this.loadCourses();
  }

  getCategoryClass(category?: string): string {
    const cat = (category || 'design').toLowerCase();
    if (cat.includes('web')) return 'category-webdev';
    if (cat.includes('market')) return 'category-marketing';
    if (cat.includes('commun')) return 'category-communication';
    if (cat.includes('busi')) return 'category-business';
    if (cat.includes('well')) return 'category-wellness';
    if (cat.includes('lang')) return 'category-language';
    return 'category-design';
  }

  getStatusClass(status?: string): string {
    const s = (status || 'active').toLowerCase();
    if (s === 'active') return 'active';
    if (s === 'draft') return 'draft';
    if (s === 'archived') return 'archived';
    return 'active';
  }

  downloadResource(resourceName: string): void {
    alert(`Starting download for ${resourceName}...`);
  }
}
