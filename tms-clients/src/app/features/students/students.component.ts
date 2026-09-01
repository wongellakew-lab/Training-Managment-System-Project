import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { LayoutComponent } from '../../ui/layout/layout.component';

interface StudentItem {
  id: number;
  name: string;
  email: string;
  regNumber: string;
  coursesEnrolled: number;
  gpa: number;
  status: string;
  avatar: string;
}

@Component({
  selector: 'app-students-page',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, LayoutComponent],
  template: `
    <app-layout pageTitle="Students">
      <div class="students-page-content">
        <!-- Top Stats Row -->
        <div class="students-stats-row">
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle blue"><mat-icon>school</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Total Students</span>
              <span class="stat-num">18,500+</span>
            </div>
          </div>
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle green"><mat-icon>how_to_reg</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Active Enrolled</span>
              <span class="stat-num">14,280</span>
            </div>
          </div>
          <div class="eduva-card stat-item">
            <div class="stat-icon-circle cyan"><mat-icon>military_tech</mat-icon></div>
            <div class="stat-content">
              <span class="stat-lbl">Certificates Issued</span>
              <span class="stat-num">5,420</span>
            </div>
          </div>
        </div>

        <!-- Students Table -->
        <div class="eduva-card table-card">
          <div class="table-header">
            <h3 class="table-title">Student Directory</h3>
          </div>

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
                          <span class="name">{{ s.name }}</span>
                          <span class="email">{{ s.email }}</span>
                        </div>
                      </div>
                    </td>
                    <td>{{ s.regNumber }}</td>
                    <td><strong>{{ s.coursesEnrolled }} Courses</strong></td>
                    <td>
                      <span class="gpa-pill">{{ s.gpa.toFixed(2) }}</span>
                    </td>
                    <td>
                      <span class="pill-badge active">&bull; {{ s.status }}</span>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
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
    }
  `]
})
export class StudentsComponent implements OnInit {
  readonly students = signal<StudentItem[]>([
    { id: 1, name: 'Grasiella Lopez', email: 'grasiella.lopez@eduva.com', regNumber: 'REG-2030-001', coursesEnrolled: 3, gpa: 3.90, status: 'Active', avatar: 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&auto=format&fit=crop&q=80' },
    { id: 2, name: 'Hendrick Chow', email: 'hendrick.chow@eduva.com', regNumber: 'REG-2030-002', coursesEnrolled: 2, gpa: 3.70, status: 'Active', avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop&q=80' },
    { id: 3, name: 'Jenice Miller', email: 'jenice.miller@eduva.com', regNumber: 'REG-2030-003', coursesEnrolled: 4, gpa: 3.80, status: 'Active', avatar: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80' },
    { id: 4, name: 'Nigel Bruce', email: 'nigel.bruce@eduva.com', regNumber: 'REG-2030-004', coursesEnrolled: 2, gpa: 3.50, status: 'Active', avatar: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150&auto=format&fit=crop&q=80' },
    { id: 5, name: 'Amber Lunge', email: 'amber.lunge@eduva.com', regNumber: 'REG-2030-005', coursesEnrolled: 3, gpa: 3.95, status: 'Active', avatar: 'https://images.unsplash.com/photo-1580489944761-15a19d654956?w=150&auto=format&fit=crop&q=80' }
  ]);

  ngOnInit(): void {}
}
