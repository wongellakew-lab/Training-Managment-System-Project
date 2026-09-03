import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CertificateResponse, IssueCertificateRequest } from '../models/transcript.model';

@Injectable({
  providedIn: 'root'
})
export class CertificateService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/certificates';

  issueCertificate(request: IssueCertificateRequest): Observable<CertificateResponse> {
    return this.http.post<CertificateResponse>(this.baseUrl, request);
  }

  verifyCertificate(code: string): Observable<CertificateResponse> {
    return this.http.get<CertificateResponse>(`${this.baseUrl}/verify/${encodeURIComponent(code)}`);
  }

  getStudentCertificates(studentId: number): Observable<CertificateResponse[]> {
    return this.http.get<CertificateResponse[]>(`${this.baseUrl}/student/${studentId}`);
  }

  getMyCertificates(): Observable<CertificateResponse[]> {
    return this.http.get<CertificateResponse[]>(`${this.baseUrl}/my-certificates`);
  }

  getDownloadUrl(id: number): string {
    return `${this.baseUrl}/${id}/download`;
  }
}
