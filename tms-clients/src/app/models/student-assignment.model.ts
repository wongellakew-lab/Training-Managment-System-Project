export interface StudentAssignmentItem {
  id: number;
  submissionId?: number;
  courseId: number;
  courseTitle: string;
  courseCode: string;
  courseModuleId?: number;
  moduleTitle?: string;
  courseLessonId?: number;
  lessonTitle?: string;
  title: string;
  instructions?: string;
  maxPoints: number;
  dueDate: string;
  attachmentUrl?: string;
  attachmentFileName?: string;
  status: 'Pending' | 'Submitted' | 'Graded';
  submittedAt?: string;
  scoredPoints?: number;
  feedback?: string;
  gradedAt?: string;
  submissionFileUrl?: string;
  submissionFileName?: string;
  submissionFileType?: string;
  submissionFileSize?: number;
  submissionText?: string;
  isPastDue: boolean;
  canSubmit: boolean;
}

export interface AssignmentSubmissionResponse {
  message: string;
  submissionId: number;
  assignmentId: number;
  status: string;
  submittedAt: string;
  fileName?: string;
  fileUrl?: string;
  submissionText?: string;
}
