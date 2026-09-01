import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HeaderComponent } from '../header/header.component';
import { FooterComponent } from '../footer/footer.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, SidebarComponent, HeaderComponent, FooterComponent],
  template: `
    <div class="eduva-app-container">
      <app-sidebar></app-sidebar>
      <div class="eduva-main-viewport">
        <app-header [pageTitle]="pageTitle" [showGreeting]="showGreeting"></app-header>
        <main class="eduva-content-area">
          <ng-content></ng-content>
        </main>
        <app-footer></app-footer>
      </div>
    </div>
  `,
  styles: [`
    .eduva-app-container {
      display: flex;
      min-height: 100vh;
      background-color: var(--bg-app);
    }
    .eduva-main-viewport {
      flex: 1;
      display: flex;
      flex-direction: column;
      min-width: 0;
      overflow-x: hidden;
    }
    .eduva-content-area {
      flex: 1;
      padding: 0 32px;
    }
  `]
})
export class LayoutComponent {
  @Input() pageTitle: string = 'Dashboard';
  @Input() showGreeting: boolean = false;
}
