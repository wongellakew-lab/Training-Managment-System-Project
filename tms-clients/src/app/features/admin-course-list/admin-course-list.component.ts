import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { NavbarComponent } from '../../ui/navbar/navbar.component';
import { CourseService } from '../../services/course.service';
import { Course, CreateCourseDto, UpdateCourseDto } from '../../models/course.model';

@Component({
  selector: 'app-admin-course-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, NavbarComponent],
  template: `
    <div class="min-h-screen bg-slate-950 px-4 sm:px-8 py-6 max-w-7xl mx-auto">
      <app-navbar></app-navbar>

      <!-- Header & Action Controls -->
      <div class="glass-panel p-6 sm:p-8 mb-8 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div class="flex items-center gap-2 mb-2">
            <span class="badge badge-warning text-[10px]">Admin Administration</span>
            <span class="text-xs text-slate-400">Course Management</span>
          </div>
          <h1 class="text-3xl font-black text-white tracking-tight">System Courses</h1>
          <p class="text-xs text-slate-400 mt-1">Create, update, and manage academic courses across all departments.</p>
        </div>

        <button (click)="openCreateModal()" class="btn-primary text-xs py-2.5 px-4">
          <span class="material-icons text-sm">add</span>
          <span>Add New Course</span>
        </button>
      </div>

      <!-- Alert Banner -->
      @if (alertMessage()) {
        <div
          class="mb-6 p-4 rounded-xl flex items-center justify-between text-xs"
          [ngClass]="isSuccess() ? 'bg-emerald-500/15 border border-emerald-500/30 text-emerald-300' : 'bg-rose-500/15 border border-rose-500/30 text-rose-300'"
        >
          <div class="flex items-center gap-2">
            <span class="material-icons text-base">{{ isSuccess() ? 'check_circle' : 'error' }}</span>
            <span>{{ alertMessage() }}</span>
          </div>
          <button (click)="alertMessage.set(null)" class="material-icons text-sm">close</button>
        </div>
      }

      <!-- Courses Table -->
      <div class="glass-panel p-6 sm:p-8">
        @if (isLoading()) {
          <div class="py-16 text-center text-slate-400">
            <span class="inline-block animate-spin w-8 h-8 border-2 border-indigo-500 border-t-transparent rounded-full mb-3"></span>
            <div class="text-sm">Loading course database...</div>
          </div>
        } @else {
          <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse">
              <thead>
                <tr class="border-b border-white/10 text-[11px] uppercase tracking-wider text-slate-400 font-semibold">
                  <th class="py-3 px-4">Code</th>
                  <th class="py-3 px-4">Title & Department</th>
                  <th class="py-3 px-4 text-center">Credits</th>
                  <th class="py-3 px-4 text-center">Enrolled / Cap</th>
                  <th class="py-3 px-4">Instructor</th>
                  <th class="py-3 px-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-white/5 text-xs text-slate-300">
                @for (c of courses(); track c.id) {
                  <tr class="hover:bg-slate-900/50 transition-colors">
                    <td class="py-3.5 px-4 font-mono font-bold text-indigo-400">
                      {{ c.code }}
                    </td>
                    <td class="py-3.5 px-4 font-semibold text-white">
                      {{ c.title }}
                      <span class="block text-[11px] text-cyan-400/80 font-normal">{{ c.department }}</span>
                    </td>
                    <td class="py-3.5 px-4 text-center font-semibold text-slate-200">
                      {{ c.credits }}
                    </td>
                    <td class="py-3.5 px-4 text-center font-mono">
                      <span [ngClass]="c.enrolledCount >= c.maxCapacity ? 'text-rose-400 font-bold' : 'text-slate-300'">
                        {{ c.enrolledCount }} / {{ c.maxCapacity }}
                      </span>
                    </td>
                    <td class="py-3.5 px-4 text-slate-400">
                      {{ c.instructorName || 'Unassigned' }}
                    </td>
                    <td class="py-3.5 px-4 text-right space-x-2">
                      <button (click)="openEditModal(c)" class="text-indigo-400 hover:text-indigo-300 font-medium">Edit</button>
                      <button (click)="deleteCourse(c.id)" class="text-rose-400 hover:text-rose-300 font-medium">Delete</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>

      <!-- Create / Edit Modal -->
      @if (showModal()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm">
          <div class="glass-panel w-full max-w-lg p-6 sm:p-8 border border-white/10 shadow-2xl relative">
            <button (click)="showModal.set(false)" class="absolute top-5 right-5 text-slate-400 hover:text-white">
              <span class="material-icons text-xl">close</span>
            </button>

            <h2 class="text-xl font-bold text-white mb-1">{{ editingCourseId() ? 'Edit Course' : 'Create New Course' }}</h2>
            <p class="text-xs text-slate-400 mb-6">Enter academic course specifications</p>

            <form (ngSubmit)="saveCourse()" class="space-y-4">
              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label class="block text-xs font-semibold text-slate-300 mb-1.5">Course Code</label>
                  <input type="text" [(ngModel)]="formData.code" name="code" required placeholder="e.g. CS101" class="form-input text-xs font-mono" />
                </div>
                <div>
                  <label class="block text-xs font-semibold text-slate-300 mb-1.5">Credits</label>
                  <input type="number" [(ngModel)]="formData.credits" name="credits" required min="1" max="10" class="form-input text-xs" />
                </div>
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-300 mb-1.5">Course Title</label>
                <input type="text" [(ngModel)]="formData.title" name="title" required placeholder="Introduction to Computer Science" class="form-input text-xs" />
              </div>

              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label class="block text-xs font-semibold text-slate-300 mb-1.5">Department</label>
                  <select [(ngModel)]="formData.department" name="department" class="form-select text-xs">
                    <option value="Computer Science">Computer Science</option>
                    <option value="Engineering">Engineering</option>
                    <option value="Business">Business</option>
                    <option value="Mathematics">Mathematics</option>
                  </select>
                </div>
                <div>
                  <label class="block text-xs font-semibold text-slate-300 mb-1.5">Max Capacity</label>
                  <input type="number" [(ngModel)]="formData.maxCapacity" name="maxCapacity" required min="1" class="form-input text-xs" />
                </div>
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-300 mb-1.5">Description</label>
                <textarea [(ngModel)]="formData.description" name="description" rows="3" class="form-textarea text-xs" placeholder="Course syllabus overview..."></textarea>
              </div>

              <div class="flex items-center justify-end gap-3 pt-4 border-t border-white/10">
                <button type="button" (click)="showModal.set(false)" class="btn-secondary text-xs py-2 px-4">Cancel</button>
                <button type="submit" [disabled]="isSaving()" class="btn-primary text-xs py-2 px-5">
                  {{ isSaving() ? 'Saving...' : 'Save Course' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `
})
export class AdminCourseListComponent implements OnInit {
  private courseService = inject(CourseService);

