import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { Subscription } from 'rxjs';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { StudentService } from '../../services/student.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { StudentCourseProgressDetail } from '../../models/student-progress-detail.model';

@Component({
  selector: 'app-course-progress-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LayoutComponent,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatChipsModule
  ],
  templateUrl: './course-progress-details.component.html',
  styleUrls: ['./course-progress-details.component.scss']
})
export class CourseProgressDetailsComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private studentService = inject(StudentService);
  private liveSync = inject(LiveSyncService);

  readonly progressData = signal<StudentCourseProgressDetail | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly courseId = signal<number | null>(null);

  readonly activeTab = signal<'inProgress' | 'completed'>('inProgress');

  private sub?: Subscription;

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('courseId');
    const id = idParam ? parseInt(idParam, 10) : null;
    if (!id || isNaN(id)) {
      this.errorMessage.set('Invalid course identifier provided.');
      this.isLoading.set(false);
      return;
    }

    this.courseId.set(id);
    this.loadProgressDetails(id);

    // LiveSync subscriptions
    this.sub = this.liveSync.gradePublished$.subscribe((data) => {
      if (data && data.courseId === this.courseId()) {
        this.loadProgressDetails(this.courseId()!);
      }
    });

    this.sub.add(
      this.liveSync.scheduleUpdated$.subscribe(() => {
        if (this.courseId()) {
          this.loadProgressDetails(this.courseId()!);
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  loadProgressDetails(id: number): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.studentService.getCourseProgressDetails(id).subscribe({
      next: (data) => {
        this.progressData.set(data);
        // Default active tab to 'completed' if no in-progress assignments exist
        if (data.assignments.inProgress.length === 0 && data.assignments.completed.length > 0) {
          this.activeTab.set('completed');
        }
        this.isLoading.set(false);
      },
      error: (err) => {
        if (err.status === 404) {
          this.errorMessage.set('You are not enrolled in this course or the course could not be found.');
        } else if (err.status === 403) {
          this.errorMessage.set('You are not authorized to view progress for this course.');
        } else {
          this.errorMessage.set('Unable to load course progress details. Please try again later.');
        }
        this.isLoading.set(false);
      }
    });
  }

  setActiveTab(tab: 'inProgress' | 'completed'): void {
    this.activeTab.set(tab);
  }

  getDayName(dayOfWeek: number): string {
    const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    return days[dayOfWeek] || 'Unknown';
  }
}
