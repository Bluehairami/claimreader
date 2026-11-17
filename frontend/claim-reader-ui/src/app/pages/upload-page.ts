import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ClaimService } from '../services/claim.service';

@Component({
  selector: 'app-upload-page',
  template: `
    <h2>Upload a claim document</h2>
    <p class="muted">PDF, PNG or JPG, up to 10 MB. Use fictional documents only.</p>

    <div
      class="drop"
      [class.over]="over()"
      (dragover)="onDragOver($event)"
      (dragleave)="over.set(false)"
      (drop)="onDrop($event)"
    >
      @if (uploading()) {
        <p class="big">Reading the document…</p>
        <p class="muted">This can take a few seconds.</p>
      } @else {
        <p class="big">Drag and drop a file here</p>
        <p class="muted">or</p>
        <button class="btn btn-primary" type="button" (click)="picker.click()">Choose file</button>
        <input #picker type="file" hidden accept=".pdf,.png,.jpg,.jpeg" (change)="onPick($event)" />
      }
    </div>

    @if (error()) {
      <p class="error">{{ error() }}</p>
    }

    <ol class="steps">
      <li><strong>1. Upload</strong><span>Add a claim form or letter.</span></li>
      <li><strong>2. Extract</strong><span>The AI reads the name, policy, amount, date and description.</span></li>
      <li><strong>3. Review</strong><span>You check the highlighted fields and approve.</span></li>
    </ol>
  `,
  styles: `
    h2 { margin-bottom: 4px; }
    .drop { margin: 20px 0; padding: 48px 16px; text-align: center; background: #fff; border: 2px dashed var(--slate-300); border-radius: 12px; }
    .drop.over { border-color: var(--teal); background: var(--teal-light); }
    .big { font-size: 18px; font-weight: 600; margin: 0 0 6px; }
    .steps { list-style: none; padding: 0; display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 12px; }
    .steps li { background: #fff; border: 1px solid var(--slate-200); border-radius: 10px; padding: 14px; display: flex; flex-direction: column; gap: 4px; }
    .steps span { color: var(--slate-500); font-size: 14px; }
  `,
})
export class UploadPage {
  private service = inject(ClaimService);
  private router = inject(Router);

  over = signal(false);
  uploading = signal(false);
  error = signal('');

  onDragOver(e: DragEvent) {
    e.preventDefault();
    this.over.set(true);
  }

  onDrop(e: DragEvent) {
    e.preventDefault();
    this.over.set(false);
    const file = e.dataTransfer?.files?.[0];
    if (file) this.send(file);
  }

  onPick(e: Event) {
    const input = e.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.send(file);
    input.value = '';
  }

  private send(file: File) {
    const ext = file.name.split('.').pop()?.toLowerCase() ?? '';
    if (!['pdf', 'png', 'jpg', 'jpeg'].includes(ext)) {
      this.error.set('Only PDF, PNG and JPG files are allowed.');
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      this.error.set('The file is larger than 10 MB.');
      return;
    }

    this.error.set('');
    this.uploading.set(true);
    this.service.upload(file).subscribe({
      next: (claim) => this.router.navigate(['/claims', claim.id]),
      error: (err) => {
        this.uploading.set(false);
        this.error.set(err?.error?.error ?? 'Upload failed. Is the API running?');
      },
    });
  }
}