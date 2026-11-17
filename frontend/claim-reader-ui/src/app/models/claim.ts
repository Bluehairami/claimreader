export type ClaimStatus = 'Uploaded' | 'Extracted' | 'NeedsReview' | 'Approved' | 'Failed';

export interface Claim {
  id: number;
  fileName: string;
  status: ClaimStatus;
  claimantName: string | null;
  policyNumber: string | null;
  claimAmount: number | null;
  incidentDate: string | null; // "2026-09-14"
  description: string | null;
  issues: string[];
  createdAt: string;
}

export interface ReviewRequest {
  claimantName: string | null;
  policyNumber: string | null;
  claimAmount: number | null;
  incidentDate: string | null;
  description: string | null;
}