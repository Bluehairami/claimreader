import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="top">
      <a class="brand" routerLink="/claims">Claim Reader</a>
      <nav>
        <a routerLink="/upload" routerLinkActive="active">Upload</a>
        <a routerLink="/claims" routerLinkActive="active">Claims</a>
      </nav>
    </header>
    <main class="container"><router-outlet /></main>
  `,
  styles: `
    .top { display: flex; align-items: center; justify-content: space-between; padding: 0 24px; height: 56px; background: var(--slate-900); }
    .brand { color: #fff; font-weight: 700; font-size: 18px; text-decoration: none; }
    nav { display: flex; gap: 20px; }
    nav a { color: var(--slate-300); text-decoration: none; padding: 4px 0; border-bottom: 2px solid transparent; }
    nav a.active { color: #fff; border-bottom-color: #2dd4bf; }
  `,
})
export class App {}