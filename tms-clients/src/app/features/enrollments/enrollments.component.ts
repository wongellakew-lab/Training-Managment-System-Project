import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { EnrollmentService } from '../../services/enrollment.service';
import { Enrollment } from '../../models/enrollment.model';
import { PaymentStatusBreakdown } from '../../models/dashboard.model';

@Component({
  selector: 'app-enrollments-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    LayoutComponent
  ],
  templateUrl: './enrollments.component.html',
  styleUrls: ['./enrollments.component.scss']
})
export class EnrollmentsComponent implements OnInit {
  private enrollmentService = inject(EnrollmentService);

  readonly enrollments = signal<Enrollment[]>([]);
  readonly breakdown = signal<PaymentStatusBreakdown | null>(null);
  readonly isLoading = signal(false);

  // Filters
  searchTerm = '';
  selectedStatus = 'Any Status';
  selectedTime = 'All Time';
  currentPage = 1;
  pageSize = 8;
  totalCount = 18540;

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);
    this.enrollmentService.getEnrollmentsPaged({
      page: this.currentPage,
      pageSize: this.pageSize,
      search: this.searchTerm,
      status: this.selectedStatus === 'Any Status' ? undefined : this.selectedStatus
    }).subscribe({
      next: (res) => {
        this.enrollments.set(res.items);
        if (res.totalCount) this.totalCount = res.totalCount;
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });

    this.enrollmentService.getPaymentBreakdown().subscribe({
      next: (data) => this.breakdown.set(data)
    });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadData();
  }

  onStatusChange(status: string): void {
    this.selectedStatus = status;
    this.currentPage = 1;
    this.loadData();
  }

  setPage(p: number): void {
    if (p < 1) return;
    this.currentPage = p;
    this.loadData();
  }

  getCategoryClass(category?: string): string {
    const cat = (category || 'design').toLowerCase();
    if (cat.includes('web')) return 'category-webdev';
    if (cat.includes('market')) return 'category-marketing';
    if (cat.includes('commun')) return 'category-communication';
    if (cat.includes('busi')) return 'category-business';
    if (cat.includes('well')) return 'category-wellness';
    return 'category-design';
  }

  getStatusClass(status: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'active' || s === 'enrolled' || s === 'completed' || s === 'approved') return 'enrolled';
    if (s === 'pending' || s === 'applied') return 'applied';
    if (s === 'waitlisted') return 'waitlisted';
    return 'applied';
  }

  getStatusLabel(status: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'active' || s === 'enrolled' || s === 'approved') return 'Enrolled';
    if (s === 'pending' || s === 'applied') return 'Applied';
    if (s === 'waitlisted') return 'Waitlisted';
    return status;
  }

  getPaymentClass(payment?: string): string {
    const p = (payment || 'paid').toLowerCase();
    if (p === 'paid') return 'paid';
    if (p === 'pending') return 'pending';
    return 'unpaid';
  }
}
