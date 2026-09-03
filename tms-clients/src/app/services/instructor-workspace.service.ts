import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  InstructorDashboard,
  CourseAttendanceResponse,
  CourseAttendanceStats,
  CourseBuilderDetail,
  InstructorCourseSummary,
  AssignmentItem,
  SubmissionItem,
  SubmissionDetail,
  GradebookResponse,
  InstructorProfile,
  BuilderModule,
  BuilderLesson,
  CourseResourceItem
} from '../models/instructor-workspace.model';
import { Schedule } from '../models/schedule.model';

@Injectable({
  providedIn: 'root'
})
export class InstructorWorkspaceService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1';

  // ─── Dashboard ────────────────────────────────────────────────────────────
  getDashboard(): Observable<InstructorDashboard> {
    return this.http.get<InstructorDashboard>(`${this.baseUrl}/dashboard/instructor`);
  }

  replyQuestion(questionId: number, answer: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/dashboard/instructor/questions/${questionId}/reply`, { answer });
  }

  // ─── Master Schedule ──────────────────────────────────────────────────────
  getSchedule(year?: number, month?: number, date?: string): Observable<Schedule[]> {
    let params = new HttpParams();
    if (year) params = params.set('year', year.toString());
    if (month) params = params.set('month', month.toString());
    if (date) params = params.set('date', date);

    return this.http.get<Schedule[]>(`${this.baseUrl}/schedules/instructor-schedule`, { params });
  }

  // ─── Digital Roll Call / Attendance ───────────────────────────────────────
  getCourseAttendance(courseId: number, date?: string): Observable<CourseAttendanceResponse> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<CourseAttendanceResponse>(`${this.baseUrl}/attendance/course/${courseId}`, { params });
  }

  markAttendance(data: {
    studentId: number;
    courseId: number;
    scheduleId?: number;
    sessionDate: string;
    status: number;
    remarks?: string;
  }): Observable<{ message: string; status: string }> {
    return this.http.post<{ message: string; status: string }>(`${this.baseUrl}/attendance/mark`, data);
  }

  bulkAttendance(data: {
    courseId: number;
    scheduleId?: number;
    sessionDate: string;
    items: { studentId: number; status: number; remarks?: string }[];
  }): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/attendance/bulk`, data);
  }

  getAttendanceStats(courseId: number): Observable<CourseAttendanceStats> {
    return this.http.get<CourseAttendanceStats>(`${this.baseUrl}/attendance/stats/course/${courseId}`);
  }

  // ─── Courses & Content Builder ───────────────────────────────────────────
  getMyCourses(): Observable<InstructorCourseSummary[]> {
    return this.http.get<InstructorCourseSummary[]>(`${this.baseUrl}/courses/instructor-my-courses`);
  }

  getCourseBuilder(courseId: number): Observable<CourseBuilderDetail> {
    return this.http.get<CourseBuilderDetail>(`${this.baseUrl}/courses/${courseId}/builder`);
  }

  updateCourse(courseId: number, data: Partial<CourseBuilderDetail>): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/courses/${courseId}`, data);
  }

  uploadCourseThumbnail(courseId: number, file: File): Observable<{ imageUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ imageUrl: string }>(`${this.baseUrl}/courses/${courseId}/thumbnail`, formData);
  }

  addModule(courseId: number, data: { title: string; description?: string }): Observable<BuilderModule> {
    return this.http.post<BuilderModule>(`${this.baseUrl}/courses/${courseId}/modules`, data);
  }

  updateModule(courseId: number, moduleId: number, data: { title: string; description?: string }): Observable<BuilderModule> {
    return this.http.put<BuilderModule>(`${this.baseUrl}/courses/${courseId}/modules/${moduleId}`, data);
  }

  deleteModule(courseId: number, moduleId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/courses/${courseId}/modules/${moduleId}`);
  }

  reorderModules(courseId: number, items: { id: number; orderIndex: number }[]): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/courses/${courseId}/modules/reorder`, { items });
  }

  addLesson(courseId: number, moduleId: number, data: {
    title: string;
    description?: string;
    durationMinutes: number;
    contentText?: string;
    pdfUrl?: string;
    pdfFileName?: string;
  }): Observable<BuilderLesson> {
    return this.http.post<BuilderLesson>(`${this.baseUrl}/courses/${courseId}/modules/${moduleId}/lessons`, data);
  }

  updateLesson(courseId: number, moduleId: number, lessonId: number, data: {
    title: string;
    description?: string;
    durationMinutes: number;
    contentText?: string;
    pdfUrl?: string;
    pdfFileName?: string;
  }): Observable<BuilderLesson> {
    return this.http.put<BuilderLesson>(`${this.baseUrl}/courses/${courseId}/modules/${moduleId}/lessons/${lessonId}`, data);
  }

  deleteLesson(courseId: number, moduleId: number, lessonId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/courses/${courseId}/modules/${moduleId}/lessons/${lessonId}`);
  }

  reorderLessons(courseId: number, moduleId: number, items: { id: number; orderIndex: number }[]): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/courses/${courseId}/modules/${moduleId}/lessons/reorder`, { items });
  }

  uploadResource(courseId: number, file: File, title?: string): Observable<CourseResourceItem> {
    const formData = new FormData();
    formData.append('file', file);
    if (title) formData.append('title', title);
    return this.http.post<CourseResourceItem>(`${this.baseUrl}/courses/${courseId}/resources/upload`, formData);
  }

  deleteResource(courseId: number, resourceId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/courses/${courseId}/resources/${resourceId}`);
  }

  // ─── Assignments ──────────────────────────────────────────────────────────
  getAssignments(courseId: number): Observable<AssignmentItem[]> {
    return this.http.get<AssignmentItem[]>(`${this.baseUrl}/assignments/course/${courseId}`);
  }

  getAssignment(id: number): Observable<AssignmentItem> {
    return this.http.get<AssignmentItem>(`${this.baseUrl}/assignments/${id}`);
  }

  createAssignment(data: {
    courseId: number;
    courseModuleId?: number;
    courseLessonId?: number;
    title: string;
    instructions: string;
    maxPoints: number;
    dueDate: string;
    isPublished: boolean;
  }): Observable<{ id: number; message: string }> {
    return this.http.post<{ id: number; message: string }>(`${this.baseUrl}/assignments`, data);
  }

  updateAssignment(id: number, data: {
    title: string;
    instructions: string;
    maxPoints: number;
    dueDate: string;
    isPublished: boolean;
  }): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/assignments/${id}`, data);
  }

  deleteAssignment(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/assignments/${id}`);
  }

  uploadAssignmentAttachment(id: number, file: File): Observable<{ attachmentUrl: string; attachmentFileName: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ attachmentUrl: string; attachmentFileName: string }>(`${this.baseUrl}/assignments/${id}/attachment`, formData);
  }

  getSubmissions(assignmentId: number): Observable<SubmissionItem[]> {
    return this.http.get<SubmissionItem[]>(`${this.baseUrl}/assignments/${assignmentId}/submissions`);
  }

  getSubmissionDetail(submissionId: number): Observable<SubmissionDetail> {
    return this.http.get<SubmissionDetail>(`${this.baseUrl}/assignments/submissions/${submissionId}`);
  }

  gradeSubmission(submissionId: number, data: {
    scoredPoints: number;
    feedback?: string;
    rubricScoresJson?: string;
  }): Observable<{ message: string; status: string; scoredPoints: number; gradedAt: string }> {
    return this.http.post<{ message: string; status: string; scoredPoints: number; gradedAt: string }>(`${this.baseUrl}/assignments/submissions/${submissionId}/grade`, data);
  }

  // ─── Gradebook Matrix ─────────────────────────────────────────────────────
  getGradebook(courseId: number): Observable<GradebookResponse> {
    return this.http.get<GradebookResponse>(`${this.baseUrl}/grades/gradebook/course/${courseId}`);
  }

  updateGradebookCell(data: {
    courseId: number;
    studentId: number;
    assignmentId: number;
    score: number | null;
    feedback?: string;
  }): Observable<{ message: string; assessmentId: number; score: number | null; status: string }> {
    return this.http.post<{ message: string; assessmentId: number; score: number | null; status: string }>(`${this.baseUrl}/grades/gradebook/cell`, data);
  }

  // ─── Instructor Profile ───────────────────────────────────────────────────
  getProfile(): Observable<InstructorProfile> {
    return this.http.get<InstructorProfile>(`${this.baseUrl}/instructorprofile`);
  }

  updateProfile(data: {
    firstName: string;
    lastName: string;
    phoneNumber?: string;
    department?: string;
    bio?: string;
  }): Observable<{ message: string; firstName: string; lastName: string; phoneNumber?: string; department?: string }> {
    return this.http.put<{ message: string; firstName: string; lastName: string; phoneNumber?: string; department?: string }>(`${this.baseUrl}/instructorprofile`, data);
  }

  uploadAvatar(file: File): Observable<{ profilePictureUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ profilePictureUrl: string }>(`${this.baseUrl}/instructorprofile/avatar`, formData);
  }

  changePassword(data: { currentPassword: string; newPassword: string }): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/instructorprofile/change-password`, data);
  }
}
