import { Component, computed, input } from '@angular/core';
import { ClaimStatus } from '../models/claim';

@Component({
  selector: 'app-status-badge',
  template: `<span class="badge" [class]="status()">{{ label() }}</span>`,
  styles: `
    .badge { display: inline-block; padding: 2px 10px; border-radius: 999px; font-size: 12px; font-weight: 600; background: var(--slate-200); color: var(--slate-700); }
    .Extracted { background: var(--green-bg); color: var(--green); }
    .NeedsReview { background: var(--amber-bg); color: var(--amber); }
    .Approved { background: var(--teal-light); color: var(--teal-dark); }
    .Failed { background: var(--red-bg); color: var(--red); }
  `,
})
export class StatusBadge {
  status = input.required<ClaimStatus>();
  label = computed(() => (this.status() === 'NeedsReview' ? 'Needs review' : this.status()));
}