import { Injectable, signal, effect } from '@angular/core';

export type AppTheme = 'dark' | 'light';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  readonly currentTheme = signal<AppTheme>(this.getInitialTheme());

  constructor() {
    effect(() => {
      const theme = this.currentTheme();
      localStorage.setItem('tms_app_theme', theme);
      if (theme === 'light') {
        document.documentElement.classList.add('light-theme');
        document.documentElement.classList.remove('dark-theme');
      } else {
        document.documentElement.classList.add('dark-theme');
        document.documentElement.classList.remove('light-theme');
      }
    });
  }

  toggleTheme(): void {
    this.currentTheme.update((t) => (t === 'dark' ? 'light' : 'dark'));
  }

  private getInitialTheme(): AppTheme {
    const saved = localStorage.getItem('tms_app_theme') as AppTheme;
    if (saved === 'light' || saved === 'dark') return saved;
    return 'dark';
  }
}
