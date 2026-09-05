import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { StudentAssignmentItem, AssignmentSubmissionResponse } from '../models/student-assignment.model';

@Injectable({
  providedIn: 'root'
})
export class StudentAssignmentService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/assignments';

  getMyAssignments(): Observable<StudentAssignmentItem[]> {
    return this.http.get<StudentAssignmentItem[]>(`${this.baseUrl}/my-assignments`);
  }

  getAssignmentDetail(id: number): Observable<StudentAssignmentItem> {
    return this.http.get<StudentAssignmentItem>(`${this.baseUrl}/${id}/student-view`);
  }

  submitAssignment(id: number, file?: File, submissionText?: string): Observable<AssignmentSubmissionResponse> {
    const formData = new FormData();
    if (file) {
      formData.append('file', file, file.name);
    }
    if (submissionText) {
      formData.append('submissionText', submissionText);
    }
    return this.http.post<AssignmentSubmissionResponse>(`${this.baseUrl}/${id}/submit`, formData);
  }

  getDownloadUrl(relativeUrl: string): string {
    if (!relativeUrl) return '#';
    if (relativeUrl.startsWith('http://') || relativeUrl.startsWith('https://')) {
      return relativeUrl;
    }
    return relativeUrl.startsWith('/') ? relativeUrl : `/${relativeUrl}`;
  }
}
