import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
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
  readonly currentMonth = signal<number>(5); // May
  readonly currentYear = signal<number>(2030);
  readonly selectedSchedule = signal<Schedule | null>(null);
  readonly showNewAgendaModal = signal(false);

  // New agenda form model
  newAgenda: CreateScheduleDto = {
    title: '',
    category: 'Meetings',
    date: '2030-05-15',
    startTime: '09:00 AM',
    endTime: '10:30 AM',
    picName: 'James Ocampo',
    location: 'Eduva Creative Studio Room',
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
          this.selectedSchedule.set(data.find(s => s.category === 'Workshops') || data[0]);
        }
      }
    });
  }

  filterCategory(cat: string): void {
    this.selectedCategory.set(cat);
    this.loadSchedules();
  }

  buildCalendarGrid(): void {
    const list = this.schedules();
    const cells: CalendarCell[] = [];

    // April leading days (28, 29, 30)
    for (let d = 28; d <= 30; d++) {
      cells.push({ day: d, dateStr: `2030-04-${d}`, isCurrentMonth: false, schedules: [] });
    }

    // May days (1 to 31)
    for (let d = 1; d <= 31; d++) {
      const dateKey = `2030-05-${d.toString().padStart(2, '0')}`;
      const daySchedules = list.filter(s => {
        if (!s.date) return false;
        const sDate = s.date.split('T')[0];
        return sDate === dateKey || new Date(s.date).getDate() === d;
      });
      cells.push({ day: d, dateStr: dateKey, isCurrentMonth: true, schedules: daySchedules });
    }

    // June trailing days (1)
    cells.push({ day: 1, dateStr: '2030-06-01', isCurrentMonth: false, schedules: [] });

    this.calendarDays = cells;
  }

  selectSchedule(schedule: Schedule): void {
    this.selectedSchedule.set(schedule);
  }

  openNewAgendaModal(): void {
    this.newAgenda = {
      title: '',
      category: 'Meetings',
      date: '2030-05-15',
      startTime: '09:00 AM',
      endTime: '10:30 AM',
      picName: 'James Ocampo',
      location: 'Eduva Creative Studio Room',
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
    return this.schedules().length || 12;
  }
  get totalMeetings(): number {
    return this.schedules().filter(s => s.category.toLowerCase() === 'meetings').length || 4;
  }
  get totalWorkshops(): number {
    return this.schedules().filter(s => s.category.toLowerCase() === 'workshops').length || 4;
  }
  get totalEvents(): number {
    return this.schedules().filter(s => s.category.toLowerCase() === 'events').length || 4;
  }
}
