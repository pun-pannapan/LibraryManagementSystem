import { environment } from '../../../environments/environment';

const apiBaseUrl = environment.apiBaseUrl;

export const apiEndpoints = {
  auth: `${apiBaseUrl}/auth`,
  books: `${apiBaseUrl}/books`,
  categories: `${apiBaseUrl}/categories`,
  borrowings: `${apiBaseUrl}/borrowings`,
  health: `${apiBaseUrl}/health`,
};