  courses = signal<Course[]>([]);
  isLoading = signal(false);
  isSaving = signal(false);
  alertMessage = signal<string | null>(null);
  isSuccess = signal(true);

  showModal = signal(false);
  editingCourseId = signal<number | null>(null);

  formData = {
    code: '',
    title: '',
    department: 'Computer Science',
    credits: 3,
    maxCapacity: 30,
    description: ''
  };

  ngOnInit() {
    this.loadCourses();
  }

  loadCourses() {
    this.isLoading.set(true);
    this.courseService.getCourses({ pageSize: 100 }).subscribe({
      next: (res) => {
        this.courses.set(res.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  openCreateModal() {
    this.editingCourseId.set(null);
    this.formData = {
      code: '',
      title: '',
      department: 'Computer Science',
      credits: 3,
      maxCapacity: 30,
      description: ''
    };
    this.showModal.set(true);
  }

  openEditModal(course: Course) {
    this.editingCourseId.set(course.id);
    this.formData = {
      code: course.code,
      title: course.title,
      department: course.department,
      credits: course.credits,
      maxCapacity: course.maxCapacity,
      description: course.description
    };
    this.showModal.set(true);
  }

  saveCourse() {
    this.isSaving.set(true);
    const id = this.editingCourseId();

    if (id) {
      const updateDto: UpdateCourseDto = {
        title: this.formData.title,
        description: this.formData.description,
        credits: this.formData.credits,
        maxCapacity: this.formData.maxCapacity,
        department: this.formData.department
      };
      this.courseService.updateCourse(id, updateDto).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.showModal.set(false);
          this.isSuccess.set(true);
          this.alertMessage.set('Course updated successfully!');
          this.loadCourses();
        },
        error: (err) => {
          this.isSaving.set(false);
          this.isSuccess.set(false);
          this.alertMessage.set(err.message || 'Failed to update course.');
        }
      });
    } else {
      const createDto: CreateCourseDto = {
        code: this.formData.code,
        title: this.formData.title,
        description: this.formData.description,
        credits: this.formData.credits,
        maxCapacity: this.formData.maxCapacity,
        department: this.formData.department
      };
      this.courseService.createCourse(createDto).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.showModal.set(false);
          this.isSuccess.set(true);
          this.alertMessage.set('Course created successfully!');
          this.loadCourses();
        },
        error: (err) => {
          this.isSaving.set(false);
          this.isSuccess.set(false);
          this.alertMessage.set(err.message || 'Failed to create course.');
        }
      });
    }
  }

  deleteCourse(id: number) {
    if (confirm('Are you sure you want to delete this course?')) {
      this.courseService.deleteCourse(id).subscribe({
        next: () => {
          this.isSuccess.set(true);
          this.alertMessage.set('Course deleted.');
          this.loadCourses();
        },
        error: (err) => {
          this.isSuccess.set(false);
          this.alertMessage.set(err.message || 'Failed to delete course.');
        }
      });
    }
  }
}
