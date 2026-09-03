import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from './auth.service';
import { NotificationService } from './notification.service';

export interface LiveNotification {
  id: string;
  type: 'enrollment' | 'course' | 'capacity' | 'transcript' | 'info';
  title: string;
  message: string;
  timestamp: Date;
  data?: unknown;
}

@Injectable({
  providedIn: 'root'
})
export class LiveSyncService {
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);
  private hubConnection: signalR.HubConnection | null = null;

  // Reactive state
  readonly isConnected = signal<boolean>(false);

  // Event streams
  readonly enrollmentUpdated$ = new Subject<{ studentId: number; courseId: number; status?: string; grade?: number; letterGrade?: string }>();
  readonly gradePublished$ = new Subject<{ studentId: number; courseId: number; grade: number; letterGrade: string }>();
  readonly scheduleUpdated$ = new Subject<void>();
  readonly courseCapacityChanged$ = new Subject<{ courseId: number; enrolledCount: number; maxCapacity: number }>();
  readonly transcriptCompleted$ = new Subject<{ jobId: string; studentId: number; downloadUrl: string }>();

  constructor() {
    // If authenticated, automatically connect and load notifications
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
        this.joinUserGroups();
        // Load real notifications from DB on connect
        this.loadInitialNotifications();
      })
      .catch((err) => {
        console.warn('SignalR Hub Connection error:', err);
        this.isConnected.set(false);
      });

    this.hubConnection.onreconnected(() => {
      this.isConnected.set(true);
      this.joinUserGroups();
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

  private joinUserGroups(): void {
    if (!this.hubConnection) return;

    const user = this.authService.currentUser();
    if (!user) return;

    // Join user's private notification group
    this.hubConnection.invoke('JoinUserGroup', user.id).catch(console.warn);

    // If student, also join student group
    if (user.studentId) {
      this.hubConnection.invoke('JoinStudentGroup', String(user.studentId)).catch(console.warn);
    }

    // If admin, join admin broadcast group
    if (user.role === 'Admin') {
      this.hubConnection.invoke('JoinAdminGroup').catch(console.warn);
    }
  }

  private loadInitialNotifications(): void {
    // Load real notifications from API on connect
    this.notificationService.loadNotifications(1, 20).subscribe({ error: console.warn });
    this.notificationService.loadUnreadCount().subscribe({ error: console.warn });
  }

  private registerEventHandlers(): void {
    if (!this.hubConnection) return;

    // Handle enrollment notifications (pushed after approve/reject)
    this.hubConnection.on('ReceiveNotification', (title: string, message: string) => {
      this.notificationService.addRealtimeNotification(title, message, 'Enrollment');
    });

    // Enrollment updates (e.g., capacity change events)
    this.hubConnection.on('ReceiveEnrollmentNotification', (data: { studentId: number; courseId: number; status: string }) => {
      this.enrollmentUpdated$.next(data);
    });

    this.hubConnection.on('ReceiveCapacityAlert', (data: { courseId: number; enrolledCount: number; maxCapacity: number }) => {
      this.courseCapacityChanged$.next(data);
      this.notificationService.addRealtimeNotification(
        'Course Capacity Alert',
        `Course is near capacity (${data.enrolledCount}/${data.maxCapacity} students)`,
        'Course'
      );
    });

    this.hubConnection.on('ReceiveTranscriptStatus', (requestId: string, status: string, downloadUrl: string | null) => {
      if (status === 'Completed' && downloadUrl) {
        this.transcriptCompleted$.next({ jobId: requestId, studentId: 0, downloadUrl });
        this.notificationService.addRealtimeNotification(
          'Transcript Ready',
          'Your transcript has been generated and is ready for download.',
          'Transcript'
        );
      }
    });

    this.hubConnection.on('ReceiveGradePublished', (data: { studentId: number; courseId: number; grade: number; letterGrade: string }) => {
      this.gradePublished$.next(data);
      this.notificationService.addRealtimeNotification(
        'Grade Published',
        `A new grade (${data.letterGrade}) has been published.`,
        'Grade'
      );
    });

    this.hubConnection.on('ReceiveScheduleUpdate', () => {
      this.scheduleUpdated$.next();
    });

    // Generic system message
    this.hubConnection.on('ReceiveMessage', (user: string, message: string) => {
      this.notificationService.addRealtimeNotification(user || 'System', message, 'System');
    });
  }
}
