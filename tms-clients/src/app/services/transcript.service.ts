import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TranscriptDto, TranscriptJobResponse, TranscriptStatusResponse } from '../models/transcript.model';

@Injectable({
  providedIn: 'root'
})
export class TranscriptService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/transcripts';

  getMyTranscript(): Observable<TranscriptDto> {
    return this.http.get<TranscriptDto>(`${this.baseUrl}/my-transcript`);
  }

  getStudentTranscript(studentId: number): Observable<TranscriptDto> {
    return this.http.get<TranscriptDto>(`${this.baseUrl}/student/${studentId}`);
  }

  requestTranscript(studentId?: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/request`, { studentId });
  }

  getDownloadUrl(fileName: string): string {
    return `${this.baseUrl}/download/${fileName}`;
  }

  getPreviewUrl(): string {
    return `${this.baseUrl}/preview`;
  }

  getJobStatus(jobId: string): Observable<TranscriptStatusResponse> {
    return this.http.get<TranscriptStatusResponse>(`${this.baseUrl}/status/${jobId}`);
  }
}
