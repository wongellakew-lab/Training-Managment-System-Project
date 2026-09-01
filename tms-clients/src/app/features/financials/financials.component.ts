import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { LayoutComponent } from '../../ui/layout/layout.component';

@Component({
  selector: 'app-financials-page',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, LayoutComponent],
  template: `
    <app-layout pageTitle="Financials">
      <div class="financials-page-content">
        <!-- 3 Big Metric Cards -->
        <div class="financials-stats-row">
          <div class="eduva-card fin-stat-card">
            <div class="fin-icon-box bg-navy"><mat-icon>account_balance_wallet</mat-icon></div>
            <div class="fin-stat-info">
              <span class="fin-lbl">Total Revenue</span>
              <span class="fin-val">$294,500</span>
              <span class="fin-sub text-green">↑ +14.8% vs last period</span>
            </div>
          </div>

          <div class="eduva-card fin-stat-card">
            <div class="fin-icon-box bg-cyan"><mat-icon>payments</mat-icon></div>
            <div class="fin-stat-info">
              <span class="fin-lbl">Total Paid Out</span>
              <span class="fin-val">$946,210</span>
              <span class="fin-sub text-muted">13,180 Student Transactions</span>
            </div>
          </div>

          <div class="eduva-card fin-stat-card">
            <div class="fin-icon-box bg-blue"><mat-icon>pending</mat-icon></div>
            <div class="fin-stat-info">
              <span class="fin-lbl">Pending Receivables</span>
              <span class="fin-val">$268,340</span>
              <span class="fin-sub text-orange">3,900 Invoices in progress</span>
            </div>
          </div>
        </div>

        <!-- Recent Transactions Table -->
        <div class="eduva-card tx-card">
          <div class="card-header-flex">
            <h3 class="card-title">Recent Invoices &amp; Transactions</h3>
            <button class="export-btn">
              <span>Export CSV</span>
              <mat-icon>download</mat-icon>
            </button>
          </div>

          <div class="table-responsive">
            <table class="tx-table">
              <thead>
                <tr>
                  <th>Invoice ID</th>
                  <th>Student</th>
                  <th>Course / Service</th>
                  <th>Amount</th>
                  <th>Payment Method</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                @for (tx of transactions(); track tx.id) {
                  <tr>
                    <td><strong>{{ tx.id }}</strong></td>
                    <td>{{ tx.student }}</td>
                    <td>{{ tx.course }}</td>
                    <td><strong class="text-primary">{{ tx.amount }}</strong></td>
                    <td>{{ tx.method }}</td>
                    <td><span class="pill-badge" [ngClass]="tx.status.toLowerCase()">&bull; {{ tx.status }}</span></td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
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
    }
  `]
})
export class FinancialsComponent implements OnInit {
  readonly transactions = signal([
    { id: 'INV-2030-984', student: 'Grasiella Lopez', course: 'UX/UI Design Fundamentals', amount: '$42.00', method: 'Credit Card (Stripe)', status: 'Paid' },
    { id: 'INV-2030-983', student: 'Hendrick Chow', course: 'Python for Beginners', amount: '$55.00', method: 'Bank Transfer', status: 'Pending' },
    { id: 'INV-2030-982', student: 'Jenice Miller', course: 'Public Speaking & Presentation Skills', amount: '$27.00', method: 'PayPal', status: 'Paid' },
    { id: 'INV-2030-981', student: 'Nigel Bruce', course: 'Digital Marketing Mastery', amount: '$49.00', method: 'Credit Card', status: 'Unpaid' },
    { id: 'INV-2030-980', student: 'Amber Lunge', course: 'Graphic Design Fundamentals', amount: '$32.00', method: 'Apple Pay', status: 'Paid' },
    { id: 'INV-2030-979', student: 'L. Mitch Saul', course: 'Full Stack Web Development Bootcamp', amount: '$79.00', method: 'Bank Transfer', status: 'Pending' }
  ]);

  ngOnInit(): void {}
}
