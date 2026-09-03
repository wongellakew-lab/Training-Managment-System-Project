import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { InstructorWorkspaceService } from '../../services/instructor-workspace.service';
import {
  GradebookResponse,
  GradebookRow,
  GradebookAssignmentHeader,
  InstructorCourseSummary
} from '../../models/instructor-workspace.model';

@Component({
  selector: 'app-instructor-gradebook',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LayoutComponent,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './instructor-gradebook.component.html',
  styleUrls: ['./instructor-gradebook.component.scss']
})
export class InstructorGradebookComponent implements OnInit {
  private instructorService = inject(InstructorWorkspaceService);
  private route = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);

  courses = signal<InstructorCourseSummary[]>([]);
  selectedCourseId = signal<number | null>(null);
  gradebookData = signal<GradebookResponse | null>(null);
  assignments = signal<GradebookAssignmentHeader[]>([]);
  rows = signal<GradebookRow[]>([]);
  isLoading = signal<boolean>(true);
  isSavingCell = signal<boolean>(false);

  // Quick Stats
  classAverage = computed(() => {
    const r = this.rows();
    if (!r.length) return 0;
    const sum = r.reduce((acc, row) => acc + (row.averagePercentage || 0), 0);
    return Math.round(sum / r.length);
  });

  passingCount = computed(() => {
    return this.rows().filter(r => (r.averagePercentage || 0) >= 60).length;
  });

  ngOnInit(): void {
    const queryCourseId = this.route.snapshot.queryParamMap.get('courseId');

    this.instructorService.getMyCourses().subscribe({
      next: (list) => {
        this.courses.set(list || []);
        if (queryCourseId) {
          this.selectedCourseId.set(Number(queryCourseId));
        } else if (list.length > 0) {
          this.selectedCourseId.set(list[0].id);
        }
        if (this.selectedCourseId()) {
          this.loadGradebook();
        } else {
          this.isLoading.set(false);
        }
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadGradebook(): void {
    const cid = this.selectedCourseId();
    if (!cid) return;

    this.isLoading.set(true);
    this.instructorService.getGradebook(cid).subscribe({
      next: (data) => {
        this.gradebookData.set(data);
        this.assignments.set(data.assignments || []);
        this.rows.set(data.rows || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load gradebook:', err);
        this.isLoading.set(false);
      }
    });
  }

  onCourseChange(): void {
    this.loadGradebook();
  }

  onScoreChange(row: GradebookRow, assignment: GradebookAssignmentHeader, scoreVal: any): void {
    const cid = this.selectedCourseId();
    if (!cid) return;

    let numericVal: number | null = null;
    if (scoreVal !== '' && scoreVal !== null && scoreVal !== undefined) {
      numericVal = Number(scoreVal);
      if (isNaN(numericVal)) {
        numericVal = null;
      } else {
        if (numericVal < 0) numericVal = 0;
        if (numericVal > assignment.maxPoints) numericVal = assignment.maxPoints;
      }
    }

    // Update local cell
    const cell = row.grades.find(g => g.assignmentId === assignment.id);
    if (cell) {
      cell.score = numericVal !== null ? numericVal : undefined;
      cell.isGraded = numericVal !== null;
      cell.status = numericVal !== null ? 'Graded' : 'Pending';
    }

    // Live recompute row totals
    const graded = row.grades.filter(g => g.isGraded && g.score !== undefined);
    row.totalScore = graded.reduce((acc, g) => acc + (g.score || 0), 0);
    row.totalMaxScore = graded.reduce((acc, g) => acc + g.maxScore, 0);
    row.averagePercentage = row.totalMaxScore > 0 ? Math.round((row.totalScore / row.totalMaxScore) * 100) : 0;
    row.letterGrade = this.calculateLetter(row.averagePercentage);

    // Persist to DB
    this.isSavingCell.set(true);
    this.instructorService.updateGradebookCell({
      courseId: cid,
      studentId: row.studentId,
      assignmentId: assignment.id,
      score: numericVal
    }).subscribe({
      next: () => {
        this.isSavingCell.set(false);
        this.snackBar.open('Grade saved.', 'OK', { duration: 1500 });
      },
      error: () => {
        this.isSavingCell.set(false);
        this.snackBar.open('Failed to save grade cell.', 'Dismiss', { duration: 3000 });
      }
    });
  }

  private calculateLetter(pct: number): string {
    if (pct >= 90) return 'A';
    if (pct >= 80) return 'B';
    if (pct >= 70) return 'C';
    if (pct >= 60) return 'D';
    return 'F';
  }
}
