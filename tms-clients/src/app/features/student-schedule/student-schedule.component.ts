import { Component, inject, OnInit, signal, computed, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { StudentScheduleService } from '../../services/student-schedule.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { Schedule, StudentScheduleSummary, CourseLookup } from '../../models/schedule.model';
import { Subscription } from 'rxjs';

export interface CalendarCell {
  day: number;
  date: Date;
  dateStr: string; // YYYY-MM-DD
  isCurrentMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
  classes: Schedule[];
}

@Component({
  selector: 'app-student-schedule',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    LayoutComponent
  ],
  templateUrl: './student-schedule.component.html',
  styleUrls: ['./student-schedule.component.scss']
})
export class StudentScheduleComponent implements OnInit, OnDestroy {
  private studentScheduleService = inject(StudentScheduleService);
  private liveSync = inject(LiveSyncService);
  private snackBar = inject(MatSnackBar);

  readonly summary = signal<StudentScheduleSummary | null>(null);
  readonly schedules = signal<Schedule[]>([]);
  readonly enrolledCourses = signal<CourseLookup[]>([]);
  readonly selectedCourseId = signal<number>(0);
  readonly selectedView = signal<'Month' | 'Week' | 'Day'>('Month');
  readonly isLoading = signal<boolean>(false);
  readonly hasError = signal<boolean>(false);

  // Calendar Engine Date State
  readonly currentYear = signal<number>(new Date().getFullYear());
  readonly currentMonth = signal<number>(new Date().getMonth() + 1); // 1-indexed (1..12)
  readonly selectedDate = signal<Date>(new Date());

  // Weekdays header
  readonly weekDays = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'];

  private liveSub?: Subscription;

  // Formatted date string for selected date: YYYY-MM-DD
  readonly selectedDateKey = computed(() => {
    return this.formatDateKey(this.selectedDate());
  });

  // Selected Day's Classes sorted chronologically
  readonly selectedDayClasses = computed(() => {
    const key = this.selectedDateKey();
    return this.schedules()
      .filter(s => s.date && s.date.split('T')[0] === key)
      .sort((a, b) => (a.startTime || '').localeCompare(b.startTime || ''));
  });

  // Dynamic Calendar Matrix for currentYear & currentMonth
  readonly calendarGrid = computed<CalendarCell[]>(() => {
    const year = this.currentYear();
    const month = this.currentMonth() - 1; // 0-indexed for JS Date
    const scheduleList = this.schedules();
    const selKey = this.selectedDateKey();
    const today = new Date();
    const todayKey = this.formatDateKey(today);

    const firstDayIndex = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const daysInPrevMonth = new Date(year, month, 0).getDate();

    const cells: CalendarCell[] = [];

    // Leading days from previous month
    for (let x = firstDayIndex; x > 0; x--) {
      const d = daysInPrevMonth - x + 1;
      const cellDate = new Date(year, month - 1, d);
      const key = this.formatDateKey(cellDate);
      const classes = scheduleList.filter(s => s.date && s.date.split('T')[0] === key);
      cells.push({
        day: d,
        date: cellDate,
        dateStr: key,
        isCurrentMonth: false,
        isToday: key === todayKey,
        isSelected: key === selKey,
        classes
      });
    }

    // Days in current month
    for (let d = 1; d <= daysInMonth; d++) {
      const cellDate = new Date(year, month, d);
      const key = this.formatDateKey(cellDate);
      const classes = scheduleList.filter(s => s.date && s.date.split('T')[0] === key);
      cells.push({
        day: d,
        date: cellDate,
        dateStr: key,
        isCurrentMonth: true,
        isToday: key === todayKey,
        isSelected: key === selKey,
        classes
      });
    }

    // Trailing days to fill 35 or 42 grid slots
    const totalSlots = cells.length <= 35 ? 35 : 42;
    const remaining = totalSlots - cells.length;
    for (let j = 1; j <= remaining; j++) {
      const cellDate = new Date(year, month + 1, j);
      const key = this.formatDateKey(cellDate);
      const classes = scheduleList.filter(s => s.date && s.date.split('T')[0] === key);
      cells.push({
        day: j,
        date: cellDate,
        dateStr: key,
        isCurrentMonth: false,
        isToday: key === todayKey,
        isSelected: key === selKey,
        classes
      });
    }

    return cells;
  });

