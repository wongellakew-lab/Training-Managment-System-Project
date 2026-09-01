import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { NavbarComponent } from '../../ui/navbar/navbar.component';
import { CourseCardComponent } from '../../ui/course-card/course-card.component';
import { CourseStore } from '../../store/course.store';
import { EnrollmentStore } from '../../store/enrollment.store';
import { AuthService } from '../../services/auth.service';
import { Course } from '../../models/course.model';
import { LiveSyncService } from '../../services/live-sync.service';

@Component({
  selector: 'app-course-browser',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, NavbarComponent, CourseCardComponent],
  template: `
    <div class="min-h-screen bg-slate-950 px-4 sm:px-8 py-6 max-w-7xl mx-auto">
      <app-navbar></app-navbar>

      <!-- Header & Search Controls -->
      <div class="glass-panel p-6 sm:p-8 mb-8">
        <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
          <div>
            <h1 class="text-2xl sm:text-3xl font-black text-white tracking-tight">Course Catalog</h1>
            <p class="text-xs text-slate-400 mt-1">Explore and enroll in available semester courses</p>
          </div>

          <!-- HybridCache Toggle & Version Switcher -->
          <div class="flex items-center gap-2 bg-slate-900/80 p-1.5 rounded-xl border border-white/5">
            <button
              (click)="useCachedVersion.set(false); searchCourses()"
              class="px-3 py-1.5 text-xs font-semibold rounded-lg transition-colors"
              [ngClass]="!useCachedVersion() ? 'bg-indigo-600 text-white shadow-sm' : 'text-slate-400 hover:text-slate-200'"
            >
              V1 (Live DB)
            </button>
            <button
              (click)="useCachedVersion.set(true); searchCourses()"
              class="px-3 py-1.5 text-xs font-semibold rounded-lg transition-colors flex items-center gap-1"
              [ngClass]="useCachedVersion() ? 'bg-indigo-600 text-white shadow-sm' : 'text-slate-400 hover:text-slate-200'"
            >
              <span class="material-icons text-xs">bolt</span>
              <span>V2 (HybridCache)</span>
            </button>
          </div>
        </div>

        <!-- Filter Bar -->
        <div class="grid grid-cols-1 sm:grid-cols-12 gap-3">
          <div class="sm:col-span-6 relative">
            <span class="material-icons absolute left-3 top-2.5 text-slate-400 text-sm">search</span>
            <input
              type="text"
              [(ngModel)]="searchTerm"
              (keyup.enter)="searchCourses()"
              placeholder="Search course title or code..."
              class="form-input pl-9 text-xs"
            />
          </div>

          <div class="sm:col-span-4">
            <select [(ngModel)]="departmentFilter" (change)="searchCourses()" class="form-select text-xs">
              <option value="">All Departments</option>
              <option value="Computer Science">Computer Science</option>
              <option value="Engineering">Engineering</option>
              <option value="Business">Business</option>
              <option value="Mathematics">Mathematics</option>
            </select>
          </div>

          <div class="sm:col-span-2">
            <button (click)="searchCourses()" class="btn-primary w-full text-xs py-2">
              Apply Filters
            </button>
          </div>
        </div>
      </div>

      <!-- Success & Error Alert -->
      @if (alertMessage()) {
        <div
          class="mb-6 p-4 rounded-xl flex items-center justify-between text-xs"
          [ngClass]="isSuccessAlert() ? 'bg-emerald-500/15 border border-emerald-500/30 text-emerald-300' : 'bg-rose-500/15 border border-rose-500/30 text-rose-300'"
        >
          <div class="flex items-center gap-2">
            <span class="material-icons text-base">{{ isSuccessAlert() ? 'check_circle' : 'error' }}</span>
            <span>{{ alertMessage() }}</span>
          </div>
          <button (click)="alertMessage.set(null)" class="material-icons text-sm hover:opacity-80">close</button>
        </div>
      }

      <!-- Course Cards Grid -->
      @if (courseStore.isLoading()) {
        <div class="py-16 text-center text-slate-400">
          <span class="inline-block animate-spin w-8 h-8 border-2 border-indigo-500 border-t-transparent rounded-full mb-3"></span>
          <div class="text-sm font-medium">Loading catalog courses...</div>
        </div>
      } @else if (courseStore.courses().length === 0) {
        <div class="glass-panel p-12 text-center">
          <span class="material-icons text-4xl text-slate-600 mb-2">search_off</span>
          <h3 class="text-base font-bold text-slate-300">No courses match your query</h3>
          <p class="text-xs text-slate-500 mt-1">Try resetting search filters or departments.</p>
        </div>
      } @else {
        <div class="grid-cards mb-8">
          @for (course of courseStore.courses(); track course.id) {
            <app-course-card
              [course]="course"
              actionLabel="Enroll"
              buttonClass="btn-primary text-xs py-1.5 px-3"
              (actionClicked)="onEnrollCourse($event)"
            ></app-course-card>
          }
        </div>

        <!-- Pagination Controls -->
        <div class="flex items-center justify-between glass-panel px-6 py-3 border border-white/5">
          <div class="text-xs text-slate-400">
            Page <span class="font-bold text-slate-200">{{ courseStore.pageNumber() }}</span> of <span class="font-bold text-slate-200">{{ courseStore.totalPages() }}</span>
            ({{ courseStore.totalCount() }} courses total)
          </div>
          <div class="flex items-center gap-2">
            <button
              (click)="changePage(courseStore.pageNumber() - 1)"
              [disabled]="courseStore.pageNumber() <= 1"
              class="btn-secondary text-xs py-1.5 px-3"
            >
              Previous
            </button>
            <button
              (click)="changePage(courseStore.pageNumber() + 1)"
              [disabled]="courseStore.pageNumber() >= courseStore.totalPages()"
              class="btn-secondary text-xs py-1.5 px-3"
            >
              Next
            </button>
          </div>
        </div>
      }
    </div>
  `
})
export class CourseBrowserComponent implements OnInit {
  courseStore = inject(CourseStore);
  enrollmentStore = inject(EnrollmentStore);
  authService = inject(AuthService);
  liveSync = inject(LiveSyncService);
  router = inject(Router);

