import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialogModule } from '@angular/material/dialog';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { ScheduleService } from '../../services/schedule.service';
import { Schedule, CreateScheduleDto } from '../../models/schedule.model';

interface CalendarCell {
  day: number;
  dateStr: string;
  isCurrentMonth: boolean;
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
    LayoutComponent
  ],
  templateUrl: './calendar.component.html',
  styleUrls: ['./calendar.component.scss']
})
export class CalendarComponent implements OnInit {
  private scheduleService = inject(ScheduleService);

  readonly schedules = signal<Schedule[]>([]);
  readonly selectedCategory = signal<string>('All');
  readonly selectedView = signal<'Month' | 'Week' | 'Day'>('Month');
  readonly currentMonth = signal<number>(new Date().getMonth() + 1);
  readonly currentYear = signal<number>(new Date().getFullYear());
  readonly selectedSchedule = signal<Schedule | null>(null);
  readonly showNewAgendaModal = signal(false);

  // New agenda form model
  newAgenda: CreateScheduleDto = {
    title: '',
    category: 'Meetings',
    date: new Date().toISOString().slice(0, 10),
    startTime: '09:00 AM',
    endTime: '10:30 AM',
    picName: '',
    location: '',
    notes: ''
  };

  // Calendar matrix calculation
  calendarDays: CalendarCell[] = [];

  ngOnInit(): void {
    this.loadSchedules();
  }

  loadSchedules(): void {
    this.scheduleService.getSchedules(this.selectedCategory(), this.currentMonth(), this.currentYear()).subscribe({
      next: (data) => {
        this.schedules.set(data);
        this.buildCalendarGrid();
        if (data.length > 0 && !this.selectedSchedule()) {
          this.selectedSchedule.set(data[0]);
        } else if (data.length === 0) {
          this.selectedSchedule.set(null);
        }
      }
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

  buildCalendarGrid(): void {
    const list = this.schedules();
    const cells: CalendarCell[] = [];
    const year = this.currentYear();
    const month = this.currentMonth() - 1; // 0-indexed for JS Date

    const firstDayIndex = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const daysInPrevMonth = new Date(year, month, 0).getDate();

    // Leading days from previous month
    for (let x = firstDayIndex; x > 0; x--) {
      const d = daysInPrevMonth - x + 1;
      cells.push({ day: d, dateStr: '', isCurrentMonth: false, schedules: [] });
    }

    // Days in current month
    for (let d = 1; d <= daysInMonth; d++) {
      const dateKey = `${year}-${(month + 1).toString().padStart(2, '0')}-${d.toString().padStart(2, '0')}`;
      const daySchedules = list.filter(s => {
        if (!s.date) return false;
        const sDate = s.date.split('T')[0];
        return sDate === dateKey || new Date(s.date).getDate() === d;
      });
      cells.push({ day: d, dateStr: dateKey, isCurrentMonth: true, schedules: daySchedules });
    }

    // Trailing days to fill 35 or 42 grid cells
    const totalSlots = cells.length <= 35 ? 35 : 42;
    const remaining = totalSlots - cells.length;
    for (let j = 1; j <= remaining; j++) {
      cells.push({ day: j, dateStr: '', isCurrentMonth: false, schedules: [] });
    }

    this.calendarDays = cells;
  }

  selectSchedule(schedule: Schedule): void {
    this.selectedSchedule.set(schedule);
  }

  openNewAgendaModal(): void {
    this.newAgenda = {
      title: '',
      category: 'Meetings',
      date: new Date().toISOString().slice(0, 10),
      startTime: '09:00 AM',
      endTime: '10:30 AM',
      picName: '',
      location: '',
      notes: ''
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
