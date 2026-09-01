import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Course } from '../../models/course.model';
import { EnrollmentService } from '../../services/enrollment.service';

@Component({
  selector: 'app-enrollment-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (isOpen) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm animate-fade-in">
        <div class="glass-panel w-full max-w-lg p-6 sm:p-8 border border-white/10 shadow-2xl relative">
          <!-- Close button -->
          <button (click)="onClose()" class="absolute top-5 right-5 text-slate-400 hover:text-white transition-colors">
            <span class="material-icons text-xl">close</span>
          </button>

          <!-- Header -->
          <div class="flex items-center gap-3 mb-6">
            <div class="w-10 h-10 rounded-xl bg-indigo-600/30 border border-indigo-500/30 flex items-center justify-center text-indigo-400">
              <span class="material-icons">how_to_reg</span>
            </div>
            <div>
              <h2 class="text-xl font-bold text-white">Course Registration</h2>
              <p class="text-xs text-slate-400">Confirm student enrollment details</p>
            </div>
          </div>

          @if (course) {
            <!-- Course Summary Box -->
            <div class="p-4 rounded-xl bg-slate-900/80 border border-white/5 mb-6 space-y-2">
              <div class="flex items-center justify-between">
                <span class="font-bold text-white text-sm">{{ course.title }}</span>
                <span class="badge badge-primary font-mono text-[10px]">{{ course.code }}</span>
              </div>
              <div class="text-xs text-slate-400 flex items-center gap-4">
                <span>Department: <strong class="text-slate-300">{{ course.department }}</strong></span>
                <span>Credits: <strong class="text-slate-300">{{ course.credits }}</strong></span>
                <span>Seats: <strong class="text-slate-300">{{ course.enrolledCount }}/{{ course.maxCapacity }}</strong></span>
              </div>
            </div>
          }

          <!-- Form Fields -->
          <form (ngSubmit)="onSubmit()" class="space-y-4">
            <div>
              <label class="block text-xs font-semibold text-slate-300 mb-1.5">Student ID</label>
              <input
                type="number"
                [(ngModel)]="studentId"
                name="studentId"
                required
                min="1"
                class="form-input text-xs"
                placeholder="e.g. 1"
              />
            </div>

            @if (errorMessage()) {
              <div class="p-3 rounded-lg bg-rose-500/15 border border-rose-500/30 text-xs text-rose-300 flex items-center gap-2">
                <span class="material-icons text-sm">error</span>
                <span>{{ errorMessage() }}</span>
              </div>
            }

            <div class="flex items-center justify-end gap-3 pt-4 border-t border-white/10">
              <button type="button" (click)="onClose()" class="btn-secondary text-xs py-2 px-4">
                Cancel
              </button>
              <button type="submit" [disabled]="isLoading()" class="btn-primary text-xs py-2 px-5">
                @if (isLoading()) {
                  <span class="inline-block animate-spin w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full mr-1.5"></span>
                  <span>Enrolling...</span>
                } @else {
                  <span>Confirm Enrollment</span>
                }
              </button>
            </div>
          </form>
        </div>
      </div>
    }
  `
})
export class EnrollmentModalComponent {
  private enrollmentService = inject(EnrollmentService);

  @Input() isOpen = false;
  @Input() course: Course | null = null;
  @Input() defaultStudentId: number = 1;
  @Output() close = new EventEmitter<void>();
  @Output() enrolled = new EventEmitter<Course>();

  studentId: number = 1;
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  ngOnInit() {
    this.studentId = this.defaultStudentId;
  }

  onClose() {
    this.errorMessage.set(null);
    this.close.emit();
  }

  onSubmit() {
    if (!this.course) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.enrollmentService.enrollStudent({
      studentId: this.studentId,
      courseId: this.course.id
    }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.enrolled.emit(this.course!);
        this.onClose();
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.message || 'Enrollment rejected by business rules.');
      }
    });
  }
}
