import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiErrorService } from '../../../core/http/api-error.service';
import { NotificationService } from '../../../core/notifications/notification.service';
import { BooksApiService } from '../../books/books-api.service';
import { CategoryDto, CreateBookRequest } from '../../../shared/models/book.models';
import { CategoriesApiService } from '../../books/categories-api.service';
import { ValidationMessageComponent } from '../../../shared/components/validation-message/validation-message.component';

type BookFormField = 'isbn' | 'title' | 'author' | 'publisher' | 'publishedYear' | 'categoryId';

@Component({
  selector: 'app-book-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ValidationMessageComponent],
  templateUrl: './book-form.component.html',
})
export class BookFormComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(BooksApiService);
  private readonly categoriesApi = inject(CategoriesApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly apiErrors = inject(ApiErrorService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly isEdit = this.route.snapshot.data['mode'] === 'edit';
  protected readonly isLoadingBook = signal(this.isEdit);
  protected readonly loadError = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly needsReload = signal(false);
  protected readonly fieldErrors = signal<Partial<Record<BookFormField, string>>>({});
  protected readonly categories = signal<CategoryDto[]>([]);
  protected readonly categoriesLoading = signal(true);
  protected readonly categoriesError = signal<string | null>(null);
  private rowVersion: string | null = null;
  protected readonly currentYear = new Date().getUTCFullYear();
  protected readonly form = this.formBuilder.nonNullable.group({
    isbn: ['', [Validators.required, Validators.maxLength(20)]],
    title: ['', [Validators.required, Validators.maxLength(250)]],
    author: ['', [Validators.required, Validators.maxLength(200)]],
    publisher: ['', Validators.maxLength(200)],
    publishedYear: [
      '',
      [Validators.pattern(/^\d+$/), Validators.min(1000), Validators.max(this.currentYear + 1)],
    ],
    categoryId: ['', [Validators.required, Validators.pattern(/^[1-9]\d*$/)]],
  });

  ngOnInit(): void {
    this.loadCategories();
    if (!this.isEdit) return;

    this.loadBook();
  }

  protected loadCategories(): void {
    this.categoriesLoading.set(true);
    this.categoriesError.set(null);
    this.categoriesApi
      .getCategories()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.categoriesLoading.set(false)),
      )
      .subscribe({
        next: (categories) => this.categories.set(categories),
        error: (error: unknown) =>
          this.categoriesError.set(this.apiErrors.messageFor(error, 'Unable to load categories.')),
      });
  }

  protected reloadBook(): void {
    this.formError.set(null);
    this.needsReload.set(false);
    this.loadBook();
  }

  private loadBook(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(id) || id < 1) {
      this.isLoadingBook.set(false);
      this.loadError.set('The requested book could not be found.');
      return;
    }

    this.isLoadingBook.set(true);
    this.loadError.set(null);
    this.api
      .getBookById(id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingBook.set(false)),
      )
      .subscribe({
        next: (book) => {
          this.rowVersion = book.rowVersion;
          this.fieldErrors.set({});
          this.form.reset({
            isbn: book.isbn,
            title: book.title,
            author: book.author,
            publisher: book.publisher ?? '',
            publishedYear: book.publishedYear?.toString() ?? '',
            categoryId: book.categoryId.toString(),
          });
        },
        error: (error: unknown) =>
          this.loadError.set(
            error instanceof HttpErrorResponse && error.status === 404
              ? 'This book could not be found in the inventory.'
              : this.apiErrors.messageFor(error, 'Unable to load this book.'),
          ),
      });
  }

  protected fieldError(field: BookFormField): string | null {
    const serverError = this.fieldErrors()[field];
    if (serverError) return serverError;

    const control = this.form.controls[field];
    if (!control.touched || !control.errors) return null;
    if (control.hasError('required')) return 'This field is required.';
    if (control.hasError('maxlength')) return 'This value is too long.';
    if (control.hasError('pattern'))
      return field === 'publishedYear'
        ? 'Enter a whole-number year.'
        : 'Enter a positive whole-number category ID.';
    if (control.hasError('min') || control.hasError('max')) {
      return `Enter a year between 1000 and ${this.currentYear + 1}.`;
    }
    return 'Enter a valid value.';
  }

  protected clearServerError(field: BookFormField): void {
    const errors = { ...this.fieldErrors() };
    delete errors[field];
    this.fieldErrors.set(errors);
  }

  protected submit(): void {
    if (this.isSubmitting()) return;
    this.formError.set(null);
    this.fieldErrors.set({});

    const values = this.form.getRawValue();
    this.form.patchValue({
      isbn: values.isbn.trim(),
      title: values.title.trim(),
      author: values.author.trim(),
      publisher: values.publisher.trim(),
    });

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: CreateBookRequest = {
      isbn: values.isbn.trim(),
      title: values.title.trim(),
      author: values.author.trim(),
      publisher: values.publisher.trim() || null,
      publishedYear: values.publishedYear ? Number(values.publishedYear) : null,
      categoryId: Number(values.categoryId),
    };

    const rowVersion = this.rowVersion;
    if (this.isEdit && !rowVersion) {
      this.formError.set('The edit version is missing. Reload the latest book data before saving.');
      this.needsReload.set(true);
      return;
    }

    const saveRequest =
      this.isEdit && rowVersion
        ? this.api.updateBook(Number(this.route.snapshot.paramMap.get('id')), {
            ...request,
            rowVersion,
          })
        : this.api.createBook(request);

    this.isSubmitting.set(true);
    saveRequest
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => {
          this.notifications.show(
            'success',
            this.isEdit ? 'Book changes saved successfully.' : 'Book created successfully.',
          );
          void this.router.navigate(['/admin/books'], {
            queryParams: this.route.snapshot.queryParams,
          });
        },
        error: (error: unknown) => this.handleSubmitError(error),
      });
  }

  private handleSubmitError(error: unknown): void {
    if (error instanceof HttpErrorResponse && error.status === 409) {
      const message = this.apiErrors.messageFor(error, 'This ISBN is already in use.');
      if (message.toLowerCase().includes('isbn')) {
        this.fieldErrors.set({ isbn: message });
        return;
      }

      if (this.isEdit) {
        this.formError.set(
          'This book changed after you opened it. Reload the latest version and try again.',
        );
        this.needsReload.set(true);
        return;
      }
    }

    if (error instanceof HttpErrorResponse && error.status === 400) {
      const body = error.error as { errors?: unknown } | null;
      if (body?.errors && typeof body.errors === 'object') {
        const fieldErrors: Partial<Record<BookFormField, string>> = {};
        const fields: BookFormField[] = [
          'isbn',
          'title',
          'author',
          'publisher',
          'publishedYear',
          'categoryId',
        ];
        for (const [name, messages] of Object.entries(body.errors)) {
          const field = fields.find((candidate) => candidate.toLowerCase() === name.toLowerCase());
          const message = Array.isArray(messages)
            ? messages.find((value): value is string => typeof value === 'string')
            : null;
          if (field && message) fieldErrors[field] = message;
        }

        if (Object.keys(fieldErrors).length > 0) {
          this.fieldErrors.set(fieldErrors);
          return;
        }
      }
    }

    this.formError.set(this.apiErrors.messageFor(error, 'Unable to save this book.'));
  }
}
