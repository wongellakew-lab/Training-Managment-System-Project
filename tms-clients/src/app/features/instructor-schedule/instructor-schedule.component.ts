import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { InstructorWorkspaceService } from '../../services/instructor-workspace.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { Schedule } from '../../models/schedule.model';

export interface CalendarDay {
  date: Date;
  dateStr: string;
  day: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
  schedules: Schedule[];
}

@Component({
  selector: 'app-instructor-schedule',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LayoutComponent,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './instructor-schedule.component.html',
  styleUrls: ['./instructor-schedule.component.scss']
})
export class InstructorScheduleComponent implements OnInit, OnDestroy {
  private instructorService = inject(InstructorWorkspaceService);
  private liveSyncService = inject(LiveSyncService);
  private router = inject(Router);
  private syncSub?: Subscription;

  isLoading = signal<boolean>(true);
  currentDate = signal<Date>(new Date());
  selectedDate = signal<Date>(new Date());
  schedules = signal<Schedule[]>([]);
  selectedSchedule = signal<Schedule | null>(null);

  readonly monthNames = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'
  ];

  readonly weekDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  currentMonthLabel = computed(() => {
    const d = this.currentDate();
    return `${this.monthNames[d.getMonth()]} ${d.getFullYear()}`;
  });

  selectedDayClasses = computed<Schedule[]>(() => {
    const sel = this.selectedDate();
    return this.getSchedulesForDate(sel);
  });

  calendarDays = computed<CalendarDay[]>(() => {
    const current = this.currentDate();
    const sel = this.selectedDate();
    const year = current.getFullYear();
    const month = current.getMonth();
    const today = new Date();

    const firstDayOfMonth = new Date(year, month, 1);
    const lastDayOfMonth = new Date(year, month + 1, 0);

    const startingDayOfWeek = firstDayOfMonth.getDay();
    const totalDays = lastDayOfMonth.getDate();

    const days: CalendarDay[] = [];

    // Previous month padding days
    const prevMonthLastDay = new Date(year, month, 0).getDate();
    for (let i = startingDayOfWeek - 1; i >= 0; i--) {
      const d = new Date(year, month - 1, prevMonthLastDay - i);
      const isSelected = this.isSameDay(d, sel);
      const isToday = this.isSameDay(d, today);
      days.push({
        date: d,
        dateStr: this.formatDateKey(d),
        day: d.getDate(),
        isCurrentMonth: false,
        isToday,
        isSelected,
        schedules: this.getSchedulesForDate(d)
      });
    }

    // Current month days
    for (let day = 1; day <= totalDays; day++) {
      const d = new Date(year, month, day);
      const isSelected = this.isSameDay(d, sel);
      const isToday = this.isSameDay(d, today);

      days.push({
        date: d,
        dateStr: this.formatDateKey(d),
        day: d.getDate(),
        isCurrentMonth: true,
        isToday,
        isSelected,
        schedules: this.getSchedulesForDate(d)
      });
    }

    // Next month padding days to complete grid (42 cells: 6 rows)
    const remaining = 42 - days.length;
    for (let day = 1; day <= remaining; day++) {
      const d = new Date(year, month + 1, day);
      const isSelected = this.isSameDay(d, sel);
      const isToday = this.isSameDay(d, today);
      days.push({
        date: d,
        dateStr: this.formatDateKey(d),
        day: d.getDate(),
        isCurrentMonth: false,
        isToday,
        isSelected,
        schedules: this.getSchedulesForDate(d)
      });
    }

    return days;
  });

  ngOnInit(): void {
    this.loadSchedules();

    // Listen to real-time sync updates from SignalR
    this.syncSub = this.liveSyncService.scheduleUpdated$.subscribe(() => {
      this.loadSchedules(false);
    });
  }

  ngOnDestroy(): void {
    this.syncSub?.unsubscribe();
  }

  loadSchedules(showSpinner = true): void {
    if (showSpinner) {
      this.isLoading.set(true);
    }
    const d = this.currentDate();
    this.instructorService.getSchedule(d.getFullYear(), d.getMonth() + 1).subscribe({
      next: (list) => {
        this.schedules.set(list || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load instructor schedules:', err);
        this.isLoading.set(false);
      }
    });
  }

  selectDate(d: Date): void {
    this.selectedDate.set(new Date(d));
    // If user clicked a day in previous/next month, update currentDate to keep view aligned
    if (d.getMonth() !== this.currentDate().getMonth() || d.getFullYear() !== this.currentDate().getFullYear()) {
      this.currentDate.set(new Date(d.getFullYear(), d.getMonth(), 1));
      this.loadSchedules();
    }
  }

  getSchedulesForDate(date: Date): Schedule[] {
    const y = date.getFullYear();
    const m = date.getMonth();
    const d = date.getDate();

    return this.schedules().filter((s) => {
      const sDate = new Date(s.date);
      return (
        sDate.getFullYear() === y &&
        sDate.getMonth() === m &&
        sDate.getDate() === d
      );
    });
  }

  isSameDay(a: Date, b: Date): boolean {
    return (
      a.getFullYear() === b.getFullYear() &&
      a.getMonth() === b.getMonth() &&
      a.getDate() === b.getDate()
    );
  }

  formatDateKey(d: Date): string {
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${d.getFullYear()}-${month}-${day}`;
  }

  prevMonth(): void {
    const current = this.currentDate();
    this.currentDate.set(new Date(current.getFullYear(), current.getMonth() - 1, 1));
    this.loadSchedules();
  }

  nextMonth(): void {
    const current = this.currentDate();
    this.currentDate.set(new Date(current.getFullYear(), current.getMonth() + 1, 1));
    this.loadSchedules();
  }

  goToToday(): void {
    const today = new Date();
    this.currentDate.set(new Date(today));
    this.selectedDate.set(new Date(today));
    this.loadSchedules();
  }

  selectSchedule(s: Schedule, event: MouseEvent): void {
    event.stopPropagation();
    this.selectedSchedule.set(s);
  }

  closeDetails(): void {
    this.selectedSchedule.set(null);
  }

  goToAttendance(courseId?: number, date?: Date | string): void {
    if (!courseId) return;
    const dateStr = date ? new Date(date).toISOString().split('T')[0] : undefined;
    this.router.navigate(['/instructor/attendance'], { queryParams: { courseId, date: dateStr } });
  }

  getTypeClass(type?: string): string {
    if (!type) return 'type-lecture';
    const lower = type.toLowerCase();
    if (lower.includes('workshop')) return 'type-workshop';
    if (lower.includes('lab')) return 'type-lab';
    if (lower.includes('exam')) return 'type-exam';
    return 'type-lecture';
  }
}
