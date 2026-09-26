export enum BookAvailabilityStatus {
  Available = 1,
  Borrowed = 2,
}

export interface CategoryDto {
  id: number;
  name: string;
  description: string | null;
}

export interface BookDto {
  id: number;
  isbn: string;
  title: string;
  author: string;
  publisher: string | null;
  publishedYear: number | null;
  categoryId: number;
  categoryName: string;
  availabilityStatus: BookAvailabilityStatus;
  /** Base64 encoded SQL Server row-version value, required for updates. */
  rowVersion: string;
}

export type BookSummary = BookDto;
export type BookDetail = BookDto;

export interface CreateBookRequest {
  isbn: string;
  title: string;
  author: string;
  publisher: string | null;
  publishedYear: number | null;
  categoryId: number;
}

export interface UpdateBookRequest extends CreateBookRequest {
  rowVersion: string;
}

export interface BookSearchRequest {
  search?: string;
  title?: string;
  author?: string;
  isbn?: string;
  categoryId?: number;
  available?: boolean;
  sortBy?: 'title' | 'author' | 'publishedYear';
  sortDirection?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}
