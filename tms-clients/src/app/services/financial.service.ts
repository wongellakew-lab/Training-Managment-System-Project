import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FinancialTransaction {
  id: string;
  studentName: string;
  courseTitle: string;
  amount: number;
  paymentMethod: string;
  status: string;
  date: string;
}

export interface FinancialOverview {
  totalRevenue: number;
  totalPaidOut: number;
  pendingReceivables: number;
  totalTransactionsCount: number;
  revenueGrowthPercentage: number;
  recentTransactions: FinancialTransaction[];
}

@Injectable({
  providedIn: 'root'
})
export class FinancialService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/financials';

  getOverview(): Observable<FinancialOverview> {
    return this.http.get<FinancialOverview>(`${this.baseUrl}/overview`);
  }
}