  searchTerm = '';
  departmentFilter = '';
  useCachedVersion = signal(false);

  alertMessage = signal<string | null>(null);
  isSuccessAlert = signal(true);

  ngOnInit() {
    this.searchCourses();

    // Listen to real-time capacity updates
    this.liveSync.courseCapacityChanged$.subscribe((evt) => {
      this.courseStore.updateCapacity(evt.courseId, evt.enrolledCount, evt.maxCapacity);
    });
  }

  searchCourses() {
    const params = {
      pageNumber: 1,
      pageSize: 9,
      search: this.searchTerm || undefined,
      department: this.departmentFilter || undefined
    };

    if (this.useCachedVersion()) {
      this.courseStore.loadCachedCourses(params);
    } else {
      this.courseStore.loadCourses(params);
    }
  }

  changePage(newPage: number) {
    const params = {
      pageNumber: newPage,
      pageSize: 9,
      search: this.searchTerm || undefined,
      department: this.departmentFilter || undefined
    };

    if (this.useCachedVersion()) {
      this.courseStore.loadCachedCourses(params);
    } else {
      this.courseStore.loadCourses(params);
    }
  }

  async onEnrollCourse(course: Course) {
    const studentId = this.authService.currentUser()?.studentId || 1;
    try {
      await this.enrollmentStore.enroll({
        studentId,
        courseId: course.id
      });
      this.isSuccessAlert.set(true);
      this.alertMessage.set(`Successfully enrolled in ${course.title} (${course.code})!`);
      this.searchCourses(); // refresh counts
    } catch (err: any) {
      this.isSuccessAlert.set(false);
      this.alertMessage.set(err.message || 'Enrollment failed. Course may be at maximum capacity.');
    }
  }
}
