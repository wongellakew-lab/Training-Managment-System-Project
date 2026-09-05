import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../../ui/layout/layout.component';
import { InstructorService, InstructorDetailItem } from '../../../services/instructor.service';
import { Schedule } from '../../../models/schedule.model';

export interface MiniCalendarDay {
  date: Date;
  dateStr: string;
  day: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
  schedules: Schedule[];
}

@Component({
  selector: 'app-instructor-detail',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    LayoutComponent
  ],
  templateUrl: './instructor-detail.component.html',
  styleUrls: ['./instructor-detail.component.scss']
})
export class InstructorDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private instructorService = inject(InstructorService);

  readonly instructor = signal<InstructorDetailItem | null>(null);
  readonly schedules = signal<Schedule[]>([]);
  readonly activeInstructors = signal<InstructorDetailItem[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly error = signal<string>('');

  // Suspension & Reassignment Modal state
  readonly showSuspendModal = signal<boolean>(false);
  readonly isProcessing = signal<boolean>(false);
  readonly courseAssignments = signal<{ [courseId: number]: number }>({});
  readonly suspendReason = signal<string>('');

  readonly currentDate = signal<Date>(new Date());
  readonly selectedDate = signal<Date>(new Date());

  readonly weekDays = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];

  readonly monthNames = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'
  ];

  readonly currentMonthLabel = computed(() => {
    const d = this.currentDate();
    return `${this.monthNames[d.getMonth()]} ${d.getFullYear()}`;
  });

  readonly selectedDaySchedules = computed<Schedule[]>(() => {
    const sel = this.selectedDate();
    const y = sel.getFullYear();
    const m = sel.getMonth();
    const d = sel.getDate();

    return this.schedules().filter((s) => {
      if (!s.date) return false;
      const sDate = new Date(s.date);
      return (
        sDate.getFullYear() === y &&
        sDate.getMonth() === m &&
        sDate.getDate() === d
      );
    });
  });

  readonly calendarDays = computed<MiniCalendarDay[]>(() => {
    const current = this.currentDate();
    const sel = this.selectedDate();
    const year = current.getFullYear();
    const month = current.getMonth();
    const today = new Date();

    const firstDayOfMonth = new Date(year, month, 1);
    const lastDayOfMonth = new Date(year, month + 1, 0);

    const startingDayOfWeek = firstDayOfMonth.getDay();
    const totalDays = lastDayOfMonth.getDate();

    const days: MiniCalendarDay[] = [];

    // Leading padding days from previous month
    const prevMonthLastDay = new Date(year, month, 0).getDate();
    for (let i = startingDayOfWeek - 1; i >= 0; i--) {
      const d = new Date(year, month - 1, prevMonthLastDay - i);
      days.push({
        date: d,
        dateStr: this.formatDateKey(d),
        day: d.getDate(),
        isCurrentMonth: false,
        isToday: this.isSameDay(d, today),
        isSelected: this.isSameDay(d, sel),
        schedules: this.getSchedulesForDate(d)
      });
    }

    // Days in current month
    for (let day = 1; day <= totalDays; day++) {
      const d = new Date(year, month, day);
      days.push({
        date: d,
        dateStr: this.formatDateKey(d),
        day: d.getDate(),
        isCurrentMonth: true,
        isToday: this.isSameDay(d, today),
        isSelected: this.isSameDay(d, sel),
        schedules: this.getSchedulesForDate(d)
      });
    }

    // Trailing padding days to fill 42 cells (6 rows)
    const remaining = 42 - days.length;
    for (let day = 1; day <= remaining; day++) {
      const d = new Date(year, month + 1, day);
      days.push({
        date: d,
        dateStr: this.formatDateKey(d),
        day: d.getDate(),
        isCurrentMonth: false,
        isToday: this.isSameDay(d, today),
        isSelected: this.isSameDay(d, sel),
        schedules: this.getSchedulesForDate(d)
      });
    }

    return days;
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = Number(idParam);
    if (!id || isNaN(id)) {
      this.error.set('Invalid instructor identifier specified.');
      this.isLoading.set(false);
      return;
    }

    this.loadInstructor(id);
  }

  loadInstructor(id: number): void {
    this.isLoading.set(true);
    this.error.set('');

    this.instructorService.getInstructorById(id).subscribe({
      next: (data) => {
        this.instructor.set(data);
        this.isLoading.set(false);
        this.loadSchedules(id);
      },
      error: (err) => {
        console.error('Failed to load instructor:', err);
        this.error.set(err.error?.message || 'Could not find the requested instructor.');
        this.isLoading.set(false);
      }
    });
  }

  loadSchedules(id: number): void {
    const cur = this.currentDate();
    this.instructorService.getInstructorSchedules(id, cur.getFullYear(), cur.getMonth() + 1).subscribe({
      next: (list) => {
        this.schedules.set(list || []);
      },
      error: (err) => {
        console.error('Failed to load instructor schedules:', err);
        this.schedules.set([]);
      }
    });
  }

  selectDate(d: Date): void {
    this.selectedDate.set(new Date(d));
    if (d.getMonth() !== this.currentDate().getMonth() || d.getFullYear() !== this.currentDate().getFullYear()) {
      this.currentDate.set(new Date(d.getFullYear(), d.getMonth(), 1));
      const inst = this.instructor();
      if (inst) {
        this.loadSchedules(inst.id);
      }
    }
  }

  prevMonth(): void {
    const c = this.currentDate();
    this.currentDate.set(new Date(c.getFullYear(), c.getMonth() - 1, 1));
    const inst = this.instructor();
    if (inst) {
      this.loadSchedules(inst.id);
    }
  }

  nextMonth(): void {
    const c = this.currentDate();
    this.currentDate.set(new Date(c.getFullYear(), c.getMonth() + 1, 1));
    const inst = this.instructor();
    if (inst) {
      this.loadSchedules(inst.id);
    }
  }

  goToToday(): void {
    const today = new Date();
    this.currentDate.set(new Date(today));
    this.selectedDate.set(new Date(today));
    const inst = this.instructor();
    if (inst) {
      this.loadSchedules(inst.id);
    }
  }

  goBack(): void {
    this.router.navigate(['/admin/instructors']);
  }

  openSuspendModal(): void {
    const inst = this.instructor();
    if (!inst) return;
    this.suspendReason.set('');
    this.courseAssignments.set({});

    // Load available instructors for reassignment excluding current instructor
    this.instructorService.getInstructors({ pageSize: 100 }).subscribe({
      next: (res) => {
        const active = (res.items || []).filter(i => i.id !== inst.id && i.status !== 'Suspended');
        this.activeInstructors.set(active as any);
        this.showSuspendModal.set(true);
      }
    });
  }

  closeSuspendModal(): void {
    this.showSuspendModal.set(false);
  }

  onReplacementChange(courseId: number, event: Event): void {
    const val = parseInt((event.target as HTMLSelectElement).value, 10);
    const curr = { ...this.courseAssignments() };
    if (val) {
      curr[courseId] = val;
    } else {
      delete curr[courseId];
    }
    this.courseAssignments.set(curr);
  }

  canSubmitSuspension(): boolean {
    const inst = this.instructor();
    if (!inst) return false;
    const courses = inst.courses || [];
    if (courses.length === 0) return true;
    const assigned = this.courseAssignments();
    return courses.every(c => !!assigned[c.id]);
  }

  confirmSuspension(): void {
    const inst = this.instructor();
    if (!inst || !this.canSubmitSuspension()) return;

    this.isProcessing.set(true);
    const reassignments = Object.entries(this.courseAssignments()).map(([courseId, repId]) => ({
      courseId: parseInt(courseId, 10),
      replacementInstructorId: repId
    }));

    this.instructorService.suspendInstructor(inst.id, {
      courseReassignments: reassignments,
      reason: this.suspendReason()
    }).subscribe({
      next: () => {
        this.isProcessing.set(false);
        this.closeSuspendModal();
        this.loadInstructor(inst.id);
      },
      error: (err) => {
        this.isProcessing.set(false);
        alert(err.error?.message || 'Failed to suspend instructor.');
      }
    });
  }

  confirmActivation(): void {
    const inst = this.instructor();
    if (!inst) return;

    if (!confirm(`Are you sure you want to reactivate ${inst.firstName} ${inst.lastName}? Any courses reassigned during their suspension will be restored.`)) {
      return;
    }

    this.isProcessing.set(true);
    this.instructorService.activateInstructor(inst.id).subscribe({
      next: () => {
        this.isProcessing.set(false);
        this.loadInstructor(inst.id);
      },
      error: (err) => {
        this.isProcessing.set(false);
        alert(err.error?.message || 'Failed to reactivate instructor.');
      }
    });
  }

  navigateToCourse(courseId: number): void {
    if (courseId) {
      this.router.navigate(['/admin/courses', courseId]);
    }
  }

  private getSchedulesForDate(date: Date): Schedule[] {
    const y = date.getFullYear();
    const m = date.getMonth();
    const d = date.getDate();

    return this.schedules().filter((s) => {
      if (!s.date) return false;
      const sDate = new Date(s.date);
      return (
        sDate.getFullYear() === y &&
        sDate.getMonth() === m &&
        sDate.getDate() === d
      );
    });
  }

  private isSameDay(a: Date, b: Date): boolean {
    return (
      a.getFullYear() === b.getFullYear() &&
      a.getMonth() === b.getMonth() &&
      a.getDate() === b.getDate()
    );
  }

  private formatDateKey(d: Date): string {
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${d.getFullYear()}-${month}-${day}`;
  }

  onAvatarError(event: Event): void {
    const img = event.target as HTMLImageElement;
    if (img) {
      img.style.display = 'none';
    }
  }

  onThumbError(event: Event): void {
    const img = event.target as HTMLImageElement;
    if (img) {
      img.style.display = 'none';
    }
  }

  getCategoryClass(category?: string): string {
    if (!category) return 'cat-design';
    const lower = category.toLowerCase();
    if (lower.includes('market')) return 'cat-marketing';
    if (lower.includes('web') || lower.includes('dev')) return 'cat-web-dev';
    if (lower.includes('comm')) return 'cat-communication';
    if (lower.includes('bus')) return 'cat-business';
    return 'cat-design';
  }

  getCategoryIcon(category?: string): string {
    if (!category) return 'school';
    const lower = category.toLowerCase();
    if (lower.includes('market')) return 'trending_up';
    if (lower.includes('web') || lower.includes('dev')) return 'code';
    if (lower.includes('comm')) return 'record_voice_over';
    if (lower.includes('bus')) return 'analytics';
    if (lower.includes('design')) return 'palette';
    return 'school';
  }

  getScheduleTypeClass(type?: string): string {
    if (!type) return 'type-lecture';
    const lower = type.toLowerCase();
    if (lower.includes('workshop')) return 'type-workshop';
    if (lower.includes('lab')) return 'type-lab';
    if (lower.includes('exam')) return 'type-exam';
    return 'type-lecture';
  }
}
