import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

export interface StatMetric {
  label: string;
  value: string | number;
  icon: string;
  change?: string;
  trend?: 'up' | 'down' | 'neutral';
  colorGradient?: string;
}

@Component({
  selector: 'app-analytics-chart',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  templateUrl: './analytics-chart.component.html',
  styleUrls: ['./analytics-chart.component.scss']
})
export class AnalyticsChartComponent {
  @Input() metrics: StatMetric[] = [];
}
