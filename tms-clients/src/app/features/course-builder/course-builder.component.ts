import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatRadioModule } from '@angular/material/radio';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { AuthService } from '../../services/auth.service';
import { InstructorWorkspaceService } from '../../services/instructor-workspace.service';
import {
  CourseBuilderDetail,
  BuilderModule,
  BuilderLesson,
  CourseResourceItem
} from '../../models/instructor-workspace.model';

@Component({
  selector: 'app-course-builder',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    LayoutComponent,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatRadioModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './course-builder.component.html',
  styleUrls: ['./course-builder.component.scss']
})
export class CourseBuilderComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private instructorService = inject(InstructorWorkspaceService);
  private snackBar = inject(MatSnackBar);
  authService = inject(AuthService);

  courseId = signal<number>(0);
  isLoading = signal<boolean>(true);
  isSaving = signal<boolean>(false);
  isUploadingThumbnail = signal<boolean>(false);
  isUploadingResource = signal<boolean>(false);

  // Form Model
  course = signal<CourseBuilderDetail>({
    id: 0,
    code: '',
    title: '',
    description: '',
    about: '',
    category: 'Web Dev',
    level: 'Beginner',
    status: 'Draft',
    durationHours: 12,
    lessonsCount: 0,
    imageUrl: '',
    department: 'Engineering',
    featuresJson: '[]',
    modules: [],
    resources: []
  });

  // Features toggles
  featureCertificate = signal<boolean>(true);
  featureDownloadable = signal<boolean>(true);
  featureExercises = signal<boolean>(true);

  // PDF Preview modal state
  previewResource = signal<CourseResourceItem | null>(null);

  // Category options
  readonly categories = ['Web Dev', 'Design', 'Marketing', 'Communication', 'Data Science', 'Cybersecurity'];
  readonly skillLevels = ['Beginner', 'Intermediate', 'Advanced'];

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.courseId.set(Number(idParam));
      this.loadCourse(Number(idParam));
    } else {
      // If no ID provided, pick instructor's first course or redirect
      this.instructorService.getMyCourses().subscribe({
        next: (list) => {
          if (list.length > 0) {
            this.courseId.set(list[0].id);
            this.loadCourse(list[0].id);
          } else {
            this.isLoading.set(false);
          }
        },
        error: () => this.isLoading.set(false)
      });
    }
  }

  loadCourse(id: number): void {
    this.isLoading.set(true);
    this.instructorService.getCourseBuilder(id).subscribe({
      next: (detail) => {
        this.course.set(detail);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load course builder:', err);
        this.isLoading.set(false);
        this.snackBar.open('Error loading course details.', 'Close', { duration: 3000 });
      }
    });
  }

  // ─── Thumbnail Upload ────────────────────────────────────────────────────
  onThumbnailSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.isUploadingThumbnail.set(true);
    this.instructorService.uploadCourseThumbnail(this.courseId(), file).subscribe({
      next: (res) => {
        this.course.update(c => ({ ...c, imageUrl: res.imageUrl }));
        this.isUploadingThumbnail.set(false);
        this.snackBar.open('Thumbnail uploaded successfully!', 'OK', { duration: 3000 });
      },
      error: (err) => {
        this.isUploadingThumbnail.set(false);
        this.snackBar.open('Failed to upload thumbnail.', 'Close', { duration: 3000 });
      }
    });
  }

  // ─── Module Management ───────────────────────────────────────────────────
  addModule(): void {
    const currentCount = this.course().modules.length;
    const newTitle = `Module ${currentCount + 1}: Core Concepts & Principles`;

    this.instructorService.addModule(this.courseId(), { title: newTitle }).subscribe({
      next: (mod) => {
        mod.lessons = [];
        this.course.update(c => ({
          ...c,
          modules: [...c.modules, mod]
        }));
        this.snackBar.open('New module added!', 'OK', { duration: 2500 });
      }
    });
  }

  updateModuleTitle(mod: BuilderModule): void {
    this.instructorService.updateModule(this.courseId(), mod.id, {
      title: mod.title,
      description: mod.description
    }).subscribe();
  }

  deleteModule(modId: number): void {
    if (!confirm('Are you sure you want to remove this module and its lessons?')) return;

    this.instructorService.deleteModule(this.courseId(), modId).subscribe({
      next: () => {
        this.course.update(c => ({
          ...c,
          modules: c.modules.filter(m => m.id !== modId)
        }));
        this.snackBar.open('Module removed.', 'OK', { duration: 2500 });
      }
    });
  }

  moveModule(index: number, direction: 'up' | 'down'): void {
    const mods = [...this.course().modules];
    const targetIndex = direction === 'up' ? index - 1 : index + 1;
    if (targetIndex < 0 || targetIndex >= mods.length) return;

    const temp = mods[index];
    mods[index] = mods[targetIndex];
    mods[targetIndex] = temp;

    mods.forEach((m, idx) => m.orderIndex = idx + 1);
    this.course.update(c => ({ ...c, modules: mods }));

    this.instructorService.reorderModules(this.courseId(), mods.map(m => ({ id: m.id, orderIndex: m.orderIndex }))).subscribe();
  }

  // ─── Lesson Management ───────────────────────────────────────────────────
  addLesson(mod: BuilderModule): void {
    const lessonNum = mod.lessons.length + 1;
    const lessonData = {
      title: `Lesson ${lessonNum}: Practical Implementation`,
      description: 'Step-by-step practical walk-through and evaluation.',
      durationMinutes: 45
    };

    this.instructorService.addLesson(this.courseId(), mod.id, lessonData).subscribe({
      next: (lesson) => {
        mod.lessons.push(lesson);
        this.updateTotalLessonsCount();
        this.snackBar.open('Lesson added to module!', 'OK', { duration: 2500 });
      }
    });
  }

  updateLesson(modId: number, lesson: BuilderLesson): void {
    this.instructorService.updateLesson(this.courseId(), modId, lesson.id, {
      title: lesson.title,
      description: lesson.description,
      durationMinutes: lesson.durationMinutes,
      contentText: lesson.contentText,
      pdfUrl: lesson.pdfUrl,
      pdfFileName: lesson.pdfFileName
    }).subscribe();
  }

  deleteLesson(mod: BuilderModule, lessonId: number): void {
    this.instructorService.deleteLesson(this.courseId(), mod.id, lessonId).subscribe({
      next: () => {
        mod.lessons = mod.lessons.filter(l => l.id !== lessonId);
        this.updateTotalLessonsCount();
        this.snackBar.open('Lesson removed.', 'OK', { duration: 2000 });
      }
    });
  }

  moveLesson(mod: BuilderModule, index: number, direction: 'up' | 'down'): void {
    const targetIndex = direction === 'up' ? index - 1 : index + 1;
    if (targetIndex < 0 || targetIndex >= mod.lessons.length) return;

    const temp = mod.lessons[index];
    mod.lessons[index] = mod.lessons[targetIndex];
    mod.lessons[targetIndex] = temp;

    mod.lessons.forEach((l, idx) => l.orderIndex = idx + 1);

    this.instructorService.reorderLessons(this.courseId(), mod.id, mod.lessons.map(l => ({ id: l.id, orderIndex: l.orderIndex }))).subscribe();
  }

  private updateTotalLessonsCount(): void {
    const total = this.course().modules.reduce((sum, m) => sum + m.lessons.length, 0);
    this.course.update(c => ({ ...c, lessonsCount: total }));
  }

  // ─── PDF / Course Resource Upload ────────────────────────────────────────
  onResourceSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.isUploadingResource.set(true);

    this.instructorService.uploadResource(this.courseId(), file).subscribe({
      next: (res) => {
        this.course.update(c => ({
          ...c,
          resources: [...c.resources, res]
        }));
        this.isUploadingResource.set(false);
        this.snackBar.open(`Resource "${res.title}" uploaded!`, 'OK', { duration: 3000 });
      },
      error: () => {
        this.isUploadingResource.set(false);
        this.snackBar.open('Failed to upload resource document.', 'Close', { duration: 3000 });
      }
    });
  }

  deleteResource(resId: number): void {
    if (!confirm('Are you sure you want to remove this PDF resource?')) return;

    this.instructorService.deleteResource(this.courseId(), resId).subscribe({
      next: () => {
        this.course.update(c => ({
          ...c,
          resources: c.resources.filter(r => r.id !== resId)
        }));
        this.snackBar.open('Resource deleted.', 'OK', { duration: 2500 });
      }
    });
  }

  openPdfPreview(res: CourseResourceItem): void {
    this.previewResource.set(res);
  }

  closePdfPreview(): void {
    this.previewResource.set(null);
  }

  // ─── Save / Publish ──────────────────────────────────────────────────────
  saveDraft(): void {
    this.saveCourse('Draft');
  }

  publishCourse(): void {
    this.saveCourse('Active');
  }

  private saveCourse(status: string): void {
    this.isSaving.set(true);
    const c = this.course();

    this.instructorService.updateCourse(this.courseId(), {
      title: c.title,
      description: c.description,
      about: c.about,
      category: c.category,
      level: c.level,
      status: status,
      durationHours: c.durationHours,
      lessonsCount: c.lessonsCount,
      imageUrl: c.imageUrl,
      department: c.department
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.course.update(curr => ({ ...curr, status }));
        const msg = status === 'Active' ? 'Course published successfully!' : 'Course draft saved successfully!';
        this.snackBar.open(msg, 'OK', { duration: 3500 });
      },
      error: (err) => {
        this.isSaving.set(false);
        console.error('Failed to save course:', err);
        this.snackBar.open('Failed to save course. Check fields.', 'Close', { duration: 4000 });
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/instructor/courses']);
  }
}
