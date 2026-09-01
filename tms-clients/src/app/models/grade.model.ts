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

export interface StudentGradeRecord {
  enrollmentId: number;
  courseTitle: string;
  courseCode: string;
  letterGrade: string;
  numericScore: number;
  gradePoints: number;
  credits: number;
}

export interface StudentAcademicRecord {
  studentId: number;
  studentName: string;
  gpa: number;
  totalCredits: number;
  records: StudentGradeRecord[];
}
