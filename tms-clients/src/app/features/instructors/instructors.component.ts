import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { InstructorService, InstructorItem, InstructorStats } from '../../services/instructor.service';

@Component({
  selector: 'app-instructors-page',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule, LayoutComponent],
  template: `
    <app-layout pageTitle="Instructors">
      <div class="instructors-page-content">
        <!-- Top Stats -->
        <div class="instructor-stats-row">
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle blue"><mat-icon>co_present</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Total Instructors</span>
              <span class="stat-num">{{ stats()?.totalInstructors ?? 0 }}</span>
            </div>
          </div>
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle green"><mat-icon>verified</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Active Mentors</span>
              <span class="stat-num">{{ stats()?.activeMentors ?? 0 }}</span>
            </div>
          </div>
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle orange"><mat-icon>star</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Avg. Rating</span>
              <span class="stat-num">{{ (stats()?.averageRating ?? 0) | number:'1.2-2' }}</span>
            </div>
          </div>
        </div>

        <!-- Instructors Grid -->
        @if (isLoading()) {
          <div class="loading-box">
            <mat-spinner diameter="36"></mat-spinner>
            <span>Loading faculty directory...</span>
          </div>
        } @else if (instructors().length === 0) {
          <div class="empty-box">
            <mat-icon>co_present</mat-icon>
            <h4>No Faculty Mentors Found</h4>
            <p>Instructors added to the system will appear here.</p>
          </div>
        } @else {
          <div class="instructors-grid">
            @for (inst of instructors(); track inst.id) {
              <div class="eduva-card instructor-card">
                <div class="inst-card-top">
                  <img [src]="inst.avatar" alt="Instructor Avatar" class="inst-avatar-large" />
                  <span class="rating-badge"><mat-icon>star</mat-icon> {{ inst.rating }}</span>
                </div>
                <h4 class="inst-name">{{ inst.firstName }} {{ inst.lastName }}</h4>
                <span class="pill-badge category-design">{{ inst.department }}</span>
                <span class="inst-email">{{ inst.email }}</span>

                <div class="inst-metrics-footer">
                  <div class="metric-item">
                    <span class="val">{{ inst.coursesCount }}</span>
                    <span class="lbl">Courses</span>
                  </div>
                  <div class="metric-divider"></div>
                  <div class="metric-item">
                    <span class="val">{{ inst.studentsCount }}</span>
                    <span class="lbl">Students</span>
                  </div>
                </div>
              </div>
            }
          </div>
        }
      </div>
    </app-layout>
  `,
  styles: [`
    .instructors-page-content {
      display: flex;
      flex-direction: column;
      gap: 24px;
      padding-bottom: 24px;
    }
    .instructor-stats-row {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 20px;
      @media (max-width: 900px) { grid-template-columns: 1fr; }
    }
    .stat-item {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 16px 20px;
      .stat-icon-circle {
        width: 44px;
        height: 44px;
        border-radius: var(--radius-md);
        display: flex;
        align-items: center;
        justify-content: center;
        &.blue { background: #e0f2fe; color: #0284c7; }
        &.green { background: #ecfdf5; color: #10b981; }
        &.orange { background: #fffbeb; color: #f59e0b; }
      }
      .stat-content {
        display: flex;
        flex-direction: column;
        .stat-lbl { font-size: 0.75rem; color: var(--text-muted); }
        .stat-num { font-size: 1.5rem; font-weight: 800; color: var(--text-primary); }
      }
    }
    .instructors-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
      gap: 20px;
    }
    .instructor-card {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      gap: 10px;
      padding: 24px;
      .inst-card-top {
        position: relative;
        .inst-avatar-large {
          width: 72px;
          height: 72px;
          border-radius: 50%;
          object-fit: cover;
        }
        .rating-badge {
          position: absolute;
          bottom: 0;
          right: -8px;
          background: #ffffff;
          border: 1px solid var(--border-subtle);
          border-radius: var(--radius-full);
          padding: 2px 6px;
          font-size: 0.6875rem;
          font-weight: 700;
          color: #f59e0b;
          display: flex;
          align-items: center;
          gap: 2px;
          mat-icon { font-size: 12px; width: 12px; height: 12px; }
        }
      }
      .inst-name {
        margin: 4px 0 0 0;
        font-size: 1.0625rem;
        font-weight: 700;
        color: var(--text-primary);
      }
      .inst-email {
        font-size: 0.75rem;
        color: var(--text-muted);
      }
      .inst-metrics-footer {
        display: flex;
        align-items: center;
        justify-content: space-around;
        width: 100%;
        margin-top: 10px;
        padding-top: 12px;
        border-top: 1px solid #f1f5f9;
        .metric-item {
          display: flex;
          flex-direction: column;
          .val { font-size: 1rem; font-weight: 800; color: var(--text-primary); }
          .lbl { font-size: 0.6875rem; color: var(--text-muted); }
        }
        .metric-divider {
          width: 1px;
          height: 24px;
          background: var(--border-subtle);
        }
      }
    }
    .loading-box, .empty-box {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px 24px;
      gap: 12px;
      text-align: center;
      color: var(--text-muted);
      mat-icon { font-size: 40px; width: 40px; height: 40px; color: var(--text-muted); }
      h4 { margin: 0; color: var(--text-primary); font-size: 1.125rem; font-weight: 700; }
      p { margin: 0; font-size: 0.8125rem; }
    }
  `]
})
export class InstructorsComponent implements OnInit {
  private instructorService = inject(InstructorService);

  readonly instructors = signal<InstructorItem[]>([]);
  readonly stats = signal<InstructorStats | null>(null);
  readonly isLoading = signal(false);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);

    this.instructorService.getInstructors({ pageSize: 50 }).subscribe({
      next: (res) => {
        this.instructors.set(res.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });

    this.instructorService.getInstructorStats().subscribe({
      next: (st) => this.stats.set(st),
      error: () => {}
    });
  }
}
