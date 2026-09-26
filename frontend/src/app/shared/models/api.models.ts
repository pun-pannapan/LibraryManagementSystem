export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  message?: string;
}

export interface ValidationProblemDetails extends ApiProblemDetails {
  errors: Record<string, string[]>;
}

export interface ApiMessageError {
  message: string;
}

export type ApiErrorResponse =
  | ApiProblemDetails
  | ValidationProblemDetails
  | ApiMessageError;
