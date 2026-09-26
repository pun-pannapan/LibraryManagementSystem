export enum BorrowTransactionStatus {
  Borrowed = 1,
  Returned = 2,
}

export interface BorrowBookRequest {
  bookId: string;
}

export interface BorrowTransactionDto {
  id: string;
  bookId: string;
  bookTitle: string;
  userId: string;
  userEmail: string;
  borrowedAtUtc: string;
  dueAtUtc: string;
  returnedAtUtc: string | null;
  status: BorrowTransactionStatus;
}

export type BorrowingHistoryItem = BorrowTransactionDto;

export interface BorrowingHistoryRequest {
  status?: 'Borrowed' | 'Returned';
  sort?: string;
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
