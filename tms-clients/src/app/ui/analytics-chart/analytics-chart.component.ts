import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

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
  imports: [CommonModule],
  template: `
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
      @for (metric of metrics; track metric.label) {
        <div class="glass-card p-5 relative overflow-hidden">
          <div class="flex items-center justify-between mb-3">
            <span class="text-xs font-semibold uppercase tracking-wider text-slate-400">{{ metric.label }}</span>
            <div class="w-9 h-9 rounded-lg flex items-center justify-center" [ngStyle]="{ 'background': metric.colorGradient || 'rgba(99, 102, 241, 0.15)' }">
              <span class="material-icons text-lg text-indigo-300">{{ metric.icon }}</span>
            </div>
          </div>
          <div class="text-2xl font-black text-white tracking-tight">{{ metric.value }}</div>
          @if (metric.change) {
            <div class="mt-2 text-[11px] flex items-center gap-1" [ngClass]="metric.trend === 'up' ? 'text-emerald-400' : 'text-slate-400'">
              <span class="material-icons text-xs">{{ metric.trend === 'up' ? 'trending_up' : 'info' }}</span>
              <span>{{ metric.change }}</span>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class AnalyticsChartComponent {
  @Input() metrics: StatMetric[] = [];
}
