import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { LayoutComponent } from '../../ui/layout/layout.component';

interface InstructorCard {
  id: number;
  name: string;
  department: string;
  email: string;
  coursesCount: number;
  studentsCount: number;
  rating: number;
  avatar: string;
}

@Component({
  selector: 'app-instructors-page',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, LayoutComponent],
  template: `
    <app-layout pageTitle="Instructors">
      <div class="instructors-page-content">
        <!-- Top Stats -->
        <div class="instructor-stats-row">
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle blue"><mat-icon>co_present</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Total Instructors</span>
              <span class="stat-num">85+</span>
            </div>
          </div>
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle green"><mat-icon>verified</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Active Mentors</span>
              <span class="stat-num">72</span>
            </div>
          </div>
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle orange"><mat-icon>star</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Avg. Rating</span>
              <span class="stat-num">4.85</span>
            </div>
          </div>
        </div>

        <!-- Instructors Grid -->
        <div class="instructors-grid">
          @for (inst of instructors(); track inst.id) {
            <div class="eduva-card instructor-card">
              <div class="inst-card-top">
                <img [src]="inst.avatar" alt="Instructor Avatar" class="inst-avatar-large" />
                <span class="rating-badge"><mat-icon>star</mat-icon> {{ inst.rating }}</span>
              </div>
              <h4 class="inst-name">{{ inst.name }}</h4>
              <span class="pill-badge category-design">{{ inst.department }}</span>
              <span class="inst-email">{{ inst.email }}</span>

              <div class="inst-metrics-footer">
                <div class="metric-item">
                  <span class="val">{{ inst.coursesCount }}</span>
                  <span class="lbl">Courses</span>
                </div>
                <div class="metric-divider"></div>
                <div class="metric-item">
                  <span class="val">{{ inst.studentsCount }}+</span>
                  <span class="lbl">Students</span>
                </div>
              </div>
            </div>
          }
        </div>
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
  `]
})
export class InstructorsComponent implements OnInit {
  readonly instructors = signal<InstructorCard[]>([
    { id: 1, name: 'Gina Alexander', department: 'Web Dev', email: 'gina.alexander@eduva.com', coursesCount: 5, studentsCount: 1450, rating: 4.9, avatar: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80' },
    { id: 2, name: 'Elise Wong', department: 'Design', email: 'elise.wong@eduva.com', coursesCount: 4, studentsCount: 1180, rating: 4.8, avatar: 'https://images.unsplash.com/photo-1580489944761-15a19d654956?w=150&auto=format&fit=crop&q=80' },
    { id: 3, name: 'Fabricio Conrad', department: 'Marketing', email: 'fabricio.conrad@eduva.com', coursesCount: 6, studentsCount: 980, rating: 4.7, avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop&q=80' },
    { id: 4, name: 'Angela Cole', department: 'Communication', email: 'angela.cole@eduva.com', coursesCount: 3, studentsCount: 820, rating: 4.6, avatar: 'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=150&auto=format&fit=crop&q=80' },
    { id: 5, name: 'Mason James', department: 'Web Dev', email: 'mason.james@eduva.com', coursesCount: 4, studentsCount: 1250, rating: 4.8, avatar: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150&auto=format&fit=crop&q=80' },
    { id: 6, name: 'Jacqueline Hart', department: 'Design', email: 'jacqueline.hart@eduva.com', coursesCount: 5, studentsCount: 1320, rating: 4.9, avatar: 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150&auto=format&fit=crop&q=80' }
  ]);

  ngOnInit(): void {}
}
