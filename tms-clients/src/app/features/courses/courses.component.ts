import { Component, inject, OnInit, signal, computed, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
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
import { Subscription, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

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
export class CoursesComponent implements OnInit, OnDestroy {
  private router = inject(Router);
  private courseService = inject(CourseService);
  private authService = inject(AuthService);
  private enrollmentService = inject(EnrollmentService);
  private snackBar = inject(MatSnackBar);
  private liveSync = inject(LiveSyncService);

  readonly isStudent = computed(() => this.authService.userRole() === 'Student');
  readonly courses = signal<Course[]>([]);
  readonly selectedStatus = signal<string>('All');
  readonly selectedCategory = signal<string>('All Category');
  readonly selectedSkillLevel = signal<string>('All');
  readonly selectedSubject = signal<string>('All');
  readonly selectedAvailability = signal<string>('All');

  readonly searchQuery = signal<string>('');
  private searchSubject = new Subject<string>();
  private searchSub?: Subscription;

  readonly viewMode = signal<'list' | 'grid'>('grid');
  readonly selectedCourse = signal<Course | null>(null);
  readonly showDetailsDrawer = signal<boolean>(false);
  readonly isLoading = signal<boolean>(false);
  readonly isEnrolling = signal<boolean>(false);


  // Pagination
  currentPage = 1;
  pageSize = 8;
  totalCount = 0;

  readonly availableSubjects = computed(() => {
    const set = new Set<string>(['Computer Science', 'Web Development', 'Design', 'Marketing', 'Communication', 'Language']);
    for (const c of this.courses()) {
      if (c.department) set.add(c.department);
      if (c.category) set.add(c.category);
    }
    return Array.from(set).sort();
  });

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount / this.pageSize)));
  readonly pageNumbers = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage;
    const pages: number[] = [];
    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  });

  readonly topCourses = computed(() => {
    const list = [...this.courses()];
    return list.sort((a, b) => (b.enrolledCount ?? 0) - (a.enrolledCount ?? 0)).slice(0, 3);
  });

  readonly totalCapacity = computed(() => this.courses().reduce((sum, c) => sum + (c.maxCapacity ?? 0), 0));
  readonly totalEnrolled = computed(() => this.courses().reduce((sum, c) => sum + (c.enrolledCount ?? 0), 0));

  readonly categoryCounts = computed(() => {
    const list = this.courses();
    const map = new Map<string, number>();
    for (const c of list) {
      const cat = c.category || 'General';
      map.set(cat, (map.get(cat) || 0) + 1);
    }
    const colors = ['sq-design', 'sq-webdev', 'sq-marketing', 'sq-business', 'sq-others'];
    return Array.from(map.entries()).map(([name, count], idx) => ({
      name,
      count,
      colorClass: colors[idx % colors.length],
      percent: list.length > 0 ? Math.round((count / list.length) * 100) : 0
    }));
  });

  private sub?: Subscription;

  ngOnInit(): void {
    this.searchSub = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe((query) => {
      this.searchQuery.set(query);
      this.currentPage = 1;
      this.loadCourses();
    });

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
    this.searchSub?.unsubscribe();
  }

  onSearchInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.searchSubject.next(val);
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.currentPage = 1;
    this.loadCourses();
  }

  loadCourses(): void {
    this.isLoading.set(true);

    const filter: any = {
      page: this.currentPage,
      pageSize: this.pageSize
    };

    if (this.searchQuery()) {
      filter.searchTerm = this.searchQuery();
    }

    if (this.isStudent()) {
      if (this.selectedSkillLevel() !== 'All' && this.selectedSkillLevel() !== 'All Levels') {
        filter.skillLevel = this.selectedSkillLevel();
      }
      if (this.selectedSubject() !== 'All' && this.selectedSubject() !== 'All Subjects') {
        filter.subjectMatter = this.selectedSubject();
      }
      if (this.selectedAvailability() !== 'All') {
        filter.availability = this.selectedAvailability();
      }
    } else {
      if (this.selectedCategory() !== 'All' && this.selectedCategory() !== 'All Category') {
        filter.category = this.selectedCategory();
      }
      if (this.selectedStatus() !== 'All') {
        filter.status = this.selectedStatus();
      }
    }

    const call$ = this.isStudent()
      ? this.courseService.getCatalog(filter)
      : this.courseService.getCourses(filter);

    call$.subscribe({
      next: (res) => {
        this.courses.set(res.items);
        if (res.totalCount !== undefined) this.totalCount = res.totalCount;
        this.isLoading.set(false);

        if (!this.selectedCourse() && res.items.length > 0) {
          this.selectedCourse.set(res.items[0]);
        }
      },
      error: () => this.isLoading.set(false)
    });
  }


  enrollInCourse(course: Course): void {
    const studentId = this.authService.currentUser()?.studentId || 0;
    this.isEnrolling.set(true);

    this.enrollmentService.enrollStudent({
      studentId: studentId,
      courseId: course.id,
      batch: 'Batch 1',
      status: 'Active'
    }).subscribe({
      next: () => {
        this.isEnrolling.set(false);
        this.snackBar.open(`Successfully registered for ${course.title}!`, 'Close', { duration: 4000 });

        const currentEnrolled = course.enrolledCount ?? 0;
        const newEnrolled = currentEnrolled + 1;
        const newAvailable = Math.max(0, (course.maxCapacity ?? 30) - newEnrolled);

        this.courses.update(list => list.map(c => c.id === course.id ? {
          ...c,
          isStudentEnrolled: true,
          studentEnrollmentStatus: 'Active',
          enrolledCount: newEnrolled,
          availableSeats: newAvailable
        } : c));
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
          msg = 'Enrollment failed. Please try again.';
        }
        this.snackBar.open(msg, 'Close', { duration: 4000 });
      }
    });
  }

  filterSkillLevel(level: string): void {
    this.selectedSkillLevel.set(level);
    this.currentPage = 1;
    this.loadCourses();
  }

  filterSubject(subject: string): void {
    this.selectedSubject.set(subject);
    this.currentPage = 1;
    this.loadCourses();
  }

  filterAvailability(availability: string): void {
    this.selectedAvailability.set(availability);
    this.currentPage = 1;
    this.loadCourses();
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
    const role = this.authService.userRole();
    if (role === 'Admin') {
      this.router.navigate(['/admin/courses', course.id]);
    } else if (role === 'Instructor') {
      this.router.navigate(['/instructor/courses', course.id]);
    } else {
      this.router.navigate(['/student/courses', course.id]);
    }
  }

  viewCourse(course: Course, event?: Event): void {
    if (event) event.stopPropagation();
    this.openCourseDetails(course);
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
    if (s === 'active' || s === 'published') return 'active';
    if (s === 'under review') return 'under-review';
    if (s === 'rejected') return 'rejected';
    if (s === 'draft') return 'draft';
    if (s === 'archived') return 'archived';
    return 'active';
  }

  downloadResource(resourceName: string): void {
    alert(`Starting download for ${resourceName}...`);
  }
}
