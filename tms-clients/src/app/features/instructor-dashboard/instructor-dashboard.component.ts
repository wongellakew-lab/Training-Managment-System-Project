import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { AuthService } from '../../services/auth.service';
import { InstructorWorkspaceService } from '../../services/instructor-workspace.service';
import { InstructorDashboard, InstructorStudentQuestion } from '../../models/instructor-workspace.model';

@Component({
  selector: 'app-instructor-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    LayoutComponent,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './instructor-dashboard.component.html',
  styleUrls: ['./instructor-dashboard.component.scss']
})
export class InstructorDashboardComponent implements OnInit {
  authService = inject(AuthService);
  private instructorService = inject(InstructorWorkspaceService);
  private router = inject(Router);

  isLoading = signal<boolean>(true);
  dashboard = signal<InstructorDashboard | null>(null);
  activeQuestion = signal<InstructorStudentQuestion | null>(null);
  replyText = signal<string>('');
  isSubmittingReply = signal<boolean>(false);

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.instructorService.getDashboard().subscribe({
      next: (data) => {
        this.dashboard.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load instructor dashboard:', err);
        this.isLoading.set(false);
      }
    });
  }

  goToAttendance(courseId: number): void {
    this.router.navigate(['/instructor/attendance'], { queryParams: { courseId } });
  }

  goToSubmissions(assignmentId: number, courseId: number): void {
    this.router.navigate(['/instructor/submissions'], { queryParams: { assignmentId, courseId } });
  }

  goToCourseBuilder(courseId: number): void {
    this.router.navigate(['/instructor/courses', courseId, 'builder']);
  }

  openReply(q: InstructorStudentQuestion): void {
    this.activeQuestion.set(q);
    this.replyText.set(q.answer || '');
  }

  closeReply(): void {
    this.activeQuestion.set(null);
    this.replyText.set('');
  }

  submitReply(): void {
    const q = this.activeQuestion();
    if (!q || !this.replyText().trim()) return;

    this.isSubmittingReply.set(true);
    this.instructorService.replyQuestion(q.id, this.replyText().trim()).subscribe({
      next: () => {
        this.isSubmittingReply.set(false);
        this.closeReply();
        this.loadDashboard();
      },
      error: () => {
        this.isSubmittingReply.set(false);
      }
    });
  }
}
