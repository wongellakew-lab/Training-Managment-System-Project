import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { DashboardService } from '../../services/dashboard.service';
import { ScheduleService } from '../../services/schedule.service';
import { AdminDashboardMetrics } from '../../models/dashboard.model';
import { Schedule } from '../../models/schedule.model';
import { LiveSyncService } from '../../services/live-sync.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatTooltipModule,
    LayoutComponent
  ],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private scheduleService = inject(ScheduleService);
  private liveSync = inject(LiveSyncService);

  readonly dashboard = signal<AdminDashboardMetrics | null>(null);
  readonly monthlySchedules = signal<Schedule[]>([]);
  readonly isLoading = signal(false);
  readonly selectedMonth = signal('February');
  readonly currentDate = signal(new Date());
  readonly selectedCalendarDay = signal(new Date().getDate());

  get calendarDays() {
    const d = this.currentDate();
    const year = d.getFullYear();
    const month = d.getMonth();
    const firstDayIndex = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const daysInPrevMonth = new Date(year, month, 0).getDate();

    const schedules = this.monthlySchedules();
    const scheduledDays = new Set(
      schedules
        .map(s => {
          const dt = new Date(s.date);
          return (dt.getFullYear() === year && dt.getMonth() === month) ? dt.getDate() : -1;
        })
        .filter(day => day > 0)
    );

    const days: { day: number; isCurrentMonth: boolean; hasSchedule: boolean }[] = [];
    for (let x = firstDayIndex; x > 0; x--) {
      days.push({ day: daysInPrevMonth - x + 1, isCurrentMonth: false, hasSchedule: false });
    }
    for (let i = 1; i <= daysInMonth; i++) {
      days.push({ day: i, isCurrentMonth: true, hasSchedule: scheduledDays.has(i) });
    }
    const remaining = 35 - days.length;
    for (let j = 1; j <= (remaining > 0 ? remaining : 0); j++) {
      days.push({ day: j, isCurrentMonth: false, hasSchedule: false });
    }
    return days;
  }

  prevMonth(): void {
    const d = new Date(this.currentDate());
    d.setMonth(d.getMonth() - 1);
    this.currentDate.set(d);
    this.loadMonthSchedules();
  }

  nextMonth(): void {
    const d = new Date(this.currentDate());
    d.setMonth(d.getMonth() + 1);
    this.currentDate.set(d);
    this.loadMonthSchedules();
  }

  getTrendPercent(val: number): number {
    const list = this.dashboard()?.enrollmentTrends;
    if (!list || list.length === 0) return 0;
    const max = Math.max(...list.map(r => r.count), 1);
    return Math.round((val / max) * 100);
  }

  ngOnInit(): void {
    this.loadDashboard();
    this.loadMonthSchedules();
    this.liveSync.enrollmentUpdated$.subscribe(() => {
      this.loadDashboard();
      this.loadMonthSchedules();
    });
  }

  loadMonthSchedules(): void {
    const d = this.currentDate();
    this.scheduleService.getSchedules(undefined, d.getMonth() + 1, d.getFullYear()).subscribe({
      next: (schedules) => this.monthlySchedules.set(schedules ?? []),
      error: () => this.monthlySchedules.set([])
    });
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.dashboardService.getAdminDashboard().subscribe({
      next: (data) => {
        this.dashboard.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  selectDay(day: number): void {
    this.selectedCalendarDay.set(day);
  }
}

