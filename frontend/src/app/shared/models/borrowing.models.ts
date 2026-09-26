export enum BorrowTransactionStatus {
  Borrowed = 1,
  Returned = 2,
  Requested = 3,
  ReturnRequested = 4,
  Rejected = 5,
  Cancelled = 6,
}

export const BORROWING_STATUS_FILTERS = [
  'Requested',
  'Borrowed',
  'ReturnRequested',
  'Returned',
  'Rejected',
  'Cancelled',
] as const;

export type BorrowingStatusFilter = (typeof BORROWING_STATUS_FILTERS)[number];

export const BORROWING_HISTORY_SORTS = [
  '-requestedat',
  'requestedat',
  '-borrowedat',
  'borrowedat',
  '-dueat',
  'dueat',
  '-returnedat',
  'returnedat',
  '-status',
  'status',
] as const;

export type BorrowingHistorySort = (typeof BORROWING_HISTORY_SORTS)[number];

export interface BorrowBookRequest {
  bookId: string;
}

export interface BorrowTransactionDto {
  id: string;
  bookId: string;
  bookTitle: string;
  userId: string;
  userEmail: string;
  borrowedAtUtc: string | null;
  dueAtUtc: string | null;
  returnedAtUtc: string | null;
  status: BorrowTransactionStatus;
  requestedAtUtc?: string;
  assignedAtUtc?: string | null;
  returnRequestedAtUtc?: string | null;
  assignedByUserId?: string | null;
  processedByUserId?: string | null;
  rejectedAtUtc?: string | null;
  rejectedByUserId?: string | null;
  cancelledAtUtc?: string | null;
}

export type BorrowingHistoryItem = BorrowTransactionDto;

export interface BorrowingHistoryRequest {
  status?: BorrowingStatusFilter;
  sort?: BorrowingHistorySort;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminBorrowingHistoryRequest extends BorrowingHistoryRequest {
  userId?: string;
  bookId?: string;
  borrowedFrom?: string;
  borrowedTo?: string;
  returnedFrom?: string;
  returnedTo?: string;
}
