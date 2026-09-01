import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from './auth.service';

export interface LiveNotification {
  id: string;
  type: 'enrollment' | 'course' | 'capacity' | 'transcript' | 'info';
  title: string;
  message: string;
  timestamp: Date;
  data?: any;
}

@Injectable({
  providedIn: 'root'
})
export class LiveSyncService {
  private authService = inject(AuthService);
  private hubConnection: signalR.HubConnection | null = null;

  // Reactive state
  readonly isConnected = signal<boolean>(false);
  readonly notifications = signal<LiveNotification[]>([]);
  readonly unreadCount = signal<number>(0);

  // Event streams
  readonly enrollmentUpdated$ = new Subject<{ studentId: number; courseId: number; status: string }>();
  readonly courseCapacityChanged$ = new Subject<{ courseId: number; enrolledCount: number; maxCapacity: number }>();
  readonly transcriptCompleted$ = new Subject<{ jobId: string; studentId: number; downloadUrl: string }>();

  constructor() {
    // If authenticated, automatically connect
    if (this.authService.isAuthenticated()) {
      this.connect();
    }
  }

  connect(): void {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/tms', {
        accessTokenFactory: () => this.authService.getAccessToken() || '',
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.registerEventHandlers();

    this.hubConnection
      .start()
      .then(() => {
        this.isConnected.set(true);
      })
      .catch((err) => {
        console.warn('SignalR Hub Connection error:', err);
        this.isConnected.set(false);
      });

    this.hubConnection.onreconnected(() => {
      this.isConnected.set(true);
    });

    this.hubConnection.onreconnecting(() => {
      this.isConnected.set(false);
    });

    this.hubConnection.onclose(() => {
      this.isConnected.set(false);
    });
  }

  disconnect(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
      this.isConnected.set(false);
    }
  }

  private registerEventHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('ReceiveEnrollmentNotification', (data: any) => {
      this.enrollmentUpdated$.next(data);
      this.addNotification({
        id: crypto.randomUUID(),
        type: 'enrollment',
        title: 'Enrollment Update',
        message: data.message || `Enrollment updated for course #${data.courseId}`,
        timestamp: new Date(),
        data
      });
    });

    this.hubConnection.on('ReceiveCapacityAlert', (data: any) => {
      this.courseCapacityChanged$.next(data);
      this.addNotification({
        id: crypto.randomUUID(),
        type: 'capacity',
        title: 'Course Capacity Alert',
        message: `Course #${data.courseId} is near or at capacity (${data.enrolledCount}/${data.maxCapacity})`,
        timestamp: new Date(),
        data
      });
    });

    this.hubConnection.on('ReceiveTranscriptReady', (data: any) => {
      this.transcriptCompleted$.next(data);
      this.addNotification({
        id: crypto.randomUUID(),
        type: 'transcript',
        title: 'Transcript Ready',
        message: 'Your official transcript generation is complete and ready for download.',
        timestamp: new Date(),
        data
      });
    });

    this.hubConnection.on('ReceiveMessage', (user: string, message: string) => {
      this.addNotification({
        id: crypto.randomUUID(),
        type: 'info',
        title: user || 'System Notification',
        message,
        timestamp: new Date()
      });
    });
  }

  private addNotification(notification: LiveNotification): void {
    this.notifications.update((list) => [notification, ...list.slice(0, 19)]);
    this.unreadCount.update((count) => count + 1);
  }

  clearNotifications(): void {
    this.notifications.set([]);
    this.unreadCount.set(0);
  }

  markAllAsRead(): void {
    this.unreadCount.set(0);
  }
}
