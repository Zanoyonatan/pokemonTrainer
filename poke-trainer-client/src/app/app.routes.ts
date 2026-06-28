import { Routes } from '@angular/router';
import { AppShell } from './core/layout/app-shell';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/auth/login/login-page').then(m => m.LoginPage) },
  { path: 'register', loadComponent: () => import('./features/auth/register/register-page').then(m => m.RegisterPage) },
  {
    path: 'app',
    component: AppShell,
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard-page').then(m => m.DashboardPage) },
      { path: 'catalog', loadComponent: () => import('./features/pokemon-catalog/catalog-page').then(m => m.CatalogPage) },
      { path: 'pokemon/:id', loadComponent: () => import('./features/pokemon-details/pokemon-details-page').then(m => m.PokemonDetailsPage) },
      { path: 'smart-search', loadComponent: () => import('./features/smart-search/smart-search-page').then(m => m.SmartSearchPage) },
      { path: 'team', loadComponent: () => import('./features/dream-team/dream-team-page').then(m => m.DreamTeamPage) },
      { path: 'analyzer', loadComponent: () => import('./features/team-analyzer/team-analyzer-page').then(m => m.TeamAnalyzerPage) },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
    ]
  },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: '**', redirectTo: 'login' }
];
