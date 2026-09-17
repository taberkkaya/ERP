import { inject } from '@angular/core';
import { Routes } from '@angular/router';
import { AuthService } from './core/auth.service';
import { ShellComponent } from './layout/shell.component';

/**
 * Yollar Türkçe: uygulamanın dili Türkçe ve adres çubuğu da arayüzün bir parçası.
 *
 * Ekranlar tembel yükleniyor; giriş ekranına gelen bir ziyaretçi, hiç açmayacağı
 * reçete veya üretim ekranını indirmek zorunda kalmıyor.
 */
export const routes: Routes = [
  {
    path: 'giris',
    title: 'Giriş · Tezgah',
    loadComponent: () => import('./pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    component: ShellComponent,
    canActivateChild: [() => inject(AuthService).isAuthenticated()],
    children: [
      {
        path: '',
        data: { title: 'Panel' },
        loadComponent: () =>
          import('./pages/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'musteriler',
        data: { title: 'Müşteriler' },
        loadComponent: () =>
          import('./pages/customers/customers.component').then((m) => m.CustomersComponent),
      },
      {
        path: 'depolar',
        data: { title: 'Depolar' },
        loadComponent: () => import('./pages/depots/depots.component').then((m) => m.DepotsComponent),
      },
      {
        path: 'urunler',
        data: { title: 'Ürünler' },
        loadComponent: () =>
          import('./pages/products/products.component').then((m) => m.ProductsComponent),
      },
      {
        path: 'receteler',
        data: { title: 'Reçeteler' },
        loadComponent: () =>
          import('./pages/recipes/recipes.component').then((m) => m.RecipesComponent),
      },
      {
        path: 'receteler/:id',
        data: { title: 'Reçete Detayı' },
        loadComponent: () =>
          import('./pages/recipe-detail/recipe-detail.component').then(
            (m) => m.RecipeDetailComponent
          ),
      },
      {
        path: 'siparisler',
        data: { title: 'Siparişler' },
        loadComponent: () => import('./pages/orders/orders.component').then((m) => m.OrdersComponent),
      },
      {
        path: 'siparisler/:orderId/ihtiyac-plani',
        data: { title: 'İhtiyaç Planlama' },
        loadComponent: () =>
          import('./pages/planning/planning.component').then((m) => m.PlanningComponent),
      },
      {
        path: 'kullanicilar',
        data: { title: 'Kullanıcılar' },
        canActivate: [() => inject(AuthService).isAdmin()],
        loadComponent: () => import('./pages/users/users.component').then((m) => m.UsersComponent),
      },
      {
        path: 'uretim',
        data: { title: 'Üretim' },
        loadComponent: () =>
          import('./pages/productions/productions.component').then((m) => m.ProductionsComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
