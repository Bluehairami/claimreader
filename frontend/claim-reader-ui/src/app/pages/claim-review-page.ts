import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { DomSanitizer } from '@angular/platform-browser';
import { ClaimService } from '../services/claim.service';
import { Claim, ReviewRequest } from '../models/claim';
import { StatusBadge } from '../components/status-badge';

@Component({
  selector: 'app-claim-review-page',
  imports: [ReactiveFormsModule, RouterLink, StatusBadge],
  template: `
    <a class="back" routerLink="/claims">← Back to claims</a>

    @if (loadError()) {
      <p class="error">{{ loadError() }}</p>
    } @else if (claim(); as c) {
      <div class="head">
        <h2>Claim #{{ c.id }} · {{ c.fileName }}</h2>
        <app-status-badge [status]="c.status" />
      </div>

      @if (c.status === 'NeedsReview') {
        <p class="note warn">Check the highlighted fields against the document, correct them, then approve.</p>
      } @else if (c.status === 'Failed') {
        <p class="note bad">Extraction failed. Type the values from the document, or re-run extraction.</p>
      } @else if (c.status === 'Approved') {
        <p class="note ok">This claim is approved. You can still correct it and approve again.</p>
      }

      <div class="grid">
        <section class="doc">
          @if (isImage()) {
            <img [src]="rawUrl" alt="Claim document" />
          } @else {
            <iframe [src]="frameUrl" title="Claim document"></iframe>
          }
        </section>

        <section class="panel">
          <form [formGroup]="form" (ngSubmit)="approve()">
            <label class="field" [class.flag]="flagged('claimantName')">
              <span>Claimant name</span>
              <input formControlName="claimantName" />
              @if (flagged('claimantName')) { <small>Missing or looks wrong</small> }
            </label>

            <label class="field" [class.flag]="flagged('policyNumber')">
              <span>Policy number</span>
              <input formControlName="policyNumber" placeholder="PM-123456" />
              @if (flagged('policyNumber')) { <small>Missing or not in the format PM-123456</small> }
            </label>

            <label class="field" [class.flag]="flagged('claimAmount')">
              <span>Amount claimed</span>
              <input type="number" step="0.01" formControlName="claimAmount" />
              @if (flagged('claimAmount')) { <small>Missing or outside the expected range</small> }
            </label>

            <label class="field" [class.flag]="flagged('incidentDate')">
              <span>Date of incident</span>
              <input type="date" formControlName="incidentDate" />
              @if (flagged('incidentDate')) { <small>Missing, in the future, or too old</small> }
            </label>

            <label class="field">
              <span>Description</span>
              <textarea rows="4" formControlName="description"></textarea>
            </label>

            @if (error()) { <p class="error">{{ error() }}</p> }
            @if (message()) { <p class="success">{{ message() }}</p> }

            <div class="actions">
              <button class="btn btn-primary" type="submit" [disabled]="busy()">
                {{ busy() ? 'Working…' : 'Approve' }}
              </button>
              <button class="btn btn-secondary" type="button" (click)="rerun()" [disabled]="busy() || c.status === 'Approved'">
                Re-run extraction
              </button>
            </div>
          </form>
        </section>
      </div>
    } @else {
      <p class="muted">Loading…</p>
    }
  `,
  styles: `
    .back { display: inline-block; margin-bottom: 8px; }
    .head { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; margin-bottom: 8px; }
    .note { padding: 10px 14px; border-radius: 8px; margin: 8px 0 16px; }
    .note.warn { background: var(--amber-bg); color: var(--amber); }
    .note.bad { background: var(--red-bg); color: var(--red); }
    .note.ok { background: var(--teal-light); color: var(--teal-dark); }
    .grid { display: grid; grid-template-columns: 1.2fr 1fr; gap: 16px; align-items: start; }
    .doc { background: #fff; border: 1px solid var(--slate-200); border-radius: 10px; overflow: hidden; }
    .doc iframe { width: 100%; height: 78vh; border: 0; display: block; }
    .doc img { width: 100%; display: block; }
    .panel { background: #fff; border: 1px solid var(--slate-200); border-radius: 10px; padding: 16px; }
    .field { display: flex; flex-direction: column; gap: 4px; margin-bottom: 14px; }
    .field span { font-size: 13px; font-weight: 600; color: var(--slate-700); }
    .field input, .field textarea { padding: 8px 10px; border: 1px solid var(--slate-300); border-radius: 8px; font: inherit; }
    .field input:focus, .field textarea:focus { outline: 2px solid var(--teal); border-color: var(--teal); }
    .field.flag input { border-color: #f59e0b; background: #fffbeb; }
    .field small { color: var(--amber); }
    .actions { display: flex; gap: 8px; margin-top: 8px; }
    .success { color: var(--teal-dark); }
    @media (max-width: 900px) { .grid { grid-template-columns: 1fr; } .doc iframe { height: 60vh; } }
  `,
})
export class ClaimReviewPage {
  private service = inject(ClaimService);
  private route = inject(ActivatedRoute);
  private sanitizer = inject(DomSanitizer);

  private id = Number(this.route.snapshot.paramMap.get('id'));

  claim = signal<Claim | null>(null);
  issues = signal<string[]>([]);
  busy = signal(false);
  error = signal('');
  message = signal('');
  loadError = signal('');

  rawUrl = this.service.fileUrl(this.id);
  frameUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.service.fileUrl(this.id));
  isImage = computed(() => /\.(png|jpe?g)$/i.test(this.claim()?.fileName ?? ''));

  form = new FormGroup({
    claimantName: new FormControl<string | null>(''),
    policyNumber: new FormControl<string | null>(''),
    claimAmount: new FormControl<number | null>(null),
    incidentDate: new FormControl<string | null>(''),
    description: new FormControl<string | null>(''),
  });

  constructor() {
    this.service.get(this.id).subscribe({
      next: (c) => this.apply(c),
      error: () => this.loadError.set('Could not load this claim.'),
    });
  }

  flagged(field: string): boolean {
    return this.issues().includes(field);
  }

  approve() {
    const v = this.form.getRawValue();
    const body: ReviewRequest = {
      claimantName: v.claimantName?.trim() || null,
      policyNumber: v.policyNumber?.trim() || null,
      claimAmount: v.claimAmount,
      incidentDate: v.incidentDate || null,
      description: v.description?.trim() || null,
    };

    this.start();
    this.service.review(this.id, body).subscribe({
      next: (c) => {
        this.apply(c);
        this.busy.set(false);
        this.message.set('Claim approved.');
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err?.error?.error ?? 'Could not save the claim.');
        if (Array.isArray(err?.error?.issues)) this.issues.set(err.error.issues);
      },
    });
  }

  rerun() {
    this.start();
    this.service.reextract(this.id).subscribe({
      next: (c) => {
        this.apply(c);
        this.busy.set(false);
        this.message.set('Extraction re-run. Check the fields again.');
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err?.error?.error ?? 'Re-running extraction failed.');
      },
    });
  }

  private start() {
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
  }

  private apply(c: Claim) {
    this.claim.set(c);
    this.issues.set(c.issues);
    this.form.reset({
      claimantName: c.claimantName ?? '',
      policyNumber: c.policyNumber ?? '',
      claimAmount: c.claimAmount,
      incidentDate: c.incidentDate ?? '',
      description: c.description ?? '',
    });
  }
}