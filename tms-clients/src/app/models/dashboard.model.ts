import { Course } from './course.model';
import { Enrollment } from './enrollment.model';
import { Schedule } from './schedule.model';

export interface MonthlyEnrollmentTrend {
  month: string;
  count: number;
  isPeak: boolean;
}

export interface TopCourseStat {
  name: string;
  studentsCount: number;
  percentage: number;
  color: string;
}

export interface SystemActivity {
  id: string;
  title: string;
  description: string;
  type: string;
  actorName?: string;
  actorRole?: string;
  userAvatar?: string;
  timestamp: string;
}

export interface AdminDashboardMetrics {
  totalStudents: number;
  totalInstructors: number;
  totalCourses: number;
  activeEnrollments: number;
  pendingEnrollments: number;
  waitlistedEnrollments: number;
  totalCertificates: number;

  enrollmentGrowthPercentage: number;
  enrollmentTrends: MonthlyEnrollmentTrend[];

  topCoursesDistribution: TopCourseStat[];

  newCourses: Course[];
  upcomingSchedules: Schedule[];
  recentActivities: SystemActivity[];

  pendingApprovalList?: Enrollment[];
  recentActivity?: SystemActivity[];
}

