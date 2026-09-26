import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-route-message',
  standalone: true,
  imports: [RouterLink],
  template: `
    <main class="container py-5">
      <section class="mx-auto text-center" style="max-width: 38rem" aria-labelledby="message-title">
        <p class="text-uppercase small fw-semibold text-primary mb-2">Library Management System</p>
        <h1 id="message-title" class="display-6">{{ title }}</h1>
        <p class="lead text-secondary">{{ description }}</p>
        <a class="btn btn-primary" routerLink="/books">Go to books</a>
      </section>
    </main>
  `,
})
export class RouteMessageComponent {
  private readonly route = inject(ActivatedRoute);
  protected readonly title = this.route.snapshot.data['title'] as string ?? 'Page not found';
  protected readonly description = this.route.snapshot.data['description'] as string
    ?? 'The page you requested could not be found.';
}
