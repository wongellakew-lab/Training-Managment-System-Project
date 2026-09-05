import { CourseSchedule } from './course.model';

export interface StudentEnrolledCourse {
  courseId: number;
  courseCode: string;
  title: string;
  description: string;
  level: string;
  department: string;
  category: string;
  imageUrl?: string;
  credits: number;
  durationWeeks: number;
  startDate?: string;
  endDate?: string;

  // Schedule info
  schedules: CourseSchedule[];
  daysDisplay: string;
  timeDisplay: string;
  location: string;
  room: string;

  // Instructor info
  instructorId?: number;
  instructorName: string;
  instructorAvatar?: string;
  instructorDepartment?: string;

  // Enrollment & Progress info
  enrollmentId: number;
  enrolledAt: string;
  enrollmentStatus: string;
  academicStatus: 'Active' | 'Upcoming' | 'Completed' | 'Withdrawn' | string;
  progressPercentage: number;
  completedAssessments?: number;
  totalAssessments?: number;
  totalLessons: number;
  completedLessons: number;
  totalModules: number;
  completedModules: number;
  grade?: number;
}

export interface StudentCoursesStatistics {
  totalEnrolledCourses: number;
  activeCourses: number;
  completedCourses: number;
  upcomingCourses: number;
  totalCredits: number;
  overallProgressPercentage: number;
  currentSemesterCourses: number;
  upcomingClassesCount: number;
}
