import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Course } from '../../models/course.model';

@Component({
  selector: 'app-course-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="glass-card p-6 flex flex-col justify-between h-full relative overflow-hidden group">
      <!-- Top Decorative Accent Gradient Border -->
      <div class="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-indigo-500 via-cyan-400 to-emerald-400 opacity-60 group-hover:opacity-100 transition-opacity"></div>

      <div>
        <!-- Card Header: Code Badge + Credits -->
        <div class="flex items-center justify-between mb-3">
          <span class="badge badge-primary font-mono text-xs">{{ course.code }}</span>
          <span class="text-xs font-semibold text-slate-400 bg-slate-800/80 px-2.5 py-1 rounded-md border border-white/5">
            {{ course.credits }} Credits
          </span>
        </div>

        <!-- Title & Department -->
        <h3 class="text-lg font-bold text-slate-100 group-hover:text-indigo-300 transition-colors line-clamp-1 mb-1">
          {{ course.title }}
        </h3>
        <div class="text-xs font-medium text-cyan-400/90 mb-3 flex items-center gap-1.5">
          <span class="material-icons text-[14px]">account_balance</span>
          <span>{{ course.department }}</span>
        </div>

        <!-- Description -->
        <p class="text-xs text-slate-400 line-clamp-2 leading-relaxed mb-4">
          {{ course.description }}
        </p>
      </div>

      <div>
        <!-- Capacity Progress Bar -->
        <div class="mb-4">
          <div class="flex items-center justify-between text-[11px] mb-1.5">
            <span class="text-slate-400">Enrollment Capacity</span>
            <span class="font-bold" [ngClass]="isFull ? 'text-rose-400' : 'text-slate-300'">
              {{ course.enrolledCount }} / {{ course.maxCapacity }}
            </span>
          </div>
          <div class="w-full bg-slate-800 rounded-full h-2 overflow-hidden border border-white/5">
            <div
              class="h-full rounded-full transition-all duration-500"
              [style.width.%]="capacityPercentage"
              [ngClass]="{
                'bg-gradient-to-r from-emerald-500 to-teal-400': capacityPercentage < 75,
                'bg-gradient-to-r from-amber-500 to-orange-400': capacityPercentage >= 75 && capacityPercentage < 100,
                'bg-gradient-to-r from-rose-500 to-red-600': capacityPercentage >= 100
              }"
            ></div>
          </div>
        </div>

        <!-- Card Footer: Instructor & Action Button -->
        <div class="flex items-center justify-between pt-3 border-t border-white/5">
          <div class="flex items-center gap-2">
            <div class="w-7 h-7 rounded-full bg-indigo-600/30 border border-indigo-400/20 flex items-center justify-center text-[10px] font-bold text-indigo-300">
              {{ (course.instructorName || 'Prof')[0] }}
            </div>
            <span class="text-xs text-slate-300 font-medium truncate max-w-[110px]">
              {{ course.instructorName || 'Unassigned' }}
            </span>
          </div>

          <button
            (click)="actionClicked.emit(course)"
            [disabled]="isFull && actionLabel === 'Enroll'"
            [class]="buttonClass"
          >
            {{ isFull && actionLabel === 'Enroll' ? 'Full' : actionLabel }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      height: 100%;
    }
  `]
})
export class CourseCardComponent {
  @Input({ required: true }) course!: Course;
  @Input() actionLabel: string = 'View Details';
  @Input() buttonClass: string = 'btn-secondary text-xs py-1.5 px-3';
  @Output() actionClicked = new EventEmitter<Course>();

  get capacityPercentage(): number {
    if (!this.course.maxCapacity || this.course.maxCapacity === 0) return 0;
    return Math.min(100, Math.round((this.course.enrolledCount / this.course.maxCapacity) * 100));
  }

  get isFull(): boolean {
    return this.course.enrolledCount >= this.course.maxCapacity;
  }
}
