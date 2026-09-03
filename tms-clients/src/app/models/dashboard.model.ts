import { Course } from './course.model';
import { Enrollment } from './enrollment.model';
import { Schedule } from './schedule.model';

export interface MonthlyRevenue {
  month: string;
  revenue: number;
  profit: number;
  expense: number;
}

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

export interface ActivityHeatmapCell {
  day: string;
  timeSlot: string;
  level: number;
  value: number;
}

export interface CountryDemographic {
  country: string;
  flag: string;
  studentsCount: number;
  percentage: number;
}

export interface SystemActivity {
  id: string;
  title: string;
  description: string;
  type: string;
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

  totalRevenue: number;
  revenueHistory: MonthlyRevenue[];

  totalEnrollmentValue: number;
  enrollmentGrowthPercentage: number;
  enrollmentTrends: MonthlyEnrollmentTrend[];

  topCoursesDistribution: TopCourseStat[];
  learningActivityHeatmap: ActivityHeatmapCell[];
  studentDemographics: CountryDemographic[];

  newCourses: Course[];
  upcomingSchedules: Schedule[];
  recentActivities: SystemActivity[];

  pendingApprovalList?: Enrollment[];
  recentActivity?: SystemActivity[];
}
