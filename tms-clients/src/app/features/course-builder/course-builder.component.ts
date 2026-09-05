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
import { CourseSchedule } from '../../models/course.model';

const BACKEND_TO_FRONTEND_FIELD_MAP: Record<string, string> = {
  maxcapacity: 'maxCapacity',
  capacity: 'maxCapacity',
  'course.maxcapacity': 'maxCapacity',
  'dto.maxcapacity': 'maxCapacity',
  coursename: 'title',
  name: 'title',
  title: 'title',
  'course.title': 'title',
  'dto.title': 'title',
  code: 'code',
  coursecode: 'code',
  'course.code': 'code',
  'dto.code': 'code',
  startdate: 'startDate',
  'dto.startdate': 'startDate',
  enddate: 'endDate',
  'dto.enddate': 'endDate',
  durationhours: 'durationHours',
  duration: 'durationHours',
  'dto.durationhours': 'durationHours',
  category: 'category',
  'dto.category': 'category',
  level: 'level',
  'dto.level': 'level',
  about: 'about',
  description: 'about',
  'dto.about': 'about',
  'dto.description': 'about',
  coursecontent: 'courseContent',
  content: 'courseContent',
  modules: 'courseContent',
  lessons: 'courseContent',
  'dto.modules': 'courseContent',
  schedules: 'schedules',
  'dto.schedules': 'schedules',
  schedule: 'schedules',
  'dto.schedule': 'schedules',
  expectedassignmentcount: 'expectedAssignmentCount',
  'course.expectedassignmentcount': 'expectedAssignmentCount',
  'dto.expectedassignmentcount': 'expectedAssignmentCount',
  expectedmidtermcount: 'expectedMidtermCount',
  'course.expectedmidtermcount': 'expectedMidtermCount',
  'dto.expectedmidtermcount': 'expectedMidtermCount',
  expectedfinalcount: 'expectedFinalCount',
  'course.expectedfinalcount': 'expectedFinalCount',
  'dto.expectedfinalcount': 'expectedFinalCount'
};

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
    startDate: '',
    endDate: '',
    maxCapacity: 30,
    schedules: [
      {
        id: 0,
        dayOfWeek: 1,
        dayName: 'Monday',
        startTime: '09:00',
        endTime: '10:30',
        location: 'Main Campus',
        room: 'Room 101',
        scheduleType: 'In-Person'
      }
    ],
    rejectionReason: '',
    expectedAssignmentCount: 5,
    expectedMidtermCount: 1,
    expectedFinalCount: 1,
    totalAssessments: 7,
    createdAssignmentsCount: 0,
    modules: [],
    resources: []
  });

  // Features toggles
  featureCertificate = signal<boolean>(true);
  featureDownloadable = signal<boolean>(true);
  featureExercises = signal<boolean>(true);

  // Validation tracking
  formSubmitted = signal<boolean>(false);
  touchedFields = signal<Set<string>>(new Set<string>());
  fieldErrors = signal<Map<string, string>>(new Map<string, string>());
  scheduleRowErrors = signal<Map<number, string>>(new Map<number, string>());
  formGeneralError = signal<string | null>(null);

  // Publish success modal state
  publishSuccessData = signal<any | null>(null);

  // PDF Preview modal state
  previewResource = signal<CourseResourceItem | null>(null);

  // Category options
  readonly categories = ['Web Dev', 'Design', 'Marketing', 'Communication', 'Data Science', 'Cybersecurity'];
  readonly skillLevels = ['Beginner', 'Intermediate', 'Advanced'];
  readonly scheduleTypes = ['In-Person', 'Online', 'Hybrid'];
  readonly daysOfWeek = [
    { value: 1, label: 'Monday' },
    { value: 2, label: 'Tuesday' },
    { value: 3, label: 'Wednesday' },
    { value: 4, label: 'Thursday' },
    { value: 5, label: 'Friday' },
    { value: 6, label: 'Saturday' },
    { value: 0, label: 'Sunday' }
  ];

  touchField(name: string): void {
    const updated = new Set(this.touchedFields());
    updated.add(name);
    this.touchedFields.set(updated);
    this.validateField(name);
  }

  isFieldInvalid(name: string): boolean {
    const isTouchedOrSubmitted = this.formSubmitted() || this.touchedFields().has(name);
    return isTouchedOrSubmitted && this.fieldErrors().has(name);
  }

  getFieldError(name: string): string | null {
    if (!this.isFieldInvalid(name)) return null;
    return this.fieldErrors().get(name) || null;
  }

  validateField(name: string, targetStatus?: string): void {
    const c = this.course();
    const status = targetStatus || c.status;
    const errors = new Map(this.fieldErrors());

    if (name === 'title') {
      if (!c.title || !c.title.trim()) {
        errors.set('title', 'Course name is required.');
      } else if (c.title.trim().length < 3) {
        errors.set('title', 'Course name must be at least 3 characters.');
      } else {
        errors.delete('title');
      }
    }

    if (name === 'durationHours') {
      if (status === 'Active') {
        if (c.durationHours === null || c.durationHours === undefined || isNaN(Number(c.durationHours))) {
          errors.set('durationHours', 'Duration is required.');
        } else if (Number(c.durationHours) <= 0) {
          errors.set('durationHours', 'Duration must be greater than 0 hours to publish.');
        } else {
          errors.delete('durationHours');
        }
      } else {
        if (c.durationHours !== null && c.durationHours !== undefined && !isNaN(Number(c.durationHours)) && Number(c.durationHours) < 0) {
          errors.set('durationHours', 'Duration cannot be negative.');
        } else {
          errors.delete('durationHours');
        }
      }
    }

    if (name === 'maxCapacity') {
      if (c.maxCapacity !== null && c.maxCapacity !== undefined && !isNaN(Number(c.maxCapacity))) {
        if (Number(c.maxCapacity) < 0) {
          errors.set('maxCapacity', 'Capacity cannot be negative.');
        } else if (Number(c.maxCapacity) === 0) {
          errors.set('maxCapacity', 'Capacity must be greater than 0.');
        } else {
          errors.delete('maxCapacity');
        }
      } else {
        errors.set('maxCapacity', 'Capacity must be greater than 0.');
      }
    }

    if (name === 'startDate' || name === 'endDate') {
      if (c.startDate && c.endDate) {
        const start = new Date(c.startDate).getTime();
        const end = new Date(c.endDate).getTime();
        if (start > end) {
          errors.set('startDate', 'Start date cannot be after the end date.');
          errors.set('endDate', 'End date must be on or after the start date.');
        } else {
          errors.delete('startDate');
          errors.delete('endDate');
        }
      } else {
        errors.delete('startDate');
        errors.delete('endDate');
      }
    }

    if (name === 'category') {
      if ((status === 'Active' || status === 'Under Review') && (!c.category || !c.category.trim())) {
        errors.set('category', 'Course category is required to submit for review.');
      } else {
        errors.delete('category');
      }
    }

    if (name === 'level') {
      if ((status === 'Active' || status === 'Under Review') && (!c.level || !c.level.trim())) {
        errors.set('level', 'Skill level is required to submit for review.');
      } else {
        errors.delete('level');
      }
    }

    if (name === 'about') {
      if ((status === 'Active' || status === 'Under Review') && (!c.about || !c.about.trim()) && (!c.description || !c.description.trim())) {
        errors.set('about', 'Course overview or description is required to submit for review.');
      } else {
        errors.delete('about');
      }
    }

    if (name === 'courseContent') {
      const hasContent = c.modules && c.modules.length > 0 && c.modules.some(m => m.lessons && m.lessons.length > 0);
      if ((status === 'Active' || status === 'Under Review') && !hasContent) {
        errors.set('courseContent', 'Course content is required. Add at least one module with lessons before submitting for review.');
      } else {
        errors.delete('courseContent');
      }
    }

    if (name === 'expectedAssignmentCount') {
      const val = Number(c.expectedAssignmentCount);
      if (isNaN(val) || val < 0) {
        errors.set('expectedAssignmentCount', 'Expected assignments cannot be negative.');
      } else if (c.createdAssignmentsCount && val < c.createdAssignmentsCount) {
        errors.set('expectedAssignmentCount', `Cannot reduce below ${c.createdAssignmentsCount} (already created assignments).`);
      } else {
        errors.delete('expectedAssignmentCount');
      }
    }

    if (name === 'expectedMidtermCount') {
      const val = Number(c.expectedMidtermCount);
      if (isNaN(val) || val < 0) {
        errors.set('expectedMidtermCount', 'Expected midterms cannot be negative.');
      } else {
        errors.delete('expectedMidtermCount');
      }
    }

    if (name === 'expectedFinalCount') {
      const val = Number(c.expectedFinalCount);
      if (isNaN(val) || val < 0) {
        errors.set('expectedFinalCount', 'Expected finals cannot be negative.');
      } else {
        errors.delete('expectedFinalCount');
      }
    }

    if (name === 'schedules') {
      this.validateSchedules(status);
      return;
    }

    this.fieldErrors.set(errors);
  }

  // ─── Schedule Management & Validation ────────────────────────────────────
  addScheduleRow(): void {
    const current = this.course().schedules || [];
    const nextDay = ((current.length + 1) % 6) || 1;
    const foundDay = this.daysOfWeek.find(d => d.value === nextDay);
    const newSlot: CourseSchedule = {
      id: 0,
      dayOfWeek: nextDay,
      dayName: foundDay?.label || 'Monday',
      startTime: '09:00',
      endTime: '10:30',
      location: 'Main Campus',
      room: 'Room 101',
      scheduleType: 'In-Person'
    };
    this.course.update(c => ({
      ...c,
      schedules: [...(c.schedules || []), newSlot]
    }));
    this.validateSchedules();
  }

  removeScheduleRow(index: number): void {
    const current = this.course().schedules || [];
    if (current.length <= 1) {
      this.snackBar.open('At least one schedule slot is required for the course.', 'OK', { duration: 2500 });
      return;
    }
    const updated = current.filter((_, idx) => idx !== index);
    this.course.update(c => ({
      ...c,
      schedules: updated
    }));
    this.validateSchedules();
  }

  onScheduleDayChange(slot: CourseSchedule): void {
    slot.dayOfWeek = Number(slot.dayOfWeek);
    const found = this.daysOfWeek.find(d => d.value === slot.dayOfWeek);
    if (found) {
      slot.dayName = found.label;
    }
    this.validateSchedules();
  }

  validateSchedules(targetStatus?: string): boolean {
    const c = this.course();
    const st = targetStatus || c.status;
    const rowErrors = new Map<number, string>();
    const errors = new Map(this.fieldErrors());
    const schedules = c.schedules || [];

    if (st === 'Active' || st === 'Under Review' || st === 'Published') {
      if (schedules.length === 0) {
        errors.set('schedules', 'At least one class schedule slot is required before submitting for review.');
        this.scheduleRowErrors.set(rowErrors);
        this.fieldErrors.set(errors);
        return false;
      }
    }

    // Validate each individual schedule slot
    for (let i = 0; i < schedules.length; i++) {
      const s = schedules[i];
      if (s.dayOfWeek === null || s.dayOfWeek === undefined || isNaN(Number(s.dayOfWeek)) || Number(s.dayOfWeek) < 0 || Number(s.dayOfWeek) > 6) {
        rowErrors.set(i, 'Valid day of week is required.');
        continue;
      }
      if (!s.startTime || !s.startTime.trim()) {
        rowErrors.set(i, 'Start time is required.');
        continue;
      }
      if (!s.endTime || !s.endTime.trim()) {
        rowErrors.set(i, 'End time is required.');
        continue;
      }

      const sParts = s.startTime.split(':');
      const eParts = s.endTime.split(':');
      const sMin = parseInt(sParts[0], 10) * 60 + parseInt(sParts[1], 10);
      const eMin = parseInt(eParts[0], 10) * 60 + parseInt(eParts[1], 10);

      if (isNaN(sMin) || isNaN(eMin)) {
        rowErrors.set(i, 'Valid start and end time values are required.');
        continue;
      }

      if (eMin <= sMin) {
        rowErrors.set(i, `End time (${s.endTime}) must be strictly after start time (${s.startTime}).`);
        continue;
      }

      // Check for overlapping slots on the same day
      for (let j = 0; j < i; j++) {
        const prev = schedules[j];
        if (Number(prev.dayOfWeek) === Number(s.dayOfWeek)) {
          const pParts = prev.startTime.split(':');
          const peParts = prev.endTime.split(':');
          const pStart = parseInt(pParts[0], 10) * 60 + parseInt(pParts[1], 10);
          const pEnd = parseInt(peParts[0], 10) * 60 + parseInt(peParts[1], 10);

          if (sMin < pEnd && eMin > pStart) {
            const dayLabel = this.daysOfWeek.find(d => d.value === Number(s.dayOfWeek))?.label || 'day';
            rowErrors.set(i, `Overlaps with slot #${j + 1} on ${dayLabel} (${prev.startTime} – ${prev.endTime}).`);
            break;
          }
        }
      }
    }

    if (rowErrors.size > 0) {
      errors.set('schedules', 'Please fix time interval and overlap errors in the schedule table below.');
    } else {
      errors.delete('schedules');
    }

    this.scheduleRowErrors.set(rowErrors);
    this.fieldErrors.set(errors);
    return rowErrors.size === 0 && (!errors.has('schedules'));
  }

  validateAll(status: string): boolean {
    const c = this.course();
    const errors = new Map<string, string>();

    // Title is always required (min 3 chars)
    if (!c.title || !c.title.trim()) {
      errors.set('title', 'Course name is required.');
    } else if (c.title.trim().length < 3) {
      errors.set('title', 'Course name must be at least 3 characters.');
    }

    // Duration cannot be negative
    if (c.durationHours !== null && c.durationHours !== undefined && !isNaN(Number(c.durationHours))) {
      if (Number(c.durationHours) < 0) {
        errors.set('durationHours', 'Duration cannot be negative.');
      }
    }

    // Capacity must be > 0 and not negative
    if (c.maxCapacity !== null && c.maxCapacity !== undefined && !isNaN(Number(c.maxCapacity))) {
      if (Number(c.maxCapacity) < 0) {
        errors.set('maxCapacity', 'Capacity cannot be negative.');
      } else if (Number(c.maxCapacity) === 0) {
        errors.set('maxCapacity', 'Capacity must be greater than 0.');
      }
    } else {
      errors.set('maxCapacity', 'Capacity must be greater than 0.');
    }

    // Date range validation
    if (c.startDate && c.endDate) {
      const start = new Date(c.startDate).getTime();
      const end = new Date(c.endDate).getTime();
      if (start > end) {
        errors.set('startDate', 'Start date cannot be after the end date.');
        errors.set('endDate', 'End date must be on or after the start date.');
      }
    }

    // When submitting for review or publishing, full requirements apply
    if (status === 'Active' || status === 'Under Review') {
      if (c.durationHours === null || c.durationHours === undefined || isNaN(Number(c.durationHours)) || Number(c.durationHours) <= 0) {
        errors.set('durationHours', 'Duration must be greater than 0 hours to submit for review.');
      }
      if (!c.category || !c.category.trim()) {
        errors.set('category', 'Course category is required to submit for review.');
      }
      if (!c.level || !c.level.trim()) {
        errors.set('level', 'Skill level is required to submit for review.');
      }
      if ((!c.about || !c.about.trim()) && (!c.description || !c.description.trim())) {
        errors.set('about', 'Course overview or description is required to submit for review.');
      }

      // Mandatory Course Content requirement: must have at least 1 module and at least 1 lesson
      const hasContent = c.modules && c.modules.length > 0 && c.modules.some(m => m.lessons && m.lessons.length > 0);
      if (!hasContent) {
        errors.set('courseContent', 'Course content is required. Add at least one module with lessons before submitting for review.');
      }
    }

    // Assessment Structure Validation
    const assignCount = Number(c.expectedAssignmentCount ?? 5);
    const midCount = Number(c.expectedMidtermCount ?? 1);
    const finCount = Number(c.expectedFinalCount ?? 1);

    if (isNaN(assignCount) || assignCount < 0) {
      errors.set('expectedAssignmentCount', 'Expected assignments cannot be negative.');
    } else if (c.createdAssignmentsCount && assignCount < c.createdAssignmentsCount) {
      errors.set('expectedAssignmentCount', `Cannot reduce below ${c.createdAssignmentsCount} (already created assignments).`);
    }

    if (isNaN(midCount) || midCount < 0) {
      errors.set('expectedMidtermCount', 'Expected midterms cannot be negative.');
    }

    if (isNaN(finCount) || finCount < 0) {
      errors.set('expectedFinalCount', 'Expected finals cannot be negative.');
    }

    if ((status === 'Active' || status === 'Under Review') && (assignCount + midCount + finCount) < 1) {
      errors.set('assessmentStructure', 'At least one assessment (assignment, midterm, or final) is required to submit for review.');
    }

    this.fieldErrors.set(errors);

    // Validate schedules
    const schedulesValid = this.validateSchedules(status);

    return errors.size === 0 && schedulesValid;
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam && Number(idParam) > 0) {
      this.courseId.set(Number(idParam));
      this.loadCourse(Number(idParam));
    } else if (idParam === '0' || idParam === 'new') {
      this.courseId.set(0);
      this.course.set({
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
        schedules: [
          {
            id: 0,
            dayOfWeek: 1,
            dayName: 'Monday',
            startTime: '09:00',
            endTime: '10:30',
            location: 'Main Campus',
            room: 'Room 101',
            scheduleType: 'In-Person'
          }
        ],
        rejectionReason: '',
        expectedAssignmentCount: 5,
        expectedMidtermCount: 1,
        expectedFinalCount: 1,
        totalAssessments: 7,
        createdAssignmentsCount: 0,
        modules: [],
        resources: []
      });
      this.isLoading.set(false);
    } else {
      // If no ID provided, pick instructor's first course or redirect
      this.instructorService.getMyCourses().subscribe({
        next: (list) => {
          if (list.length > 0) {
            this.courseId.set(list[0].id);
            this.loadCourse(list[0].id);
          } else {
            this.courseId.set(0);
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
        if (!detail.schedules || detail.schedules.length === 0) {
          detail.schedules = [
            {
              id: 0,
              dayOfWeek: 1,
              dayName: 'Monday',
              startTime: '09:00',
              endTime: '10:30',
              location: 'Main Campus',
              room: 'Room 101',
              scheduleType: 'In-Person'
            }
          ];
        }
        detail.expectedAssignmentCount = detail.expectedAssignmentCount ?? 5;
        detail.expectedMidtermCount = detail.expectedMidtermCount ?? 1;
        detail.expectedFinalCount = detail.expectedFinalCount ?? 1;
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

    if (this.courseId() === 0) {
      // Local addition for draft/new courses
      const tempId = -Math.floor(Date.now() % 10000000);
      const newMod: BuilderModule = {
        id: tempId,
        title: newTitle,
        description: '',
        orderIndex: currentCount + 1,
        lessons: []
      };
      this.course.update(c => ({
        ...c,
        modules: [...c.modules, newMod]
      }));
      this.snackBar.open('New module added!', 'OK', { duration: 2500 });
      return;
    }

    this.instructorService.addModule(this.courseId(), { title: newTitle }).subscribe({
      next: (mod) => {
        mod.lessons = [];
        this.course.update(c => ({
          ...c,
          modules: [...c.modules, mod]
        }));
        this.snackBar.open('New module added!', 'OK', { duration: 2500 });
      },
      error: (err) => {
        console.error('Failed to add module:', err);
        this.snackBar.open('Failed to add module.', 'Close', { duration: 3000 });
      }
    });
  }

  updateModuleTitle(mod: BuilderModule): void {
    if (this.courseId() > 0 && mod.id > 0) {
      this.instructorService.updateModule(this.courseId(), mod.id, {
        title: mod.title,
        description: mod.description
      }).subscribe();
    }
  }

  deleteModule(modId: number): void {
    if (!confirm('Are you sure you want to remove this module and its lessons?')) return;

    if (this.courseId() === 0 || modId <= 0) {
      this.course.update(c => ({
        ...c,
        modules: c.modules.filter(m => m.id !== modId)
      }));
      this.updateTotalLessonsCount();
      this.snackBar.open('Module removed.', 'OK', { duration: 2500 });
      return;
    }

    this.instructorService.deleteModule(this.courseId(), modId).subscribe({
      next: () => {
        this.course.update(c => ({
          ...c,
          modules: c.modules.filter(m => m.id !== modId)
        }));
        this.updateTotalLessonsCount();
        this.snackBar.open('Module removed.', 'OK', { duration: 2500 });
      },
      error: (err) => {
        console.error('Failed to delete module:', err);
        this.snackBar.open('Failed to remove module.', 'Close', { duration: 3000 });
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

    if (this.courseId() > 0 && mods.every(m => m.id > 0)) {
      this.instructorService.reorderModules(this.courseId(), mods.map(m => ({ id: m.id, orderIndex: m.orderIndex }))).subscribe();
    }
  }

  // ─── Lesson Management ───────────────────────────────────────────────────
  addLesson(mod: BuilderModule): void {
    const lessonNum = (mod.lessons?.length ?? 0) + 1;
    const lessonData = {
      title: `Lesson ${lessonNum}: Practical Implementation`,
      description: 'Step-by-step practical walk-through and evaluation.',
      durationMinutes: 45
    };

    if (this.courseId() === 0 || mod.id <= 0) {
      // In-memory addition for new course or unsaved module
      const tempLessonId = -Math.floor(Date.now() % 10000000);
      const newLesson: BuilderLesson = {
        id: tempLessonId,
        title: lessonData.title,
        description: lessonData.description,
        durationMinutes: lessonData.durationMinutes,
        orderIndex: lessonNum
      };
      if (!mod.lessons) mod.lessons = [];
      mod.lessons.push(newLesson);
      this.updateTotalLessonsCount();
      this.snackBar.open('Lesson added to module!', 'OK', { duration: 2500 });
      return;
    }

    this.instructorService.addLesson(this.courseId(), mod.id, lessonData).subscribe({
      next: (lesson) => {
        if (!mod.lessons) mod.lessons = [];
        mod.lessons.push(lesson);
        this.updateTotalLessonsCount();
        this.snackBar.open('Lesson added to module!', 'OK', { duration: 2500 });
      },
      error: (err) => {
        console.error('Failed to add lesson:', err);
        this.snackBar.open('Failed to add lesson.', 'Close', { duration: 3000 });
      }
    });
  }

  updateLesson(modId: number, lesson: BuilderLesson): void {
    if (this.courseId() > 0 && modId > 0 && lesson.id > 0) {
      this.instructorService.updateLesson(this.courseId(), modId, lesson.id, {
        title: lesson.title,
        description: lesson.description,
        durationMinutes: lesson.durationMinutes,
        contentText: lesson.contentText,
        pdfUrl: lesson.pdfUrl,
        pdfFileName: lesson.pdfFileName
      }).subscribe();
    }
  }

  deleteLesson(mod: BuilderModule, lessonId: number): void {
    if (this.courseId() === 0 || mod.id <= 0 || lessonId <= 0) {
      mod.lessons = mod.lessons.filter(l => l.id !== lessonId);
      this.updateTotalLessonsCount();
      this.snackBar.open('Lesson removed.', 'OK', { duration: 2000 });
      return;
    }

    this.instructorService.deleteLesson(this.courseId(), mod.id, lessonId).subscribe({
      next: () => {
        mod.lessons = mod.lessons.filter(l => l.id !== lessonId);
        this.updateTotalLessonsCount();
        this.snackBar.open('Lesson removed.', 'OK', { duration: 2000 });
      },
      error: (err) => {
        console.error('Failed to remove lesson:', err);
        this.snackBar.open('Failed to remove lesson.', 'Close', { duration: 3000 });
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

    if (this.courseId() > 0 && mod.id > 0 && mod.lessons.every(l => l.id > 0)) {
      this.instructorService.reorderLessons(this.courseId(), mod.id, mod.lessons.map(l => ({ id: l.id, orderIndex: l.orderIndex }))).subscribe();
    }
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

  submitForReview(): void {
    this.saveCourse('Under Review');
  }

  publishCourse(): void {
    this.submitForReview();
  }

  private saveCourse(status: string): void {
    this.formSubmitted.set(true);
    this.formGeneralError.set(null);

    const isValid = this.validateAll(status);
    if (!isValid) {
      const firstInvalid = ['title', 'code', 'durationHours', 'maxCapacity', 'startDate', 'endDate', 'expectedAssignmentCount', 'expectedMidtermCount', 'expectedFinalCount', 'assessmentStructure', 'schedules', 'category', 'level', 'about', 'courseContent']
        .find(f => this.fieldErrors().has(f));
      if (firstInvalid) {
        this.scrollToField(firstInvalid);
      }
      this.snackBar.open('Course validation failed. Please correct the highlighted fields.', 'Close', { duration: 4000 });
      return;
    }
    this.isSaving.set(true);
    const c = this.course();
    const instructorId = c.instructorId || this.authService.currentUser()?.instructorId;

    const payload = {
      code: c.code,
      title: c.title,
      description: c.description || c.about,
      about: c.about || c.description,
      category: c.category,
      level: c.level,
      status: status,
      durationHours: c.durationHours,
      lessonsCount: c.lessonsCount,
      imageUrl: c.imageUrl,
      department: c.department,
      instructorId: instructorId,
      startDate: c.startDate ? new Date(c.startDate).toISOString() : undefined,
      endDate: c.endDate ? new Date(c.endDate).toISOString() : undefined,
      maxCapacity: c.maxCapacity,
      expectedAssignmentCount: Number(c.expectedAssignmentCount ?? 5),
      expectedMidtermCount: Number(c.expectedMidtermCount ?? 1),
      expectedFinalCount: Number(c.expectedFinalCount ?? 1),
      schedules: (c.schedules || []).map(s => ({
        dayOfWeek: Number(s.dayOfWeek),
        startTime: s.startTime,
        endTime: s.endTime,
        location: s.location || 'Main Campus',
        room: s.room || 'Room 101',
        scheduleType: s.scheduleType || 'In-Person'
      })),
      modules: c.modules.map((m, mIdx) => ({
        id: m.id,
        title: m.title,
        description: m.description,
        orderIndex: mIdx + 1,
        lessons: (m.lessons || []).map((l, lIdx) => ({
          id: l.id,
          title: l.title,
          description: l.description,
          durationMinutes: l.durationMinutes || 30,
          contentText: l.contentText,
          pdfUrl: l.pdfUrl,
          pdfFileName: l.pdfFileName,
          orderIndex: lIdx + 1
        }))
      }))
    };

    const save$ = this.courseId() > 0
      ? this.instructorService.updateCourse(this.courseId(), payload)
      : this.instructorService.createCourse(payload);

    save$.subscribe({
      next: (res: any) => {
        this.isSaving.set(false);
        const newId = res?.id || res?.courseId || this.courseId();
        const returnedCode = res?.code || c.code;
        const returnedTitle = res?.title || c.title;
        const returnedLevel = res?.level || c.level;
        const returnedStartDate = res?.startDate || c.startDate;
        const returnedEndDate = res?.endDate || c.endDate;

        if (newId > 0) {
          this.courseId.set(newId);
          this.course.update(curr => ({
            ...curr,
            id: newId,
            code: returnedCode,
            status: res?.status || status
          }));
        } else {
          this.course.update(curr => ({ ...curr, status: res?.status || status }));
        }

        this.formSubmitted.set(false);
        this.fieldErrors.set(new Map());
        this.scheduleRowErrors.set(new Map());
        this.formGeneralError.set(null);

        if (status === 'Under Review' || status === 'Active') {
          // Show review submission success dialog with real saved course details
          this.publishSuccessData.set({
            courseId: newId,
            title: returnedTitle,
            code: returnedCode,
            level: returnedLevel,
            startDate: returnedStartDate,
            endDate: returnedEndDate,
            status: 'Under Review',
            message: 'Course submitted successfully. Your course is now under review.'
          });
        } else {
          const msg = res?.message || 'Course draft saved successfully!';
          this.snackBar.open(msg, 'OK', { duration: 3500 });
          if (this.courseId() === 0 && newId > 0) {
            this.router.navigate(['/instructor/courses', newId, 'builder']);
          }
        }
      },
      error: (err: any) => {
        this.isSaving.set(false);
        console.error('Failed to save course:', err);

        let errBody = err?.error;
        if (typeof errBody === 'string') {
          try {
            errBody = JSON.parse(errBody);
          } catch {
            // retain string
          }
        }

        // A. Structured validation response: HTTP 400 with 'errors' map or dictionary
        const errorsMap = errBody?.errors || (err.status === 400 && errBody && typeof errBody === 'object' && !errBody.message && !errBody.title && Object.keys(errBody).length > 0 ? errBody : null);

        if (err.status === 400 && errorsMap && typeof errorsMap === 'object' && Object.keys(errorsMap).length > 0) {
          const serverErrors = new Map<string, string>();
          const serverRowErrors = new Map<number, string>();
          const touched = new Set(this.touchedFields());

          for (const [key, rawMsgs] of Object.entries(errorsMap)) {
            const cleanKey = key.replace(/^\$\./, '').trim().toLowerCase();
            const mappedField = BACKEND_TO_FRONTEND_FIELD_MAP[cleanKey] || (key.charAt(0).toLowerCase() + key.slice(1));
            const msgList = Array.isArray(rawMsgs) ? rawMsgs : [String(rawMsgs)];
            if (msgList.length > 0) {
              if (cleanKey.startsWith('schedules') || cleanKey.startsWith('dto.schedules')) {
                serverErrors.set('schedules', msgList[0]);
                touched.add('schedules');
                const match = cleanKey.match(/\[(\d+)\]/);
                if (match) {
                  const rIdx = parseInt(match[1], 10);
                  serverRowErrors.set(rIdx, msgList[0]);
                }
              } else {
                serverErrors.set(mappedField, msgList[0]);
                touched.add(mappedField);
              }
            }
          }

          this.formSubmitted.set(true);
          this.touchedFields.set(touched);
          this.fieldErrors.set(serverErrors);
          if (serverRowErrors.size > 0) {
            this.scheduleRowErrors.set(serverRowErrors);
          }
          const validationMsg = 'Course validation failed. Please correct the highlighted fields.';
          this.formGeneralError.set(validationMsg);

          const firstKey = Array.from(serverErrors.keys())[0];
          if (firstKey) this.scrollToField(firstKey);
          this.snackBar.open(validationMsg, 'Close', { duration: 5000 });
          return;
        }

        // B. Business-rule errors (400 with specific message, or 409 Conflict)
        if (err.status === 409 || (err.status === 400 && errBody?.message)) {
          const msg = errBody?.message || 'The course action could not be completed due to a business rule conflict.';
          this.formGeneralError.set(msg);
          this.snackBar.open(msg, 'Close', { duration: 5000 });
          return;
        }

        // C. Authorization errors (401 / 403)
        if (err.status === 401) {
          const msg = 'Your session has expired. Please log in again.';
          this.formGeneralError.set(msg);
          this.snackBar.open(msg, 'Close', { duration: 5000 });
          return;
        }
        if (err.status === 403) {
          const msg = errBody?.message || 'You do not have permission to modify this course.';
          this.formGeneralError.set(msg);
          this.snackBar.open(msg, 'Close', { duration: 5000 });
          return;
        }

        // D. Not found (404)
        if (err.status === 404) {
          const msg = errBody?.message || `Course #${this.courseId()} was not found.`;
          this.formGeneralError.set(msg);
          this.snackBar.open(msg, 'Close', { duration: 5000 });
          return;
        }

        // E. Conflict (409)
        if (err.status === 409) {
          const msg = errBody?.message || 'A data conflict occurred while modifying this course.';
          this.formGeneralError.set(msg);
          this.snackBar.open(msg, 'Close', { duration: 5000 });
          return;
        }

        // F. Unexpected server error (500 or unrecognized error)
        const unexpectedMsg = err.status >= 500
          ? 'Unexpected server error while saving course. Please try again.'
          : (errBody?.message || 'Unexpected server error while saving course. Please try again.');
        this.formGeneralError.set(unexpectedMsg);
        this.snackBar.open(unexpectedMsg, 'Close', { duration: 5000 });
      }
    });
  }

  acknowledgePublishSuccess(): void {
    this.publishSuccessData.set(null);
    this.router.navigate(['/instructor/courses']);
  }

  viewCourseDetails(courseId: number): void {
    this.publishSuccessData.set(null);
    this.router.navigate(['/instructor/courses', courseId]);
  }

  private scrollToField(fieldName: string): void {
    setTimeout(() => {
      const el = document.getElementById(`field-${fieldName}`);
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
        el.focus();
      }
    }, 60);
  }

  cancel(): void {
    this.router.navigate(['/instructor/courses']);
  }
}
