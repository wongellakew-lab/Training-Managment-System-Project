import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { InstructorWorkspaceService } from '../../services/instructor-workspace.service';
import {
  SubmissionItem,
  SubmissionDetail,
  AssignmentItem,
  InstructorCourseSummary
} from '../../models/instructor-workspace.model';

@Component({
  selector: 'app-instructor-submissions',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LayoutComponent,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './instructor-submissions.component.html',
  styleUrls: ['./instructor-submissions.component.scss']
})
export class InstructorSubmissionsComponent implements OnInit {
  private instructorService = inject(InstructorWorkspaceService);
  private route = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);

  courses = signal<InstructorCourseSummary[]>([]);
  selectedCourseId = signal<number | null>(null);

  assignments = signal<AssignmentItem[]>([]);
  selectedAssignmentId = signal<number | null>(null);

  submissions = signal<SubmissionItem[]>([]);
  selectedSubmission = signal<SubmissionDetail | null>(null);

  isLoading = signal<boolean>(true);
  isLoadingDetail = signal<boolean>(false);
  isSubmittingGrade = signal<boolean>(false);

  // Grading form state
  gradeScore = signal<number>(0);
  gradeFeedback = signal<string>('');

  ngOnInit(): void {
    const queryCourseId = this.route.snapshot.queryParamMap.get('courseId');
    const queryAssignId = this.route.snapshot.queryParamMap.get('assignmentId');

    this.instructorService.getMyCourses().subscribe({
      next: (list) => {
        this.courses.set(list || []);
        if (queryCourseId) {
          this.selectedCourseId.set(Number(queryCourseId));
        } else if (list.length > 0) {
          this.selectedCourseId.set(list[0].id);
        }

        if (this.selectedCourseId()) {
          this.loadAssignments(queryAssignId ? Number(queryAssignId) : undefined);
        } else {
          this.isLoading.set(false);
        }
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadAssignments(autoSelectId?: number): void {
    const cid = this.selectedCourseId();
    if (!cid) return;

    this.instructorService.getAssignments(cid).subscribe({
      next: (list) => {
        this.assignments.set(list || []);
        if (autoSelectId && list.some(a => a.id === autoSelectId)) {
          this.selectedAssignmentId.set(autoSelectId);
        } else if (list.length > 0) {
          this.selectedAssignmentId.set(list[0].id);
        }

        if (this.selectedAssignmentId()) {
          this.loadSubmissions();
        } else {
          this.isLoading.set(false);
        }
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadSubmissions(): void {
    const aid = this.selectedAssignmentId();
    if (!aid) return;

    this.isLoading.set(true);
    this.selectedSubmission.set(null);

    this.instructorService.getSubmissions(aid).subscribe({
      next: (list) => {
        this.submissions.set(list || []);
        this.isLoading.set(false);
        if (list && list.length > 0) {
          this.selectSubmission(list[0]);
        }
      },
      error: (err) => {
        console.error('Failed to load submissions:', err);
        this.isLoading.set(false);
      }
    });
  }

  onCourseChange(): void {
    this.loadAssignments();
  }

  onAssignmentChange(): void {
    this.loadSubmissions();
  }

  selectSubmission(sub: SubmissionItem): void {
    this.isLoadingDetail.set(true);
    this.instructorService.getSubmissionDetail(sub.id).subscribe({
      next: (detail) => {
        this.selectedSubmission.set(detail);
        this.gradeScore.set(detail.scoredPoints ?? 0);
        this.gradeFeedback.set(detail.feedback ?? '');
        this.isLoadingDetail.set(false);
      },
      error: () => this.isLoadingDetail.set(false)
    });
  }

  submitGrade(): void {
    const sub = this.selectedSubmission();
    if (!sub) return;

    if (this.gradeScore() < 0 || this.gradeScore() > sub.maxPoints) {
      this.snackBar.open(`Score must be between 0 and ${sub.maxPoints} points.`, 'Close', { duration: 3000 });
      return;
    }

    this.isSubmittingGrade.set(true);
    this.instructorService.gradeSubmission(sub.id, {
      scoredPoints: this.gradeScore(),
      feedback: this.gradeFeedback()
    }).subscribe({
      next: (res) => {
        this.isSubmittingGrade.set(false);
        this.snackBar.open('Grade and feedback saved! Student notified via live SignalR sync.', 'OK', { duration: 3500 });

        // Update local submission list status
        this.submissions.update(list => list.map(s => {
          if (s.id === sub.id) {
            return {
              ...s,
              status: 'Graded' as const,
              scoredPoints: res.scoredPoints,
              feedback: this.gradeFeedback(),
              gradedAt: res.gradedAt
            };
          }
          return s;
        }));

        if (this.selectedSubmission()) {
          this.selectedSubmission.update(curr => curr ? {
            ...curr,
            status: 'Graded' as const,
            scoredPoints: res.scoredPoints,
            feedback: this.gradeFeedback(),
            gradedAt: res.gradedAt
          } : null);
        }
      },
      error: () => {
        this.isSubmittingGrade.set(false);
        this.snackBar.open('Failed to submit grade.', 'Close', { duration: 3000 });
      }
    });
  }
}
