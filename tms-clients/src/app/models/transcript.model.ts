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
}

export interface IssueCertificateRequest {
  studentId: number;
  courseId: number;
}
