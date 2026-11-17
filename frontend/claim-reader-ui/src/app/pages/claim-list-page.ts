import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ClaimService } from '../services/claim.service';
import { Claim, ClaimStatus } from '../models/claim';
import { StatusBadge } from '../components/status-badge';

@Component({
  selector: 'app-claim-list-page',
  imports: [RouterLink, CurrencyPipe, DatePipe, StatusBadge],
  template: `
    <div class="head">
      <h2>Claims</h2>
      <a class="btn btn-primary" routerLink="/upload">Upload claim</a>
    </div>

    <div class="chips">
      @for (f of filters; track f.label) {
        <button [class.active]="filter() === f.value" (click)="setFilter(f.value)">{{ f.label }}</button>
      }
    </div>

    @if (error()) {
      <p class="error">{{ error() }}</p>
    }

    @if (loading()) {
      <p class="muted">Loading…</p>
    } @else if (claims().length === 0 && !error()) {
      <p class="muted">No claims found.</p>
    } @else if (claims().length > 0) {
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>ID</th><th>File</th><th>Status</th><th>Claimant</th>
              <th>Policy</th><th>Amount</th><th>To check</th><th>Uploaded</th><th></th>
            </tr>
          </thead>
          <tbody>
            @for (c of claims(); track c.id) {
              <tr>
                <td>{{ c.id }}</td>
                <td>{{ c.fileName }}</td>
                <td><app-status-badge [status]="c.status" /></td>
                <td>{{ c.claimantName ?? '—' }}</td>
                <td>{{ c.policyNumber ?? '—' }}</td>
                <td>{{ c.claimAmount !== null ? (c.claimAmount | currency) : '—' }}</td>
                <td>{{ c.issues.length ? c.issues.length + ' field(s)' : '—' }}</td>
                <td>{{ c.createdAt | date: 'mediumDate' }}</td>
                <td><a [routerLink]="['/claims', c.id]">{{ c.status === 'Approved' ? 'View' : 'Review' }}</a></td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
  styles: `
    .head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; }
    .chips { display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 16px; }
    .chips button { padding: 6px 14px; border: 1px solid var(--slate-300); background: #fff; border-radius: 999px; cursor: pointer; color: var(--slate-700); }
    .chips button.active { background: var(--teal); border-color: var(--teal); color: #fff; }
    .table-wrap { overflow-x: auto; background: #fff; border: 1px solid var(--slate-200); border-radius: 10px; }
    table { width: 100%; border-collapse: collapse; }
    th, td { text-align: left; padding: 10px 12px; border-bottom: 1px solid var(--slate-200); white-space: nowrap; }
    th { font-size: 12px; text-transform: uppercase; letter-spacing: .04em; color: var(--slate-500); }
    tr:last-child td { border-bottom: none; }
  `,
})
export class ClaimListPage {
  private service = inject(ClaimService);

  claims = signal<Claim[]>([]);
  filter = signal<ClaimStatus | undefined>(undefined);
  loading = signal(true);
  error = signal('');

  filters: { label: string; value?: ClaimStatus }[] = [
    { label: 'All' },
    { label: 'Needs review', value: 'NeedsReview' },
    { label: 'Approved', value: 'Approved' },
    { label: 'Failed', value: 'Failed' },
  ];

  constructor() {
    this.load();
  }

  setFilter(value?: ClaimStatus) {
    this.filter.set(value);
    this.load();
  }

  private load() {
    this.loading.set(true);
    this.error.set('');
    this.service.list(this.filter()).subscribe({
      next: (claims) => {
        this.claims.set(claims);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load claims. Is the API running?');
        this.loading.set(false);
      },
    });
  }
}