import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { FinancialService, FinancialOverview, FinancialTransaction } from '../../services/financial.service';

@Component({
  selector: 'app-financials-page',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule, LayoutComponent],
  template: `
    <app-layout pageTitle="Financials">
      <div class="financials-page-content">
        <!-- 3 Big Metric Cards -->
        <div class="financials-stats-row">
          <div class="eduva-card fin-stat-card">
            <div class="fin-icon-box bg-navy"><mat-icon>account_balance_wallet</mat-icon></div>
            <div class="fin-stat-info">
              <span class="fin-lbl">Total Revenue</span>
              <span class="fin-val">\${{ (overview()?.totalRevenue ?? 0) | number:'1.2-2' }}</span>
              <span class="fin-sub text-green">Settled Course Enrollments</span>
            </div>
          </div>

          <div class="eduva-card fin-stat-card">
            <div class="fin-icon-box bg-cyan"><mat-icon>payments</mat-icon></div>
            <div class="fin-stat-info">
              <span class="fin-lbl">Total Paid Out</span>
              <span class="fin-val">\${{ (overview()?.totalPaidOut ?? 0) | number:'1.2-2' }}</span>
              <span class="fin-sub text-muted">{{ overview()?.totalTransactionsCount ?? 0 }} Student Transactions</span>
            </div>
          </div>

          <div class="eduva-card fin-stat-card">
            <div class="fin-icon-box bg-blue"><mat-icon>pending</mat-icon></div>
            <div class="fin-stat-info">
              <span class="fin-lbl">Pending Receivables</span>
              <span class="fin-val">\${{ (overview()?.pendingReceivables ?? 0) | number:'1.2-2' }}</span>
              <span class="fin-sub text-orange">Awaiting Payment Approval</span>
            </div>
          </div>
        </div>

        <!-- Recent Transactions Table -->
        <div class="eduva-card tx-card">
          <div class="card-header-flex">
            <h3 class="card-title">Recent Invoices &amp; Transactions</h3>
            <button class="export-btn" (click)="exportCsv()">
              <span>Export CSV</span>
              <mat-icon>download</mat-icon>
            </button>
          </div>

          @if (isLoading()) {
            <div class="loading-box">
              <mat-spinner diameter="36"></mat-spinner>
              <span>Loading ledger transactions...</span>
            </div>
          } @else if (!overview()?.recentTransactions || overview()!.recentTransactions.length === 0) {
            <div class="empty-box">
              <mat-icon>receipt_long</mat-icon>
              <h4>No Financial Transactions Yet</h4>
              <p>Course enrollments and tuition invoices will be recorded here.</p>
            </div>
          } @else {
            <div class="table-responsive">
              <table class="tx-table">
                <thead>
                  <tr>
                    <th>Invoice ID</th>
                    <th>Student</th>
                    <th>Course / Service</th>
                    <th>Amount</th>
                    <th>Payment Method</th>
                    <th>Date</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  @for (tx of overview()!.recentTransactions; track tx.id) {
                    <tr>
                      <td><strong>{{ tx.id }}</strong></td>
                      <td>{{ tx.studentName }}</td>
                      <td>{{ tx.courseTitle }}</td>
                      <td><strong class="text-primary">\${{ tx.amount | number:'1.2-2' }}</strong></td>
                      <td>{{ tx.paymentMethod }}</td>
                      <td class="font-mono text-muted">{{ tx.date | date:'mediumDate' }}</td>
                      <td><span class="pill-badge" [ngClass]="tx.status.toLowerCase()">&bull; {{ tx.status }}</span></td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </div>
      </div>
    </app-layout>
  `,
  styles: [`
    .financials-page-content {
      display: flex;
      flex-direction: column;
      gap: 24px;
      padding-bottom: 24px;
    }
    .financials-stats-row {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 20px;
      @media (max-width: 900px) { grid-template-columns: 1fr; }
    }
    .fin-stat-card {
      display: flex;
      align-items: center;
      gap: 18px;
      padding: 20px;
      .fin-icon-box {
        width: 50px;
        height: 50px;
        border-radius: var(--radius-md);
        display: flex;
        align-items: center;
        justify-content: center;
        color: #fff;
        &.bg-navy { background: #172b4d; }
        &.bg-cyan { background: #0284c7; }
        &.bg-blue { background: #06b6d4; }
        mat-icon { font-size: 26px; width: 26px; height: 26px; }
      }
      .fin-stat-info {
        display: flex;
        flex-direction: column;
        gap: 2px;
        .fin-lbl { font-size: 0.75rem; color: var(--text-muted); font-weight: 500; }
        .fin-val { font-size: 1.625rem; font-weight: 800; color: var(--text-primary); }
        .fin-sub { font-size: 0.6875rem; font-weight: 600; &.text-green { color: #059669; } &.text-orange { color: #d97706; } }
      }
    }
    .tx-card {
      padding: 20px;
      .card-header-flex {
        display: flex;
        align-items: center;
        justify-content: space-between;
      }
      .card-title { font-size: 1.125rem; font-weight: 700; color: var(--text-primary); margin: 0; }
      .export-btn {
        display: flex; align-items: center; gap: 6px; background: #ffffff; border: 1px solid var(--border-subtle);
        border-radius: var(--radius-full); padding: 6px 14px; font-size: 0.75rem; font-weight: 600; cursor: pointer;
        mat-icon { font-size: 16px; width: 16px; height: 16px; }
      }
      .tx-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.8125rem;
        margin-top: 16px;
        thead tr {
          border-bottom: 1px solid var(--border-subtle);
          th { text-align: left; padding: 12px 16px; color: var(--text-muted); font-weight: 600; }
        }
        tbody tr {
          border-bottom: 1px solid #f1f5f9;
          td { padding: 14px 16px; }
        }
      }
      .pill-badge {
        display: inline-block;
        padding: 3px 8px;
        border-radius: var(--radius-full);
        font-size: 0.6875rem;
        font-weight: 600;
        &.paid { background: #ecfdf5; color: #059669; }
        &.pending { background: #fffbeb; color: #d97706; }
        &.unpaid { background: #fef2f2; color: #dc2626; }
      }
    }
    .loading-box, .empty-box {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px 24px;
      gap: 12px;
      text-align: center;
      color: var(--text-muted);
      mat-icon { font-size: 40px; width: 40px; height: 40px; color: var(--text-muted); }
      h4 { margin: 0; color: var(--text-primary); font-size: 1.125rem; font-weight: 700; }
      p { margin: 0; font-size: 0.8125rem; }
    }
  `]
})
export class FinancialsComponent implements OnInit {
  private financialService = inject(FinancialService);

  readonly overview = signal<FinancialOverview | null>(null);
  readonly isLoading = signal(false);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);
    this.financialService.getOverview().subscribe({
      next: (data) => {
        this.overview.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  exportCsv(): void {
    const list = this.overview()?.recentTransactions;
    if (!list || list.length === 0) return;

    let csvContent = "data:text/csv;charset=utf-8,Invoice ID,Student,Course,Amount,Payment Method,Status,Date\n";
    list.forEach(tx => {
      csvContent += `"${tx.id}","${tx.studentName}","${tx.courseTitle}",${tx.amount},"${tx.paymentMethod}","${tx.status}","${tx.date}"\n`;
    });

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", `tms_invoices_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}