  // Dynamic Week Row containing selected date
  readonly currentWeekCells = computed<CalendarCell[]>(() => {
    const grid = this.calendarGrid();
    const selKey = this.selectedDateKey();
    for (let i = 0; i < grid.length; i += 7) {
      const week = grid.slice(i, i + 7);
      if (week.some(c => c.dateStr === selKey)) {
        return week;
      }
    }
    return grid.slice(0, 7);
  });

  // Month Display Name: e.g. "September 2026"
  readonly monthYearDisplay = computed(() => {
    const d = new Date(this.currentYear(), this.currentMonth() - 1, 1);
    return d.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  });

  ngOnInit(): void {
    this.loadScheduleData();

    // Subscribe to SignalR live updates for schedules
    this.liveSub = this.liveSync.scheduleUpdated$.subscribe(() => {
      this.loadScheduleData();
      this.snackBar.open('Schedule updated live from academic server!', 'Close', { duration: 3000 });
    });

    // Subscribe to SignalR live updates for enrollments
    this.liveSub.add(
      this.liveSync.enrollmentUpdated$.subscribe(() => {
        this.loadScheduleData();
      })
    );
  }

  ngOnDestroy(): void {
    this.liveSub?.unsubscribe();
  }

  loadScheduleData(): void {
    this.isLoading.set(true);
    this.hasError.set(false);

    const year = this.currentYear();
    const month = this.currentMonth();
    const courseId = this.selectedCourseId();

    this.studentScheduleService.getMyScheduleOverview(year, month, courseId).subscribe({
      next: (data) => {
        this.summary.set(data);
        this.schedules.set(data.schedules || []);
        this.enrolledCourses.set(data.enrolledCourses || []);

        // Initial default date selection:
        const today = new Date();
        if (today.getFullYear() === year && (today.getMonth() + 1) === month) {
          this.selectedDate.set(today);
        } else {
          // If viewing another month, select 1st day of that month
          this.selectedDate.set(new Date(year, month - 1, 1));
        }

        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.hasError.set(true);
      }
    });
  }

  selectDay(cell: CalendarCell): void {
    this.selectedDate.set(cell.date);
  }

  prevMonth(): void {
    let m = this.currentMonth() - 1;
    let y = this.currentYear();
    if (m < 1) {
      m = 12;
      y -= 1;
    }
    this.currentMonth.set(m);
    this.currentYear.set(y);
    this.loadScheduleData();
  }

  nextMonth(): void {
    let m = this.currentMonth() + 1;
    let y = this.currentYear();
    if (m > 12) {
      m = 1;
      y += 1;
    }
    this.currentMonth.set(m);
    this.currentYear.set(y);
    this.loadScheduleData();
  }

  goToToday(): void {
    const today = new Date();
    this.currentYear.set(today.getFullYear());
    this.currentMonth.set(today.getMonth() + 1);
    this.selectedDate.set(today);
    this.loadScheduleData();
  }

  filterCourse(courseId: number): void {
    this.selectedCourseId.set(courseId);
    this.loadScheduleData();
  }

  setView(view: 'Month' | 'Week' | 'Day'): void {
    this.selectedView.set(view);
  }

  getSelectedCourseName(): string {
    const cid = this.selectedCourseId();
    if (!cid || cid === 0) return 'All Classes';
    const found = this.enrolledCourses().find(c => c.id === cid);
    return found ? found.title : 'All Classes';
  }

  getTypeClass(type?: string): string {
    const t = (type || 'lecture').toLowerCase();
    if (t.includes('lab')) return 'type-lab';
    if (t.includes('workshop')) return 'type-workshop';
    if (t.includes('exam')) return 'type-exam';
    return 'type-lecture';
  }

  private formatDateKey(d: Date): string {
    const year = d.getFullYear();
    const month = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
