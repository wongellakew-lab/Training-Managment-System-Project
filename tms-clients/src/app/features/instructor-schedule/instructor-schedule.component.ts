import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { InstructorWorkspaceService } from '../../services/instructor-workspace.service';
import { Schedule } from '../../models/schedule.model';

interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
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
export class InstructorScheduleComponent implements OnInit {
  private instructorService = inject(InstructorWorkspaceService);
  private router = inject(Router);

  isLoading = signal<boolean>(true);
  currentDate = signal<Date>(new Date());
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

  calendarDays = computed<CalendarDay[]>(() => {
    const current = this.currentDate();
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
      days.push({
        date: d,
        isCurrentMonth: false,
        isToday: false,
        schedules: this.getSchedulesForDate(d)
      });
    }

    // Current month days
    for (let day = 1; day <= totalDays; day++) {
      const d = new Date(year, month, day);
      const isToday =
        d.getDate() === today.getDate() &&
        d.getMonth() === today.getMonth() &&
        d.getFullYear() === today.getFullYear();

      days.push({
        date: d,
        isCurrentMonth: true,
        isToday,
        schedules: this.getSchedulesForDate(d)
      });
    }

    // Next month padding days to complete 35 or 42 grid cells
    const remaining = 42 - days.length;
    for (let day = 1; day <= remaining; day++) {
      const d = new Date(year, month + 1, day);
      days.push({
        date: d,
        isCurrentMonth: false,
        isToday: false,
        schedules: this.getSchedulesForDate(d)
      });
    }

    return days;
  });

  ngOnInit(): void {
    this.loadSchedules();
  }

  loadSchedules(): void {
    this.isLoading.set(true);
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
    this.currentDate.set(new Date());
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
}
