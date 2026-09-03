import { Component, inject, OnInit, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { GradeService } from '../../services/grade.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { StudentAcademicRecord, StudentGradeRecord, AssessmentGradeItem } from '../../models/grade.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-student-gradebook',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatIconModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    LayoutComponent
  ],
  templateUrl: './student-gradebook.component.html',
  styleUrls: ['./student-gradebook.component.scss']
})
export class StudentGradebookComponent implements OnInit, OnDestroy {
  private gradeService = inject(GradeService);
  private liveSync = inject(LiveSyncService);

  readonly academicRecord = signal<StudentAcademicRecord | null>(null);
  readonly selectedCourseGrade = signal<StudentGradeRecord | null>(null);
  readonly isLoading = signal<boolean>(true);

  private sub?: Subscription;

  ngOnInit(): void {
    this.loadGrades();

    // Live update on grade published
    this.sub = this.liveSync.gradePublished$.subscribe(() => {
      this.loadGrades();
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  loadGrades(): void {
    this.isLoading.set(true);
    this.gradeService.getMyAcademicRecord().subscribe({
      next: (data) => {
        this.academicRecord.set(data);
        if (data.grades.length > 0 && !this.selectedCourseGrade()) {
          this.selectedCourseGrade.set(data.grades[0]);
        }
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  selectCourse(grade: StudentGradeRecord): void {
    this.selectedCourseGrade.set(grade);
  }

  getRubricItems(json?: string): any[] {
    if (!json) return [];
    try {
      return JSON.parse(json);
    } catch {
      return [];
    }
  }
}
