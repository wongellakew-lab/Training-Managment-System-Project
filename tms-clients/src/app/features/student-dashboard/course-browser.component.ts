import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NavbarComponent } from '../../ui/navbar/navbar.component';
import { CourseCardComponent } from '../../ui/course-card/course-card.component';
import { CourseStore } from '../../store/course.store';
import { EnrollmentStore } from '../../store/enrollment.store';
import { AuthService } from '../../services/auth.service';
import { Course } from '../../models/course.model';
import { LiveSyncService } from '../../services/live-sync.service';

@Component({
  selector: 'app-course-browser',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    NavbarComponent,
    CourseCardComponent,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './course-browser.component.html',
  styleUrls: ['./course-browser.component.scss']
})
export class CourseBrowserComponent implements OnInit {
  courseStore = inject(CourseStore);
  enrollmentStore = inject(EnrollmentStore);
  authService = inject(AuthService);
  liveSync = inject(LiveSyncService);
  router = inject(Router);

  searchTerm = '';
  departmentFilter = '';
  useCachedVersion = signal(false);

  alertMessage = signal<string | null>(null);
  isSuccessAlert = signal(true);

  ngOnInit(): void {
    this.searchCourses();

    // Listen to real-time capacity updates
    this.liveSync.courseCapacityChanged$.subscribe((evt) => {
      this.courseStore.updateCapacity(evt.courseId, evt.enrolledCount, evt.maxCapacity);
    });
  }

  searchCourses(): void {
    const params = {
      pageNumber: 1,
      pageSize: 9,
      search: this.searchTerm || undefined,
      department: this.departmentFilter || undefined
    };

    if (this.useCachedVersion()) {
      this.courseStore.loadCachedCourses(params);
    } else {
      this.courseStore.loadCourses(params);
    }
  }

  changePage(newPage: number): void {
    const params = {
      pageNumber: newPage,
      pageSize: 9,
      search: this.searchTerm || undefined,
      department: this.departmentFilter || undefined
    };

    if (this.useCachedVersion()) {
      this.courseStore.loadCachedCourses(params);
    } else {
      this.courseStore.loadCourses(params);
    }
  }

  async onEnrollCourse(course: Course): Promise<void> {
    const studentId = this.authService.currentUser()?.studentId || 0;
    if (studentId === 0) {
      this.isSuccessAlert.set(false);
      this.alertMessage.set('You must be logged in as an enrolled student to register for this course.');
      return;
    }
    try {
      await this.enrollmentStore.enroll({
        studentId,
        courseId: course.id
      });
      this.isSuccessAlert.set(true);
      this.alertMessage.set(`Successfully enrolled in ${course.title} (${course.code})!`);
      this.searchCourses();
    } catch (err: any) {
      this.isSuccessAlert.set(false);
      this.alertMessage.set(err.message || 'Enrollment failed. Course may be at maximum capacity.');
    }
  }
}
