import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { LoginComponent } from './features/auth/login/login.component';
import { BookListComponent } from './features/books/book-list/book-list.component';
import { BookDetailComponent } from './features/books/book-detail/book-detail.component';
import { MyBorrowingsComponent } from './features/borrowings/my-borrowings/my-borrowings.component';
import { RouteMessageComponent } from './shared/components/route-message/route-message.component';
import { AdminBooksComponent } from './features/admin/books/admin-books.component';
import { BookFormComponent } from './features/admin/book-form/book-form.component';
import { AdminTransactionsComponent } from './features/admin/transactions/admin-transactions.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'books' },
  { path: 'login', component: LoginComponent },
  {
    path: 'books',
    canActivate: [authGuard],
    component: BookListComponent,
  },
  {
    path: 'books/:id',
    canActivate: [authGuard],
    component: BookDetailComponent,
  },
  {
    path: 'my-borrowings',
    canActivate: [authGuard, roleGuard],
    component: MyBorrowingsComponent,
    data: { roles: ['User'] },
  },
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard],
    canActivateChild: [roleGuard],
    data: { roles: ['Administrator'] },
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'books' },
      { path: 'books', component: AdminBooksComponent },
      {
        path: 'books/new',
        component: BookFormComponent,
        data: { title: 'Add Book', mode: 'create' },
      },
      {
        path: 'books/:id/edit',
        component: BookFormComponent,
        data: { title: 'Edit Book', mode: 'edit' },
      },
      { path: 'transactions', component: AdminTransactionsComponent },
      {
        path: '**',
        component: RouteMessageComponent,
        data: {
          title: 'Admin page not found',
          description: 'The administrator page you requested does not exist.',
        },
      },
    ],
  },
  {
    path: 'forbidden',
    component: RouteMessageComponent,
    data: {
      title: 'Access denied',
      description: 'Your account does not have permission to open this page.',
    },
  },
  { path: 'not-found', component: RouteMessageComponent },
  { path: '**', redirectTo: 'not-found' },
];
