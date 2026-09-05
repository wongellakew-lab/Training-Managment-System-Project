import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { StudentService, StudentItem, StudentStats } from '../../services/student.service';

@Component({
  selector: 'app-students-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    LayoutComponent
  ],
  template: `
    <app-layout pageTitle="Students">
      <div class="students-page-content">
        <!-- Top Stats Row -->
        <div class="students-stats-row">
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle blue"><mat-icon>school</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Total Students</span>
              <span class="stat-num">{{ stats()?.totalStudents ?? 0 }}</span>
            </div>
          </div>
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle green"><mat-icon>how_to_reg</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Active Enrolled</span>
              <span class="stat-num">{{ stats()?.activeEnrolled ?? 0 }}</span>
            </div>
          </div>
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle cyan"><mat-icon>military_tech</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Certificates Issued</span>
              <span class="stat-num">{{ stats()?.certificatesIssued ?? 0 }}</span>
            </div>
          </div>
        </div>

        <!-- Students Directory Card with Search & Table -->
        <div class="eduva-card table-card">
          <div class="table-header-toolbar">
            <div class="title-group">
              <h3 class="table-title">Student Directory</h3>
              <span class="total-badge font-mono">{{ totalCount() }} Students</span>
            </div>

            <!-- Real-time Debounced Search Field -->
            <div class="search-input-box">
              <mat-icon class="search-icon">search</mat-icon>
              <input
                type="text"
                class="search-field"
                placeholder="Search students..."
                [value]="searchQuery()"
                (input)="onSearchInput($event)"
              />
              @if (searchQuery()) {
                <button class="clear-btn" (click)="clearSearch()" aria-label="Clear Search">
                  <mat-icon>close</mat-icon>
                </button>
              }
            </div>
          </div>

          @if (isLoading()) {
            <div class="loading-box">
              <mat-spinner diameter="36"></mat-spinner>
              <span>Loading student directory...</span>
            </div>
          } @else if (students().length === 0) {
            <div class="empty-box">
              <mat-icon>school</mat-icon>
              <h4>{{ searchQuery() ? 'No Students Found' : 'No Students Registered' }}</h4>
              <p>
                {{ searchQuery()
                  ? 'No students matched your search criteria. Try a different query.'
                  : 'Registered students will appear in this directory with their academic progress.' }}
              </p>
              @if (searchQuery()) {
                <button mat-stroked-button color="primary" (click)="clearSearch()" class="reset-search-btn">
                  Clear Search Filter
                </button>
              }
            </div>
          } @else {
            <div class="table-responsive">
              <table class="students-table">
                <thead>
                  <tr>
                    <th class="sortable-col" (click)="toggleSort('name')" title="Sort by Student Name">
                      <div class="th-content">
                        <span>Student</span>
                        <mat-icon class="sort-icon" [class.active-sort]="sortBy() === 'name'">
                          {{ sortBy() === 'name' ? (sortDescending() ? 'arrow_downward' : 'arrow_upward') : 'unfold_more' }}
                        </mat-icon>
                      </div>
                    </th>
                    <th class="sortable-col" (click)="toggleSort('studentid')" title="Sort by Student ID / Reg Number">
                      <div class="th-content">
                        <span>Reg Number</span>
                        <mat-icon class="sort-icon" [class.active-sort]="sortBy() === 'studentid'">
                          {{ sortBy() === 'studentid' ? (sortDescending() ? 'arrow_downward' : 'arrow_upward') : 'unfold_more' }}
                        </mat-icon>
                      </div>
                    </th>
                    <th class="sortable-col" (click)="toggleSort('coursesenrolled')" title="Sort by Enrolled Courses">
                      <div class="th-content">
                        <span>Enrolled Courses</span>
                        <mat-icon class="sort-icon" [class.active-sort]="sortBy() === 'coursesenrolled'">
                          {{ sortBy() === 'coursesenrolled' ? (sortDescending() ? 'arrow_downward' : 'arrow_upward') : 'unfold_more' }}
                        </mat-icon>
                      </div>
                    </th>
                    <th class="sortable-col" (click)="toggleSort('gpa')" title="Sort by GPA">
                      <div class="th-content">
                        <span>GPA</span>
                        <mat-icon class="sort-icon" [class.active-sort]="sortBy() === 'gpa'">
                          {{ sortBy() === 'gpa' ? (sortDescending() ? 'arrow_downward' : 'arrow_upward') : 'unfold_more' }}
                        </mat-icon>
                      </div>
                    </th>
                    <th>Status</th>
                    <th class="actions-col">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  @for (s of students(); track s.id) {
                    <tr class="clickable-row" (click)="viewStudent(s.id)">
                      <td>
                        <div class="student-cell">
                          <img [src]="s.avatar" alt="Avatar" class="avatar-img" />
                          <div class="info-box">
                            <span class="name">{{ s.firstName }} {{ s.lastName }}</span>
                            <span class="email">{{ s.email }}</span>
                          </div>
                        </div>
                      </td>
                      <td>
                        <span class="reg-number-pill font-mono">{{ s.registrationNumber }}</span>
                      </td>
                      <td>
                        <strong>{{ s.coursesEnrolled }} {{ s.coursesEnrolled === 1 ? 'Course' : 'Courses' }}</strong>
                      </td>
                      <td>
                        <span class="gpa-pill font-mono font-bold">{{ s.gpa.toFixed(2) }}</span>
                      </td>
                      <td>
                        <span class="pill-badge" [ngClass]="s.status.toLowerCase()">&bull; {{ s.status }}</span>
                      </td>
                      <td class="actions-cell" (click)="$event.stopPropagation()">
                        <button
                          mat-icon-button
                          [matMenuTriggerFor]="studentActionMenu"
                          [matMenuTriggerData]="{ student: s }"
                          class="action-menu-btn"
                          aria-label="Student actions"
                        >
                          <mat-icon>more_vert</mat-icon>
                        </button>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>

              <mat-menu #studentActionMenu="matMenu">
                <ng-template matMenuContent let-student="student">
                  <button mat-menu-item (click)="viewStudent(student.id)">
                    <mat-icon>visibility</mat-icon>
                    <span>View Details</span>
                  </button>

                  @if (student.status === 'Suspended') {
                    <button mat-menu-item (click)="activateStudent(student)">
                      <mat-icon class="icon-green">lock_open</mat-icon>
                      <span>Activate Account</span>
                    </button>
                  } @else {
                    @if (student.status !== 'Probation') {
                      <button mat-menu-item (click)="placeOnProbation(student)">
                        <mat-icon class="icon-orange">warning</mat-icon>
                        <span>Place on Probation</span>
                      </button>
                    } @else {
                      <button mat-menu-item (click)="activateStudent(student)">
                        <mat-icon class="icon-green">check_circle</mat-icon>
                        <span>Remove Probation</span>
                      </button>
                    }

                    <button mat-menu-item (click)="suspendStudent(student)">
                      <mat-icon class="icon-rose">block</mat-icon>
                      <span>Suspend Student</span>
                    </button>
                  }
                </ng-template>
              </mat-menu>
            </div>

            <!-- Server-Side Pagination Bar -->
            <div class="table-pagination-footer">
              <div class="pagination-info">
                Showing <strong>{{ paginationStart() }}</strong> to <strong>{{ paginationEnd() }}</strong> of <strong>{{ totalCount() }}</strong> students
              </div>

              <div class="pagination-controls">
                <div class="page-size-selector">
                  <span>Show</span>
                  <select [value]="pageSize()" (change)="onPageSizeChange($event)" class="size-select">
                    <option [value]="5">5</option>
                    <option [value]="10">10</option>
                    <option [value]="25">25</option>
                    <option [value]="50">50</option>
                  </select>
                </div>

                <div class="page-nav-buttons">
                  <button
                    class="nav-btn prev-btn"
                    [disabled]="currentPage() <= 1"
                    (click)="setPage(currentPage() - 1)"
                    aria-label="Previous Page"
                  >
                    <mat-icon>chevron_left</mat-icon>
                  </button>

                  @for (p of pagesArray(); track $index) {
                    @if (p === -1) {
                      <span class="page-ellipsis">&hellip;</span>
                    } @else {
                      <button
                        class="page-num-btn"
                        [class.active]="p === currentPage()"
                        (click)="setPage(p)"
                      >
                        {{ p }}
                      </button>
                    }
                  }

                  <button
                    class="nav-btn next-btn"
                    [disabled]="currentPage() >= totalPages()"
                    (click)="setPage(currentPage() + 1)"
                    aria-label="Next Page"
                  >
                    <mat-icon>chevron_right</mat-icon>
                  </button>
                </div>
              </div>
            </div>
          }
        </div>
      </div>
    </app-layout>
  `,
  styles: [`
    .students-page-content {
      display: flex;
      flex-direction: column;
      gap: 24px;
      padding-bottom: 24px;
    }
    .students-stats-row {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 20px;
      @media (max-width: 900px) { grid-template-columns: 1fr; }
    }
    .stat-item {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 16px 20px;
      .stat-icon-circle {
        width: 44px;
        height: 44px;
        border-radius: var(--radius-md);
        display: flex;
        align-items: center;
        justify-content: center;
        &.blue { background: #e0f2fe; color: #0284c7; }
        &.green { background: #ecfdf5; color: #10b981; }
        &.cyan { background: #ecfeff; color: #0891b2; }
      }
      .stat-content {
        display: flex;
        flex-direction: column;
        .stat-lbl { font-size: 0.75rem; color: var(--text-muted); }
        .stat-num { font-size: 1.5rem; font-weight: 800; color: var(--text-primary); }
      }
    }
    .table-card {
      .table-header-toolbar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        flex-wrap: wrap;
        gap: 16px;
        margin-bottom: 20px;

        .title-group {
          display: flex;
          align-items: center;
          gap: 12px;

          .table-title {
            font-size: 1.125rem;
            font-weight: 700;
            color: var(--text-primary);
            margin: 0;
          }

          .total-badge {
            font-size: 0.6875rem;
            font-weight: 700;
            background: var(--bg-subtle, #f1f5f9);
            color: var(--text-secondary);
            padding: 3px 8px;
            border-radius: var(--radius-full);
          }
        }

        .search-input-box {
          position: relative;
          display: flex;
          align-items: center;
          width: 300px;
          max-width: 100%;

          .search-icon {
            position: absolute;
            left: 12px;
            font-size: 18px;
            width: 18px;
            height: 18px;
            color: var(--text-muted);
            pointer-events: none;
          }

          .search-field {
            width: 100%;
            height: 38px;
            padding: 0 36px 0 38px;
            border-radius: var(--radius-full);
            border: 1px solid var(--border-subtle, #cbd5e1);
            background: var(--bg-surface, #ffffff);
            color: var(--text-primary);
            font-size: 0.8125rem;
            transition: all 0.2s;

            &:focus {
              outline: none;
              border-color: var(--eduva-blue, #0284c7);
              box-shadow: 0 0 0 3px rgba(2, 132, 199, 0.15);
            }
          }

          .clear-btn {
            position: absolute;
            right: 8px;
            background: transparent;
            border: none;
            cursor: pointer;
            color: var(--text-muted);
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 4px;
            border-radius: 50%;

            mat-icon {
              font-size: 16px;
              width: 16px;
              height: 16px;
            }

            &:hover {
              color: var(--text-primary);
              background: var(--bg-subtle);
            }
          }
        }
      }

      .table-responsive {
        width: 100%;
        overflow-x: auto;
      }

      .students-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.8125rem;

        thead tr {
          border-bottom: 1px solid var(--border-subtle, #e2e8f0);
          th {
            text-align: left;
            padding: 12px 16px;
            color: var(--text-muted);
            font-weight: 600;

            &.sortable-col {
              cursor: pointer;
              user-select: none;
              transition: color 0.15s;

              &:hover {
                color: var(--text-primary);
              }

              .th-content {
                display: flex;
                align-items: center;
                gap: 6px;

                .sort-icon {
                  font-size: 16px;
                  width: 16px;
                  height: 16px;
                  color: var(--text-muted);
                  opacity: 0.6;
                  transition: opacity 0.15s, color 0.15s;

                  &.active-sort {
                    opacity: 1;
                    color: var(--eduva-blue, #0284c7);
                    font-weight: bold;
                  }
                }
              }
            }
          }
        }

        tbody tr {
          border-bottom: 1px solid var(--border-subtle, #f1f5f9);
          transition: background 0.15s;

          &.clickable-row {
            cursor: pointer;
          }

          &:hover {
            background: var(--bg-card-hover, #f8fafc);
          }

          td {
            padding: 14px 16px;
            color: var(--text-primary);

            &.actions-cell {
              text-align: right;

              .action-menu-btn {
                color: var(--text-muted);
                &:hover {
                  color: var(--text-primary);
                }
              }
            }
          }
        }
      }

      .student-cell {
        display: flex;
        align-items: center;
        gap: 12px;
        .avatar-img { width: 34px; height: 34px; border-radius: 50%; object-fit: cover; }
        .info-box {
          display: flex;
          flex-direction: column;
          .name { font-weight: 700; color: var(--text-primary); }
          .email { font-size: 0.6875rem; color: var(--text-muted); }
        }
      }

      .reg-number-pill {
        font-size: 0.75rem;
        background: var(--bg-subtle, #f1f5f9);
        color: var(--text-secondary);
        padding: 3px 8px;
        border-radius: 4px;
      }

      .gpa-pill {
        background: var(--bg-subtle, #f1f5f9);
        padding: 3px 8px;
        border-radius: 4px;
        color: var(--text-primary);
      }

      .pill-badge {
        display: inline-block;
        padding: 3px 8px;
        border-radius: var(--radius-full);
        font-size: 0.6875rem;
        font-weight: 600;
        &.active { background: #ecfdf5; color: #059669; }
        &.enrolled { background: #eff6ff; color: #2563eb; }
        &.probation { background: #fffbeb; color: #d97706; }
        &.suspended { background: #fef2f2; color: #dc2626; }
        &.inactive { background: #f1f5f9; color: #64748b; }
      }

      .icon-green { color: #10b981; }
      .icon-orange { color: #f59e0b; }
      .icon-rose { color: #ef4444; }

      .table-pagination-footer {
        display: flex;
        align-items: center;
        justify-content: space-between;
        flex-wrap: wrap;
        gap: 16px;
        padding: 18px 4px 4px 4px;
        border-top: 1px solid var(--border-subtle, #f1f5f9);
        margin-top: 8px;

        .pagination-info {
          font-size: 0.8125rem;
          color: var(--text-secondary);

          strong {
            color: var(--text-primary);
          }
        }

        .pagination-controls {
          display: flex;
          align-items: center;
          gap: 16px;

          .page-size-selector {
            display: flex;
            align-items: center;
            gap: 8px;
            font-size: 0.8125rem;
            color: var(--text-secondary);

            .size-select {
              padding: 4px 8px;
              border-radius: 6px;
              border: 1px solid var(--border-subtle, #cbd5e1);
              background: var(--bg-surface, #ffffff);
              color: var(--text-primary);
              font-size: 0.8125rem;
              cursor: pointer;

              &:focus {
                outline: none;
                border-color: var(--eduva-blue, #0284c7);
              }
            }
          }

          .page-nav-buttons {
            display: flex;
            align-items: center;
            gap: 4px;

            .nav-btn, .page-num-btn {
              min-width: 32px;
              height: 32px;
              border-radius: 6px;
              border: 1px solid var(--border-subtle, #e2e8f0);
              background: var(--bg-surface, #ffffff);
              color: var(--text-primary);
              font-size: 0.8125rem;
              font-weight: 600;
              display: flex;
              align-items: center;
              justify-content: center;
              cursor: pointer;
              transition: all 0.15s;

              &:hover:not(:disabled) {
                background: var(--bg-subtle, #f1f5f9);
                border-color: var(--eduva-blue, #0284c7);
              }

              &:disabled {
                opacity: 0.4;
                cursor: not-allowed;
              }

              &.active {
                background: var(--eduva-navy, #172b4d);
                color: #ffffff;
                border-color: var(--eduva-navy, #172b4d);
              }

              mat-icon {
                font-size: 18px;
                width: 18px;
                height: 18px;
              }
            }

            .page-ellipsis {
              padding: 0 4px;
              color: var(--text-muted);
            }
          }
        }
      }
    }
    .loading-box, .empty-box {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px 24px;
      gap: 12px;
      text-align: center;
      color: var(--text-muted);
      mat-icon { font-size: 40px; width: 40px; height: 40px; color: var(--text-muted); }
      h4 { margin: 0; color: var(--text-primary); font-size: 1.125rem; font-weight: 700; }
      p { margin: 0; font-size: 0.8125rem; }
      .reset-search-btn { margin-top: 8px; }
    }
  `]
})
export class StudentsComponent implements OnInit, OnDestroy {
  private studentService = inject(StudentService);
  private router = inject(Router);

