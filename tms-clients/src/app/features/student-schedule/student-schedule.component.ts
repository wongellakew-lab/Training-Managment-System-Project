import { Component, inject, OnInit, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { ScheduleService } from '../../services/schedule.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { Schedule } from '../../models/schedule.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-student-schedule',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    LayoutComponent
  ],
  templateUrl: './student-schedule.component.html',
  styleUrls: ['./student-schedule.component.scss']
})
export class StudentScheduleComponent implements OnInit, OnDestroy {
  private scheduleService = inject(ScheduleService);
  private liveSync = inject(LiveSyncService);
  private snackBar = inject(MatSnackBar);

  readonly schedules = signal<Schedule[]>([]);
  readonly selectedView = signal<'Day' | 'Week' | 'Month'>('Week');
  readonly selectedSchedule = signal<Schedule | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly currentDate = signal<Date>(new Date(2030, 4, 18)); // Academic baseline: May 18, 2030

  private sub?: Subscription;

  ngOnInit(): void {
    this.loadSchedule();

    // Listen for live updates
    this.sub = this.liveSync.scheduleUpdated$.subscribe(() => {
      this.loadSchedule();
      this.snackBar.open('Schedule updated in real-time!', 'Close', { duration: 3000 });
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  loadSchedule(): void {
    this.isLoading.set(true);
    const viewParam = this.selectedView().toLowerCase();
    const dateParam = this.currentDate().toISOString();

    this.scheduleService.getMySchedule(viewParam, dateParam).subscribe({
      next: (data) => {
        this.schedules.set(data);
        if (data.length > 0 && !this.selectedSchedule()) {
          this.selectedSchedule.set(data[0]);
        }
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  setView(view: 'Day' | 'Week' | 'Month'): void {
    this.selectedView.set(view);
    this.loadSchedule();
  }

  previousPeriod(): void {
    const d = new Date(this.currentDate());
    if (this.selectedView() === 'Day') d.setDate(d.getDate() - 1);
    else if (this.selectedView() === 'Week') d.setDate(d.getDate() - 7);
    else d.setMonth(d.getMonth() - 1);
    this.currentDate.set(d);
    this.loadSchedule();
  }

  nextPeriod(): void {
    const d = new Date(this.currentDate());
    if (this.selectedView() === 'Day') d.setDate(d.getDate() + 1);
    else if (this.selectedView() === 'Week') d.setDate(d.getDate() + 7);
    else d.setMonth(d.getMonth() + 1);
    this.currentDate.set(d);
    this.loadSchedule();
  }

  selectSchedule(item: Schedule): void {
    this.selectedSchedule.set(item);
  }

  getTypeClass(type?: string): string {
    const t = (type || 'lecture').toLowerCase();
    if (t.includes('lab')) return 'type-lab';
    if (t.includes('workshop')) return 'type-workshop';
    if (t.includes('exam')) return 'type-exam';
    return 'type-lecture';
  }
}
