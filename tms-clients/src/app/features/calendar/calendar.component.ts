import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { ScheduleService } from '../../services/schedule.service';
import { CourseService } from '../../services/course.service';
import { Schedule, CreateScheduleDto } from '../../models/schedule.model';
import { Course } from '../../models/course.model';

export interface CalendarCell {
  day: number;
  dateStr: string;
  isCurrentMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
  schedules: Schedule[];
}

@Component({
  selector: 'app-calendar-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    LayoutComponent
  ],
  templateUrl: './calendar.component.html',
  styleUrls: ['./calendar.component.scss']
})
export class CalendarComponent implements OnInit {
  private scheduleService = inject(ScheduleService);
  private courseService = inject(CourseService);

  readonly schedules = signal<Schedule[]>([]);
  readonly courses = signal<Course[]>([]);
  readonly departments = signal<string[]>([
    'Computer Science',
    'Web Development',
    'Design',
    'Marketing',
    'Business',
    'Communication',
    'Information Technology',
    'Data Science'
  ]);
  readonly selectedCategory = signal<string>('All');
  readonly selectedView = signal<'Month' | 'Week' | 'Day'>('Month');
  readonly currentMonth = signal<number>(new Date().getMonth() + 1);
  readonly currentYear = signal<number>(new Date().getFullYear());
  readonly selectedDateStr = signal<string>(new Date().toISOString().slice(0, 10));
  readonly selectedSchedule = signal<Schedule | null>(null);
  readonly showNewAgendaModal = signal(false);
  readonly isLoading = signal(false);

