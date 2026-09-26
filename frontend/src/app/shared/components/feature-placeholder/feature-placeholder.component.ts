import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-feature-placeholder',
  standalone: true,
  template: `
    <main class="container py-5">
      <h1 class="h2">{{ title }}</h1>
      <p class="text-secondary">This area is protected and ready for its feature implementation.</p>
    </main>
  `,
})
export class FeaturePlaceholderComponent {
  private readonly route = inject(ActivatedRoute);
  protected readonly title = this.route.snapshot.data['title'] as string ?? 'Library';
}
