import { Routes } from '@angular/router';
import { UploadPage } from './pages/upload-page';
import { ClaimListPage } from './pages/claim-list-page';
import { ClaimReviewPage } from './pages/claim-review-page';

export const routes: Routes = [
  { path: '', redirectTo: 'upload', pathMatch: 'full' },
  { path: 'upload', component: UploadPage },
  { path: 'claims', component: ClaimListPage },
  { path: 'claims/:id', component: ClaimReviewPage },
  { path: '**', redirectTo: 'claims' },
];