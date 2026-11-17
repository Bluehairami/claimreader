import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Claim, ClaimStatus, ReviewRequest } from '../models/claim';

const API = 'https://localhost:7159/api/claims';

@Injectable({ providedIn: 'root' })
export class ClaimService {
  private http = inject(HttpClient);

  list(status?: ClaimStatus): Observable<Claim[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<Claim[]>(API, { params });
  }

  get(id: number): Observable<Claim> {
    return this.http.get<Claim>(`${API}/${id}`);
  }

  upload(file: File): Observable<Claim> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<Claim>(API, form);
  }

  review(id: number, body: ReviewRequest): Observable<Claim> {
    return this.http.put<Claim>(`${API}/${id}/review`, body);
  }

  reextract(id: number): Observable<Claim> {
    return this.http.post<Claim>(`${API}/${id}/extract`, {});
  }

  fileUrl(id: number): string {
    return `${API}/${id}/file`;
  }
}