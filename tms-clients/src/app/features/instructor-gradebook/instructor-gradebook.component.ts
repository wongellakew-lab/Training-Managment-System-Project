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

interface PendingCellChange {
  studentId: number;
  assignmentId: number;
  score: number | null;
  rawValue: string;
}

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

  // Pending changes and validation tracking
  pendingChanges = signal<Map<string, PendingCellChange>>(new Map());
  cellErrors = signal<Map<string, string>>(new Map());
  savingCells = signal<Set<string>>(new Set());
  submitStatus = signal<'idle' | 'submitting' | 'success' | 'error'>('idle');
  submitMessage = signal<string>('');

  hasPendingChanges = computed(() => this.pendingChanges().size > 0);
  hasValidationErrors = computed(() => this.cellErrors().size > 0);

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
    this.pendingChanges.set(new Map());
    this.cellErrors.set(new Map());
    this.submitStatus.set('idle');

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

  getScore(row: GradebookRow, assignmentId: number): any {
    const key = `${row.studentId}_${assignmentId}`;
    if (this.pendingChanges().has(key)) {
      return this.pendingChanges().get(key)!.rawValue;
    }
    const cell = row.grades?.find(g => g.assignmentId === assignmentId);
    return cell?.score !== undefined && cell?.score !== null ? cell.score : '';
  }

  hasCellChanged(studentId: number, assignmentId: number): boolean {
    return this.pendingChanges().has(`${studentId}_${assignmentId}`);
  }

  isCellInvalid(studentId: number, assignmentId: number): boolean {
    return this.cellErrors().has(`${studentId}_${assignmentId}`);
  }

  getCellError(studentId: number, assignmentId: number): string | null {
    return this.cellErrors().get(`${studentId}_${assignmentId}`) || null;
  }

  isCellSaving(studentId: number, assignmentId: number): boolean {
    return this.savingCells().has(`${studentId}_${assignmentId}`);
  }

  onCellEnter(row: GradebookRow, assignment: GradebookAssignmentHeader, event: Event): void {
    event.preventDefault();
    this.saveCellGrade(row, assignment, event.target as HTMLInputElement);
  }

  saveCellGrade(row: GradebookRow, assignment: GradebookAssignmentHeader, inputEl?: HTMLInputElement): void {
    const cid = this.selectedCourseId();
    if (!cid) return;

    const cellKey = `${row.studentId}_${assignment.id}`;
    if (this.savingCells().has(cellKey)) {
      return; // Prevent duplicate requests while saving
    }

    // Determine current value
    const rawVal = inputEl ? inputEl.value : (this.pendingChanges().get(cellKey)?.rawValue ?? String(this.getScore(row, assignment.id) ?? ''));
    const val = rawVal.trim();

    // Validation: A grade must NEVER be submitted when the grade input is empty
    if (val === '') {
      this.setCellError(cellKey, 'Grade is required.');
      this.snackBar.open('Grade is required.', 'Close', { duration: 3000 });
      this.focusCell(row.studentId, assignment.id);
      return;
    }

    const num = Number(val);
    if (isNaN(num)) {
      this.setCellError(cellKey, 'Must be a valid number');
      this.snackBar.open('Please enter a valid numeric grade.', 'Close', { duration: 3000 });
      this.focusCell(row.studentId, assignment.id);
      return;
    }
    if (num < 0) {
      this.setCellError(cellKey, 'Grade cannot be negative');
      this.snackBar.open('Grade cannot be negative.', 'Close', { duration: 3000 });
      this.focusCell(row.studentId, assignment.id);
      return;
    }
    if (num > assignment.maxPoints) {
      this.setCellError(cellKey, `Cannot exceed ${assignment.maxPoints} pts`);
      this.snackBar.open(`Grade cannot exceed ${assignment.maxPoints} pts.`, 'Close', { duration: 3000 });
      this.focusCell(row.studentId, assignment.id);
      return;
    }
    const scoreToSubmit: number = num;

    // Clear any previous error on this cell
    this.clearCellError(cellKey);

    // Lock cell during save
    const updatedSaving = new Set(this.savingCells());
    updatedSaving.add(cellKey);
    this.savingCells.set(updatedSaving);

    this.instructorService.updateGradebookCell({
      courseId: cid,
      studentId: row.studentId,
      assignmentId: assignment.id,
      score: scoreToSubmit
    }).subscribe({
      next: (res) => {
        // Unlock cell
        const currentSaving = new Set(this.savingCells());
        currentSaving.delete(cellKey);
        this.savingCells.set(currentSaving);

        // Remove from pending changes if present
        const pending = new Map(this.pendingChanges());
        pending.delete(cellKey);
        this.pendingChanges.set(pending);

        // Update local gradebook cell
        let cell = row.grades?.find(g => g.assignmentId === assignment.id);
        if (!cell) {
          cell = {
            assignmentId: assignment.id,
            maxScore: assignment.maxPoints,
            status: scoreToSubmit !== null ? 'Graded' : 'Pending',
            isGraded: scoreToSubmit !== null
          };
          if (!row.grades) row.grades = [];
          row.grades.push(cell);
        }

        cell.score = scoreToSubmit !== null ? scoreToSubmit : undefined;
        cell.isGraded = scoreToSubmit !== null;
        cell.status = scoreToSubmit !== null ? 'Graded' : 'Pending';

        // Recompute row totals and completion
        const totalRequired = this.assignments().length;
        const graded = row.grades.filter(g => g.isGraded && g.score !== undefined);
        const isComplete = totalRequired > 0 && graded.length === totalRequired;

        if (isComplete) {
          row.totalScore = graded.reduce((acc, g) => acc + (g.score || 0), 0);
          row.totalMaxScore = this.assignments().reduce((acc, a) => acc + a.maxPoints, 0);
          row.averagePercentage = row.totalMaxScore > 0 ? Math.round(((row.totalScore || 0) / row.totalMaxScore) * 100) : 0;
          row.letterGrade = this.calculateLetter(row.averagePercentage);
          row.isGradingComplete = true;
        } else {
          row.totalScore = null;
          row.totalMaxScore = null;
          row.averagePercentage = null;
          row.letterGrade = 'Pending';
          row.isGradingComplete = false;
        }
        row.gradedAssessmentCount = graded.length;
        row.requiredAssessmentCount = totalRequired;

        this.snackBar.open(`Saved grade for ${row.studentName} (${assignment.title}).`, 'OK', { duration: 2500 });

        // Advance to NEXT STUDENT for the SAME ASSIGNMENT
        const currentRows = this.rows();
        const currentIndex = currentRows.findIndex(r => r.studentId === row.studentId);
        if (currentIndex >= 0 && currentIndex < currentRows.length - 1) {
          const nextStudent = currentRows[currentIndex + 1];
          this.focusCell(nextStudent.studentId, assignment.id);
        } else {
          // Last student in list: keep focus on current student, do not wrap or move assignment
          this.focusCell(row.studentId, assignment.id);
        }
      },
      error: (err) => {
        // Unlock cell
        const currentSaving = new Set(this.savingCells());
        currentSaving.delete(cellKey);
        this.savingCells.set(currentSaving);

        const errorMsg = err.error?.message || 'Failed to save grade.';
        this.setCellError(cellKey, errorMsg);
        this.snackBar.open(errorMsg, 'Close', { duration: 4000 });

        // Keep focus on current student's input
        this.focusCell(row.studentId, assignment.id);
      }
    });
  }

  private setCellError(key: string, message: string): void {
    const errors = new Map(this.cellErrors());
    errors.set(key, message);
    this.cellErrors.set(errors);
  }

  private clearCellError(key: string): void {
    if (this.cellErrors().has(key)) {
      const errors = new Map(this.cellErrors());
      errors.delete(key);
      this.cellErrors.set(errors);
    }
  }

  private focusCell(studentId: number, assignmentId: number): void {
    setTimeout(() => {
      const el = document.getElementById(`grade-cell-${studentId}-${assignmentId}`) as HTMLInputElement | null;
      if (el) {
        el.focus();
        el.select();
      }
    }, 50);
  }

  onCellInput(row: GradebookRow, assignment: GradebookAssignmentHeader, event: Event): void {
    const input = event.target as HTMLInputElement;
    const val = input.value.trim();
    const cellKey = `${row.studentId}_${assignment.id}`;

    const errors = new Map(this.cellErrors());
    const pending = new Map(this.pendingChanges());

    if (val === '') {
      // Cleared / empty value -> invalid: grade is required
      errors.set(cellKey, 'Grade is required.');
      pending.set(cellKey, {
        studentId: row.studentId,
        assignmentId: assignment.id,
        score: null,
        rawValue: val
      });
    } else {
      const num = Number(val);
      if (isNaN(num)) {
        errors.set(cellKey, 'Must be a valid number');
      } else if (num < 0) {
        errors.set(cellKey, 'Grade cannot be negative');
      } else if (num > assignment.maxPoints) {
        errors.set(cellKey, `Cannot exceed ${assignment.maxPoints} pts`);
      } else {
        errors.delete(cellKey);
      }

      pending.set(cellKey, {
        studentId: row.studentId,
        assignmentId: assignment.id,
        score: isNaN(num) ? null : num,
        rawValue: val
      });
    }

    this.cellErrors.set(errors);
    this.pendingChanges.set(pending);
    this.submitStatus.set('idle');
  }

  submitGrades(): void {
    const cid = this.selectedCourseId();
    if (!cid) return;

    const changes = Array.from(this.pendingChanges().values());
    if (changes.length === 0) {
      this.snackBar.open('No pending grade changes to submit.', 'Dismiss', { duration: 2500 });
      return;
    }

    // Check for any empty/whitespace or invalid entries among changes
    const emptyOrInvalid = changes.filter(c => c.score === null || c.rawValue.trim() === '');
    if (emptyOrInvalid.length > 0) {
      emptyOrInvalid.forEach(c => this.setCellError(`${c.studentId}_${c.assignmentId}`, 'Grade is required.'));
      this.snackBar.open('Grade is required.', 'Close', { duration: 3500 });
      const first = emptyOrInvalid[0];
      this.focusCell(first.studentId, first.assignmentId);
      return;
    }

    if (this.hasValidationErrors()) {
      this.snackBar.open('Please resolve invalid grade entries (highlighted in red) before submitting.', 'Close', { duration: 4000 });
      return;
    }

    this.submitStatus.set('submitting');

    const payload = changes.map(c => ({
      studentId: c.studentId,
      assignmentId: c.assignmentId,
      score: c.score!
    }));

    this.instructorService.submitBulkGradebook(cid, payload).subscribe({
      next: () => {
        this.submitStatus.set('success');
        this.submitMessage.set('Grades submitted successfully');
        this.snackBar.open('Grades submitted successfully', 'OK', { duration: 3500 });

        // Update local gradebook rows data to reflect submitted changes
        changes.forEach(change => {
          const row = this.rows().find(r => r.studentId === change.studentId);
          if (row) {
            let cell = row.grades?.find(g => g.assignmentId === change.assignmentId);
            if (!cell) {
              cell = {
                assignmentId: change.assignmentId,
                maxScore: this.assignments().find(a => a.id === change.assignmentId)?.maxPoints || 100,
                status: change.score !== null ? 'Graded' : 'Pending',
                isGraded: change.score !== null
              };
              if (!row.grades) row.grades = [];
              row.grades.push(cell);
            }

            cell.score = change.score !== null ? change.score : undefined;
            cell.isGraded = change.score !== null;
            cell.status = change.score !== null ? 'Graded' : 'Pending';

            // Recompute row totals and completion
            const totalRequired = this.assignments().length;
            const graded = row.grades.filter(g => g.isGraded && g.score !== undefined);
            const isComplete = totalRequired > 0 && graded.length === totalRequired;

            if (isComplete) {
              row.totalScore = graded.reduce((acc, g) => acc + (g.score || 0), 0);
              row.totalMaxScore = this.assignments().reduce((acc, a) => acc + a.maxPoints, 0);
              row.averagePercentage = row.totalMaxScore > 0 ? Math.round(((row.totalScore || 0) / row.totalMaxScore) * 100) : 0;
              row.letterGrade = this.calculateLetter(row.averagePercentage);
              row.isGradingComplete = true;
            } else {
              row.totalScore = null;
              row.totalMaxScore = null;
              row.averagePercentage = null;
              row.letterGrade = 'Pending';
              row.isGradingComplete = false;
            }
            row.gradedAssessmentCount = graded.length;
            row.requiredAssessmentCount = totalRequired;
          }
        });

        this.pendingChanges.set(new Map());
        this.cellErrors.set(new Map());

        setTimeout(() => {
          if (this.submitStatus() === 'success') {
            this.submitStatus.set('idle');
          }
        }, 3500);
      },
      error: (err) => {
        this.submitStatus.set('error');
        this.submitMessage.set('Unable to submit grades. Please try again.');
        const msg = err.error?.message || 'Unable to submit grades. Please try again.';
        this.snackBar.open(msg, 'Close', { duration: 4000 });
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
