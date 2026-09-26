export enum BookAvailabilityStatus {
  Available = 1,
  Borrowed = 2,
  Reserved = 3,
}

export interface CategoryDto {
  id: string;
  name: string;
  description: string | null;
}

export interface BookDto {
  id: string;
  isbn: string;
  title: string;
  author: string;
  publisher: string | null;
  publishedYear: number | null;
  categoryId: string;
  categoryName: string;
  availabilityStatus: BookAvailabilityStatus;
  shelfCode?: string | null;
  location?: string | null;
  /** Base64 encoded SQL Server row-version value, required for updates. */
  rowVersion: string;
}

export type BookSummary = BookDto;
export type BookDetail = BookDto;

export const BOOK_SORT_FIELDS = [
  'title',
  'author',
  'isbn',
  'category',
  'publishedYear',
  'shelfCode',
  'location',
] as const;

export type BookSortField = (typeof BOOK_SORT_FIELDS)[number];

export interface CreateBookRequest {
  isbn: string;
  title: string;
  author: string;
  publisher: string | null;
  publishedYear: number | null;
  categoryId: string;
  shelfCode?: string | null;
  location?: string | null;
}

export interface UpdateBookRequest extends CreateBookRequest {
  rowVersion: string;
}

export interface BookSearchRequest {
  search?: string;
  title?: string;
  author?: string;
  isbn?: string;
  shelfCode?: string;
  location?: string;
  categoryId?: string;
  available?: boolean;
  sortBy?: BookSortField;
  sortDirection?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}
