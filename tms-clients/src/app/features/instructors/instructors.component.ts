import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { InstructorService, InstructorItem, CreateInstructorDto } from '../../services/instructor.service';

@Component({
  selector: 'app-instructors-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    LayoutComponent
  ],
  templateUrl: './instructors.component.html',
  styleUrls: ['./instructors.component.scss']
})
export class InstructorsComponent implements OnInit, OnDestroy {
  private instructorService = inject(InstructorService);
  private router = inject(Router);

  readonly instructors = signal<InstructorItem[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly searchQuery = signal<string>('');
  readonly selectedDepartment = signal<string>('All');
  readonly currentPage = signal<number>(1);
  readonly pageSize = signal<number>(12);
  readonly totalCount = signal<number>(0);

  readonly showAddModal = signal<boolean>(false);
  readonly isSaving = signal<boolean>(false);
  readonly modalError = signal<string>('');

  readonly departments = [
    'All',
    'Design',
    'Web Dev',
    'Marketing',
    'Communication',
    'Business',
    'Language'
  ];

  newInstructor: CreateInstructorDto = {
    firstName: '',
    lastName: '',
    email: '',
    employeeNumber: '',
    department: 'Web Dev'
  };

  private searchSubject = new Subject<string>();
  private searchSub?: Subscription;

  readonly totalPages = computed(() => {
    const total = this.totalCount();
    const size = this.pageSize();
    return Math.max(1, Math.ceil(total / size));
  });

  readonly pagesArray = computed<number[]>(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages: number[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
    } else {
      pages.push(1);
      if (current > 3) pages.push(-1); // ellipsis
      const start = Math.max(2, current - 1);
      const end = Math.min(total - 1, current + 1);
      for (let i = start; i <= end; i++) {
        if (!pages.includes(i)) pages.push(i);
      }
      if (current < total - 2) pages.push(-1); // ellipsis
      if (!pages.includes(total)) pages.push(total);
    }

    return pages;
  });

  ngOnInit(): void {
    this.searchSub = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe((query) => {
      this.searchQuery.set(query);
      this.currentPage.set(1);
      this.loadData();
    });

    this.loadData();
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  loadData(): void {
    this.isLoading.set(true);

    this.instructorService.getInstructors({
      page: this.currentPage(),
      pageSize: this.pageSize(),
      search: this.searchQuery().trim() || undefined,
      department: this.selectedDepartment() !== 'All' ? this.selectedDepartment() : undefined
    }).subscribe({
      next: (res) => {
        this.instructors.set(res.items || []);
        this.totalCount.set(res.totalCount || 0);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load instructors:', err);
        this.instructors.set([]);
        this.totalCount.set(0);
        this.isLoading.set(false);
      }
    });
  }

  onSearchChange(val: string): void {
    this.searchSubject.next(val);
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.searchSubject.next('');
  }

  selectDepartment(dept: string): void {
    if (this.selectedDepartment() === dept) return;
    this.selectedDepartment.set(dept);
    this.currentPage.set(1);
    this.loadData();
  }

  resetFilters(): void {
    this.searchQuery.set('');
    this.selectedDepartment.set('All');
    this.currentPage.set(1);
    this.loadData();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.currentPage()) return;
    this.currentPage.set(page);
    this.loadData();
  }

  onPageSizeChange(newSize: number): void {
    this.pageSize.set(Number(newSize));
    this.currentPage.set(1);
    this.loadData();
  }

  viewInstructor(id: number): void {
    this.router.navigate(['/admin/instructors', id]);
  }

  getCoursesDisplay(inst: InstructorItem): string {
    if (inst.courseTitles && inst.courseTitles.length > 0) {
      return inst.courseTitles.join(', ');
    }
    if (inst.coursesCount > 0) {
      return `${inst.coursesCount} Courses Assigned`;
    }
    return 'No courses assigned';
  }

  onAvatarError(event: Event, inst: InstructorItem): void {
    const img = event.target as HTMLImageElement;
    if (img) {
      img.src = 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80';
    }
  }

  // ─── Add Instructor Modal ──────────────────────────────────────────────────
  openAddModal(): void {
    this.modalError.set('');
    this.newInstructor = {
      firstName: '',
      lastName: '',
      email: '',
      employeeNumber: `INS-2030-${String(this.totalCount() + 1).padStart(3, '0')}`,
      department: 'Web Dev'
    };
    this.showAddModal.set(true);
  }

  closeAddModal(): void {
    this.showAddModal.set(false);
    this.modalError.set('');
  }

  submitNewInstructor(): void {
    if (!this.newInstructor.firstName?.trim() || !this.newInstructor.lastName?.trim() || !this.newInstructor.email?.trim()) {
      this.modalError.set('Please fill in all required fields.');
      return;
    }

    this.isSaving.set(true);
    this.modalError.set('');

    this.instructorService.createInstructor(this.newInstructor).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeAddModal();
        this.currentPage.set(1);
        this.loadData();
      },
      error: (err) => {
        this.isSaving.set(false);
        let errorMsg = err.error?.message;
        if (!errorMsg && Array.isArray(err.error?.errors) && err.error.errors.length > 0) {
          errorMsg = err.error.errors.map((e: any) => e.error || e.message || e).join(' ');
        }
        this.modalError.set(errorMsg || 'Failed to create instructor. Please try again.');
      }
    });
  }
}
