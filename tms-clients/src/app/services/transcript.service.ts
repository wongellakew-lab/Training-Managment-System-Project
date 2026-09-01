import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TranscriptJobResponse, TranscriptStatusResponse } from '../models/transcript.model';

@Injectable({
  providedIn: 'root'
})
export class TranscriptService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/transcripts';

  requestTranscript(studentId: number): Observable<TranscriptJobResponse> {
    return this.http.post<TranscriptJobResponse>(this.baseUrl, { studentId });
  }

  getJobStatus(jobId: string): Observable<TranscriptStatusResponse> {
    return this.http.get<TranscriptStatusResponse>(`${this.baseUrl}/status/${jobId}`);
  }
}
