export enum BorrowTransactionStatus {
  Borrowed = 1,
  Returned = 2,
}

export interface BorrowBookRequest {
  bookId: number;
}

export interface BorrowTransactionDto {
  id: number;
  bookId: number;
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
  page?: number;
  pageSize?: number;
}

export interface AdminBorrowingHistoryRequest extends BorrowingHistoryRequest {
  userId?: string;
  bookId?: number;
  borrowedFrom?: string;
  borrowedTo?: string;
  returnedFrom?: string;
  returnedTo?: string;
}
