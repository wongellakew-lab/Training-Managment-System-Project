import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { InstructorWorkspaceService } from '../../services/instructor-workspace.service';
import { InstructorCourseSummary } from '../../models/instructor-workspace.model';

@Component({
  selector: 'app-instructor-courses',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LayoutComponent,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './instructor-courses.component.html',
  styleUrls: ['./instructor-courses.component.scss']
})
export class InstructorCoursesComponent implements OnInit {
  private instructorService = inject(InstructorWorkspaceService);
  private router = inject(Router);

  isLoading = signal<boolean>(true);
  courses = signal<InstructorCourseSummary[]>([]);

  ngOnInit(): void {
    this.loadCourses();
  }

  loadCourses(): void {
    this.isLoading.set(true);
    this.instructorService.getMyCourses().subscribe({
      next: (list) => {
        this.courses.set(list || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load instructor courses:', err);
        this.isLoading.set(false);
      }
    });
  }

  openBuilder(courseId: number): void {
    this.router.navigate(['/instructor/courses', courseId, 'builder']);
  }

  openAttendance(courseId: number): void {
    this.router.navigate(['/instructor/attendance'], { queryParams: { courseId } });
  }

  openAssignments(courseId: number): void {
    this.router.navigate(['/instructor/assignments'], { queryParams: { courseId } });
  }

  openGradebook(courseId: number): void {
    this.router.navigate(['/instructor/gradebook'], { queryParams: { courseId } });
  }
}
