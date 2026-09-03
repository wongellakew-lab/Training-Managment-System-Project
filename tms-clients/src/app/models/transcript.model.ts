export type JobProcessingStatus = 'Queued' | 'Processing' | 'Completed' | 'Failed';

export interface TranscriptJobResponse {
  jobId: string;
  status: JobProcessingStatus;
  estimatedWaitSeconds: number;
  submittedAt: string;
}

export interface TranscriptStatusResponse {
  jobId: string;
  studentId: number;
  status: JobProcessingStatus;
  downloadUrl?: string;
  generatedAt?: string;
  errorMessage?: string;
}

export interface CertificateResponse {
  id: number;
  certificateCode: string;
  studentId: number;
  studentName: string;
  courseId: number;
  courseTitle: string;
  issuedAt: string;
  isValid: boolean;
  verificationUrl?: string;
}

export interface IssueCertificateRequest {
  studentId: number;
  courseId: number;
}

export interface TranscriptDto {
  institutionName: string;
  documentNumber: string;
  generatedAt: string;
  student: TranscriptStudentDto;
  summary: TranscriptSummaryDto;
  courseRecords: TranscriptCourseRecordDto[];
}

export interface TranscriptStudentDto {
  studentId: number;
  fullName: string;
  registrationNumber: string;
  email: string;
  phoneNumber?: string;
  department: string;
  enrollmentDate: string;
}

export interface TranscriptSummaryDto {
  totalCoursesCompleted: number;
  totalCoursesAttempted: number;
  totalCreditsEarned: number;
  cumulativeGpa: number;
  academicStanding: string;
}

export interface TranscriptCourseRecordDto {
  courseId: number;
  courseCode: string;
  courseTitle: string;
  category: string;
  credits: number;
  durationHours: number;
  term: string;
  finalScore?: number;
  gradeLetter: string;
  gradePoint: number;
  status: string;
  completionDate: string;
}
