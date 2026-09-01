import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { PagedResult } from '../models/course.model';

export interface TmsNotification {
  id: number;
  userId: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
  relatedEntityId?: number;
  relatedEntityType?: string;
}

export interface UnreadCountResponse {
  count: number;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private http = inject(HttpClient);

  readonly notifications = signal<TmsNotification[]>([]);
  readonly unreadCount = signal<number>(0);

  loadNotifications(page: number = 1, pageSize: number = 20): Observable<PagedResult<TmsNotification>> {
    return this.http.get<PagedResult<TmsNotification>>(`/api/v1/notifications?page=${page}&pageSize=${pageSize}`).pipe(
      tap((res) => {
        this.notifications.set(res.items);
      })
    );
  }

  loadUnreadCount(): Observable<UnreadCountResponse> {
    return this.http.get<UnreadCountResponse>('/api/v1/notifications/unread-count').pipe(
      tap((res) => {
        this.unreadCount.set(res.count);
      })
    );
  }

  markAsRead(id: number): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`/api/v1/notifications/${id}/read`, {}).pipe(
      tap(() => {
        this.notifications.update((list) =>
          list.map((n) => (n.id === id ? { ...n, isRead: true } : n))
        );
        this.unreadCount.update((c) => Math.max(0, c - 1));
      })
    );
  }

  markAllAsRead(): Observable<{ message: string }> {
    return this.http.put<{ message: string }>('/api/v1/notifications/mark-all-read', {}).pipe(
      tap(() => {
        this.notifications.update((list) => list.map((n) => ({ ...n, isRead: true })));
        this.unreadCount.set(0);
      })
    );
  }

  addRealtimeNotification(title: string, message: string, type: string = 'System'): void {
    const item: TmsNotification = {
      id: Date.now(),
      userId: '',
      title,
      message,
      type,
      isRead: false,
      createdAt: new Date().toISOString()
    };
    this.notifications.update((prev) => [item, ...prev]);
    this.unreadCount.update((c) => c + 1);
  }
}