  private readonly monthNames = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'
  ];

  readonly monthTitle = computed(() => {
    const m = this.currentMonth();
    const y = this.currentYear();
    const name = this.monthNames[m - 1] || '';
    return `${name} ${y}`;
  });

  readonly selectedDateSchedules = computed(() => {
    const dateKey = this.selectedDateStr();
    return this.schedules().filter(s => {
      if (!s.date) return false;
      return s.date.startsWith(dateKey);
    });
  });

  readonly selectedDateFormatted = computed(() => {
    const dateKey = this.selectedDateStr();
    if (!dateKey) return '';
    const parts = dateKey.split('-');
    if (parts.length < 3) return dateKey;
    const y = parseInt(parts[0], 10);
    const m = parseInt(parts[1], 10) - 1;
    const d = parseInt(parts[2], 10);
    const dateObj = new Date(y, m, d);
    return dateObj.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  });

  // New agenda form model
  newAgenda: CreateScheduleDto = {
    title: '',
    category: 'Meetings',
    date: new Date().toISOString().slice(0, 10),
    startTime: '09:00 AM',
    endTime: '10:30 AM',
    picName: '',
    location: '',
    notes: '',
    targetAudience: 'All',
    targetCourseId: null,
    targetDepartment: null
  };

  calendarDays: CalendarCell[] = [];

  ngOnInit(): void {
    this.loadSchedules();
    this.loadCourses();
  }

  loadCourses(): void {
    this.courseService.getCourses({ pageSize: 100 }).subscribe({
      next: (res) => {
        this.courses.set(res.items || []);
      },
      error: (err) => console.error('Error loading courses for targeting:', err)
    });
  }

  loadSchedules(): void {
    this.isLoading.set(true);
    this.scheduleService.getSchedules(this.selectedCategory(), this.currentMonth(), this.currentYear()).subscribe({
      next: (data) => {
        this.schedules.set(data);
        this.buildCalendarGrid();
        this.isLoading.set(false);

        // Update selected schedule
        const currentSelDateSchedules = this.selectedDateSchedules();
        if (currentSelDateSchedules.length > 0) {
          if (!this.selectedSchedule() || !currentSelDateSchedules.some(s => s.id === this.selectedSchedule()?.id)) {
            this.selectedSchedule.set(currentSelDateSchedules[0]);
          }
        } else if (data.length > 0 && !this.selectedSchedule()) {
          this.selectedSchedule.set(data[0]);
          if (data[0].date) {
            this.selectedDateStr.set(data[0].date.split('T')[0]);
            this.buildCalendarGrid();
          }
        }
      },
      error: () => this.isLoading.set(false)
    });
  }

  filterCategory(cat: string): void {
    this.selectedCategory.set(cat);
    this.loadSchedules();
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
    this.loadSchedules();
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
    this.loadSchedules();
  }

  goToToday(): void {
    const today = new Date();
    this.currentMonth.set(today.getMonth() + 1);
    this.currentYear.set(today.getFullYear());
    this.selectedDateStr.set(today.toISOString().slice(0, 10));
    this.loadSchedules();
  }

  buildCalendarGrid(): void {
    const list = this.schedules();
    const cells: CalendarCell[] = [];
    const year = this.currentYear();
    const month = this.currentMonth() - 1;

    const todayStr = new Date().toISOString().slice(0, 10);
    const selectedDateKey = this.selectedDateStr();

    const firstDayIndex = new Date(year, month, 1).getDay(); // 0 = Sunday
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const daysInPrevMonth = new Date(year, month, 0).getDate();

    // Leading days from previous month
    const prevYear = month === 0 ? year - 1 : year;
    const prevMonthNum = month === 0 ? 12 : month;
    for (let x = firstDayIndex; x > 0; x--) {
      const d = daysInPrevMonth - x + 1;
      const prevDateKey = `${prevYear}-${prevMonthNum.toString().padStart(2, '0')}-${d.toString().padStart(2, '0')}`;
      cells.push({
        day: d,
        dateStr: prevDateKey,
        isCurrentMonth: false,
        isToday: prevDateKey === todayStr,
        isSelected: prevDateKey === selectedDateKey,
        schedules: []
      });
    }

    // Days in current month
    for (let d = 1; d <= daysInMonth; d++) {
      const dateKey = `${year}-${(month + 1).toString().padStart(2, '0')}-${d.toString().padStart(2, '0')}`;
      const daySchedules = list.filter(s => {
        if (!s.date) return false;
        return s.date.startsWith(dateKey);
      });

      cells.push({
        day: d,
        dateStr: dateKey,
        isCurrentMonth: true,
        isToday: dateKey === todayStr,
        isSelected: dateKey === selectedDateKey,
        schedules: daySchedules
      });
    }

    // Trailing days to fill 35 or 42 grid cells
    const totalSlots = cells.length <= 35 ? 35 : 42;
    const remaining = totalSlots - cells.length;
    const nextYear = month === 11 ? year + 1 : year;
    const nextMonthNum = month === 11 ? 1 : month + 2;
    for (let j = 1; j <= remaining; j++) {
      const nextDateKey = `${nextYear}-${nextMonthNum.toString().padStart(2, '0')}-${j.toString().padStart(2, '0')}`;
      cells.push({
        day: j,
        dateStr: nextDateKey,
        isCurrentMonth: false,
        isToday: nextDateKey === todayStr,
        isSelected: nextDateKey === selectedDateKey,
        schedules: []
      });
    }

    this.calendarDays = cells;
  }

  selectDate(cell: CalendarCell): void {
    if (!cell.dateStr) return;
    this.selectedDateStr.set(cell.dateStr);
    this.buildCalendarGrid();

    if (cell.schedules.length > 0) {
      this.selectedSchedule.set(cell.schedules[0]);
    } else {
      this.selectedSchedule.set(null);
    }
  }

  selectSchedule(schedule: Schedule, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.selectedSchedule.set(schedule);
    if (schedule.date) {
      this.selectedDateStr.set(schedule.date.split('T')[0]);
      this.buildCalendarGrid();
    }
  }

  openNewAgendaModal(): void {
    this.newAgenda = {
      title: '',
      category: 'Meetings',
      date: this.selectedDateStr() || new Date().toISOString().slice(0, 10),
      startTime: '09:00 AM',
      endTime: '10:30 AM',
      picName: '',
      location: '',
      notes: '',
      targetAudience: 'All',
      targetCourseId: null,
      targetDepartment: null
    };
    this.showNewAgendaModal.set(true);
  }

  closeNewAgendaModal(): void {
    this.showNewAgendaModal.set(false);
  }

  submitNewAgenda(): void {
    if (!this.newAgenda.title) return;
    this.scheduleService.createSchedule(this.newAgenda).subscribe({
      next: (created) => {
        this.closeNewAgendaModal();
        this.loadSchedules();
        this.selectedSchedule.set(created);
      }
    });
  }

  get totalSchedules(): number {
    return this.schedules().length;
  }
  get totalMeetings(): number {
    return this.schedules().filter(s => s.category?.toLowerCase() === 'meetings').length;
  }
  get totalWorkshops(): number {
    return this.schedules().filter(s => s.category?.toLowerCase() === 'workshops').length;
  }
  get totalEvents(): number {
    return this.schedules().filter(s => s.category?.toLowerCase() === 'events').length;
  }
}
