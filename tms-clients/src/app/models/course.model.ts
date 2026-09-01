export interface Course {
  id: number;
  title: string;
  code: string;
  description: string;
  credits: number;
  maxCapacity: number;
  enrolledCount: number;
  department: string;
  instructorId?: number;
  instructorName?: string;
  isActive: boolean;
  startDate?: string;
  endDate?: string;
}

export interface CreateCourseDto {
  title: string;
  code: string;
  description: string;
  credits: number;
  maxCapacity: number;
  department: string;
  instructorId?: number;
}

export interface UpdateCourseDto {
  title: string;
  description: string;
  credits: number;
  maxCapacity: number;
  department: string;
  instructorId?: number;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface CourseFilterParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  department?: string;
  fields?: string;
  instructorId?: number;
}
