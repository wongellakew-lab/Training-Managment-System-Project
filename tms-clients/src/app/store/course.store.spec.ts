import { TestBed } from '@angular/core/testing';
import { CourseStore } from './course.store';
import { CourseService } from '../services/course.service';
import { of } from 'rxjs';
import { Course, PagedResult } from '../models/course.model';
import { describe, it, expect, beforeEach, vi } from 'vitest';

describe('CourseStore', () => {
  let store: any;
  let courseServiceSpy: { getCourses: any; getCachedCourses: any };

  const mockCourses: Course[] = [
    {
      id: 1,
      code: 'CS101',
      title: 'Intro to CS',
      description: 'Introductory course',
      department: 'Computer Science',
      category: 'Web Dev',
      level: 'Beginner',
      status: 'Active',
      durationHours: 40,
      lessonsCount: 20,
      rating: 4.8,
      reviewsCount: 150,
      imageUrl: '',
      about: '',
      featuresJson: '[]',
      resourcesJson: '[]',
      enrolledStudentsCount: 15,
      credits: 3,
      maxCapacity: 30,
      enrolledCount: 15,
      isActive: true
    },
    {
      id: 2,
      code: 'MATH201',
      title: 'Linear Algebra',
      description: 'Matrix theory',
      department: 'Mathematics',
      category: 'Design',
      level: 'Intermediate',
      status: 'Active',
      durationHours: 30,
      lessonsCount: 15,
      rating: 4.7,
      reviewsCount: 120,
      imageUrl: '',
      about: '',
      featuresJson: '[]',
      resourcesJson: '[]',
      enrolledStudentsCount: 25,
      credits: 4,
      maxCapacity: 25,
      enrolledCount: 25,
      isActive: true
    }
  ];

  const mockPagedResult: PagedResult<Course> = {
    items: mockCourses,
    pageNumber: 1,
    pageSize: 10,
    totalCount: 2,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false
  };

  beforeEach(() => {
    courseServiceSpy = {
      getCourses: vi.fn().mockReturnValue(of(mockPagedResult)),
      getCachedCourses: vi.fn().mockReturnValue(of(mockPagedResult))
    };

    TestBed.configureTestingModule({
      providers: [
        CourseStore,
        { provide: CourseService, useValue: courseServiceSpy }
      ]
    });

    store = TestBed.inject(CourseStore);
  });

  it('should initialize with default state', () => {
    expect(store.courses()).toEqual([]);
    expect(store.isLoading()).toBe(false);
    expect(store.hasCourses()).toBe(false);
  });

  it('should load courses and update computed properties', async () => {
    await store.loadCourses();

    expect(store.courses().length).toBe(2);
    expect(store.hasCourses()).toBe(true);
    expect(store.activeCoursesCount()).toBe(2);
    expect(store.availableCourses().length).toBe(1); // Only CS101 has open capacity
  });

  it('should update course capacity in real-time', async () => {
    await store.loadCourses();
    store.updateCapacity(1, 30, 30); // CS101 is now full

    expect(store.courses()[0].enrolledCount).toBe(30);
    expect(store.availableCourses().length).toBe(0); // None available
  });
});
