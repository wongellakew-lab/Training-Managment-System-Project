export interface AssignGradeRequest {
  enrollmentId: number;
  letterGrade: string;
  numericScore: number;
}

export interface BulkGradeItem {
  enrollmentId: number;
  letterGrade: string;
  numericScore: number;
}

export interface BulkAssignGradesRequest {
  grades: BulkGradeItem[];
}

export interface AssessmentGradeItem {
  id: number;
  title: string;
  maxScore: number;
  scoredPoints?: number;
  dueDate: string;
  status: string;
  feedback?: string;
  gradedAt?: string;
  rubricScoresJson?: string;
}

export interface StudentGradeRecord {
  enrollmentId: number;
  courseId?: number;
  courseTitle: string;
  courseCode: string;
  letterGrade?: string;
  numericScore?: number;
  gradePoints?: number;
  credits: number;
  status?: string;
  assessments?: AssessmentGradeItem[];
}

export interface StudentAcademicRecord {
  studentId: number;
  studentName: string;
  gpa: number;
  totalCredits: number;
  completedCoursesCount?: number;
  grades: StudentGradeRecord[];
}
