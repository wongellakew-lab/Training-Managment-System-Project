import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Course } from '../../models/course.model';

@Component({
  selector: 'app-course-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule, MatProgressBarModule],
  templateUrl: './course-card.component.html',
  styleUrls: ['./course-card.component.scss']
})
export class CourseCardComponent {
  @Input({ required: true }) course!: Course;
  @Input() actionLabel: string = 'Enroll';
  @Input() buttonClass: string = 'btn-primary text-xs py-1.5 px-3';
  @Output() actionClicked = new EventEmitter<Course>();

  get capacityPercentage(): number {
    if (!this.course.maxCapacity || this.course.maxCapacity === 0) return 0;
    return Math.min(100, Math.round((this.course.enrolledCount / this.course.maxCapacity) * 100));
  }

  get isFull(): boolean {
    return this.course.enrolledCount >= this.course.maxCapacity;
  }
}
