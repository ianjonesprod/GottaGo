import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'gg-not-found',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MatButtonModule],
  template: `
    <div class="page">
      <h1>Page not found</h1>
      <p>That page doesn't exist. The map is probably where you wanted to be.</p>
      <a mat-flat-button routerLink="/map">Go to the map</a>
    </div>
  `,
  styles: `
    .page {
      padding: 3rem 1rem;
      max-width: 40rem;
      margin: 0 auto;
      text-align: center;
    }
  `,
})
export class NotFound {}