  readonly students = signal<StudentItem[]>([]);
  readonly stats = signal<StudentStats | null>(null);
  readonly isLoading = signal(false);

  // Search, Sorting & Pagination State
  readonly searchQuery = signal<string>('');
  readonly sortBy = signal<string>('name');
  readonly sortDescending = signal<boolean>(false);
  readonly currentPage = signal<number>(1);
  readonly pageSize = signal<number>(10);
  readonly totalCount = signal<number>(0);

  private searchSubject = new Subject<string>();
  private searchSub?: Subscription;

  readonly totalPages = computed(() => {
    const total = this.totalCount();
    const size = this.pageSize();
    return Math.max(1, Math.ceil(total / size));
  });

  readonly paginationStart = computed(() => {
    if (this.totalCount() === 0) return 0;
    return (this.currentPage() - 1) * this.pageSize() + 1;
  });

  readonly paginationEnd = computed(() => {
    return Math.min(this.currentPage() * this.pageSize(), this.totalCount());
  });

  readonly pagesArray = computed<number[]>(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages: number[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
    } else {
      pages.push(1);
      if (current > 3) pages.push(-1);
      const start = Math.max(2, current - 1);
      const end = Math.min(total - 1, current + 1);
      for (let i = start; i <= end; i++) {
        if (!pages.includes(i)) pages.push(i);
      }
      if (current < total - 2) pages.push(-1);
      if (!pages.includes(total)) pages.push(total);
    }

    return pages;
  });

  ngOnInit(): void {
    this.searchSub = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe((term) => {
      this.searchQuery.set(term);
      this.currentPage.set(1);
      this.loadData();
    });

    this.loadData();
    this.loadStats();
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  onSearchInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.searchSubject.next(val);
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.currentPage.set(1);
    this.loadData();
  }

  toggleSort(column: string): void {
    if (this.sortBy() === column) {
      this.sortDescending.set(!this.sortDescending());
    } else {
      this.sortBy.set(column);
      this.sortDescending.set(false);
    }
    this.currentPage.set(1);
    this.loadData();
  }

  setPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.currentPage()) return;
    this.currentPage.set(page);
    this.loadData();
  }

  onPageSizeChange(event: Event): void {
    const size = parseInt((event.target as HTMLSelectElement).value, 10);
    this.pageSize.set(size);
    this.currentPage.set(1);
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);

    this.studentService.getStudents({
      page: this.currentPage(),
      pageSize: this.pageSize(),
      search: this.searchQuery(),
      sortBy: this.sortBy(),
      sortDescending: this.sortDescending()
    }).subscribe({
      next: (res) => {
        this.students.set(res.items);
        this.totalCount.set(res.totalCount ?? res.items.length);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadStats(): void {
    this.studentService.getStudentStats().subscribe({
      next: (st) => this.stats.set(st),
      error: () => {}
    });
  }

  viewStudent(id: number): void {
    if (id) {
      this.router.navigate(['/admin/students', id]);
    }
  }

  suspendStudent(student: StudentItem): void {
    if (!confirm(`Are you sure you want to suspend ${student.firstName} ${student.lastName}? This will prevent them from logging in.`)) {
      return;
    }

    this.studentService.suspendStudent(student.id).subscribe({
      next: () => {
        this.loadData();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to suspend student.');
      }
    });
  }

  placeOnProbation(student: StudentItem): void {
    if (!confirm(`Place ${student.firstName} ${student.lastName} on academic probation? Status will be updated without locking login.`)) {
      return;
    }

    this.studentService.placeOnProbation(student.id).subscribe({
      next: () => {
        this.loadData();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to place student on probation.');
      }
    });
  }

  activateStudent(student: StudentItem): void {
    if (!confirm(`Activate account for ${student.firstName} ${student.lastName}?`)) {
      return;
    }

    this.studentService.activateStudent(student.id).subscribe({
      next: () => {
        this.loadData();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to activate student.');
      }
    });
  }
}

