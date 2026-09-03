import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { StudentService, StudentItem, StudentStats } from '../../services/student.service';

@Component({
  selector: 'app-students-page',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule, LayoutComponent],
  template: `
    <app-layout pageTitle="Students">
      <div class="students-page-content">
        <!-- Top Stats Row -->
        <div class="students-stats-row">
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle blue"><mat-icon>school</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Total Students</span>
              <span class="stat-num">{{ stats()?.totalStudents ?? 0 }}</span>
            </div>
          </div>
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle green"><mat-icon>how_to_reg</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Active Enrolled</span>
              <span class="stat-num">{{ stats()?.activeEnrolled ?? 0 }}</span>
            </div>
          </div>
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle cyan"><mat-icon>military_tech</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Certificates Issued</span>
              <span class="stat-num">{{ stats()?.certificatesIssued ?? 0 }}</span>
            </div>
          </div>
        </div>

        <!-- Students Table -->
        <div class="eduva-card table-card">
          <div class="table-header">
            <h3 class="table-title">Student Directory</h3>
          </div>

          @if (isLoading()) {
            <div class="loading-box">
              <mat-spinner diameter="36"></mat-spinner>
              <span>Loading student directory...</span>
            </div>
          } @else if (students().length === 0) {
            <div class="empty-box">
              <mat-icon>school</mat-icon>
              <h4>No Students Registered</h4>
              <p>Registered students will appear in this directory with their academic progress.</p>
            </div>
          } @else {
            <div class="table-responsive">
              <table class="students-table">
                <thead>
                  <tr>
                    <th>Student</th>
                    <th>Reg Number</th>
                    <th>Enrolled Courses</th>
                    <th>GPA</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  @for (s of students(); track s.id) {
                    <tr>
                      <td>
                        <div class="student-cell">
                          <img [src]="s.avatar" alt="Avatar" class="avatar-img" />
                          <div class="info-box">
                            <span class="name">{{ s.firstName }} {{ s.lastName }}</span>
                            <span class="email">{{ s.email }}</span>
                          </div>
                        </div>
                      </td>
                      <td>{{ s.registrationNumber }}</td>
                      <td><strong>{{ s.coursesEnrolled }} Courses</strong></td>
                      <td>
                        <span class="gpa-pill">{{ s.gpa.toFixed(2) }}</span>
                      </td>
                      <td>
                        <span class="pill-badge" [ngClass]="s.status.toLowerCase()">&bull; {{ s.status }}</span>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </div>
      </div>
    </app-layout>
  `,
  styles: [`
    .students-page-content {
      display: flex;
      flex-direction: column;
      gap: 24px;
      padding-bottom: 24px;
    }
    .students-stats-row {
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
        &.cyan { background: #ecfeff; color: #0891b2; }
      }
      .stat-content {
        display: flex;
        flex-direction: column;
        .stat-lbl { font-size: 0.75rem; color: var(--text-muted); }
        .stat-num { font-size: 1.5rem; font-weight: 800; color: var(--text-primary); }
      }
    }
    .table-card {
      .table-header {
        margin-bottom: 16px;
        .table-title { font-size: 1.125rem; font-weight: 700; color: var(--text-primary); margin: 0; }
      }
      .students-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.8125rem;
        thead tr {
          border-bottom: 1px solid var(--border-subtle);
          th { text-align: left; padding: 12px 16px; color: var(--text-muted); font-weight: 600; }
        }
        tbody tr {
          border-bottom: 1px solid #f1f5f9;
          td { padding: 14px 16px; }
        }
      }
      .student-cell {
        display: flex;
        align-items: center;
        gap: 12px;
        .avatar-img { width: 34px; height: 34px; border-radius: 50%; object-fit: cover; }
        .info-box {
          display: flex;
          flex-direction: column;
          .name { font-weight: 700; color: var(--text-primary); }
          .email { font-size: 0.6875rem; color: var(--text-muted); }
        }
      }
      .gpa-pill {
        background: #f1f5f9;
        padding: 3px 8px;
        border-radius: 4px;
        font-weight: 700;
        color: var(--text-primary);
      }
      .pill-badge {
        display: inline-block;
        padding: 3px 8px;
        border-radius: var(--radius-full);
        font-size: 0.6875rem;
        font-weight: 600;
        &.active { background: #ecfdf5; color: #059669; }
        &.enrolled { background: #eff6ff; color: #2563eb; }
        &.inactive { background: #f1f5f9; color: #64748b; }
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
export class StudentsComponent implements OnInit {
  private studentService = inject(StudentService);

  readonly students = signal<StudentItem[]>([]);
  readonly stats = signal<StudentStats | null>(null);
  readonly isLoading = signal(false);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);

    this.studentService.getStudents({ pageSize: 50 }).subscribe({
      next: (res) => {
        this.students.set(res.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });

    this.studentService.getStudentStats().subscribe({
      next: (st) => this.stats.set(st),
      error: () => {}
    });
  }
}
