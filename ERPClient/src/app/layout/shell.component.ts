import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { DemoService } from '../core/demo.service';
import { ThemeService } from '../core/theme.service';
import { ConfirmHostComponent } from '../ui/confirm-host.component';
import { IconComponent } from '../ui/icon.component';
import { ToastHostComponent } from '../ui/toast-host.component';
import { DemoBarComponent } from './demo-bar.component';
import { DemoPromptComponent } from './demo-prompt.component';
import { RailComponent } from './rail.component';

const COLLAPSE_KEY = 'tz-rail-collapsed';

/** Oturum açıkken görünen her ekranın çerçevesi. */
@Component({
  selector: 'tz-shell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RailComponent,
    DemoBarComponent,
    DemoPromptComponent,
    ToastHostComponent,
    ConfirmHostComponent,
    IconComponent,
  ],
  template: `
    <div class="tz-shell" [class.collapsed]="collapsed()" [class.drawer-open]="drawerOpen()">
      <tz-rail (navigate)="drawerOpen.set(false)" />

      <div class="tz-main">
        <div class="tz-headstack">
          @if (demo.isDemo) {
            <tz-demo-bar />
          }

          <header class="tz-topbar">
            <button
              type="button"
              class="tz-iconbtn"
              (click)="toggleRail()"
              [attr.aria-label]="'Menüyü aç/kapat'"
            >
              <tz-icon name="menu" />
            </button>

            <div class="tz-topbar__title">{{ pageTitle() }}</div>
            <div class="tz-topbar__spacer"></div>

            <button
              type="button"
              class="tz-iconbtn"
              (click)="theme.toggle()"
              [attr.aria-label]="theme.theme() === 'dark' ? 'Açık temaya geç' : 'Koyu temaya geç'"
            >
              <tz-icon [name]="theme.theme() === 'dark' ? 'sun' : 'moon'" />
            </button>

            <!-- Çıkış yalnızca raydaki kullanıcı kutusundaydı ve fark edilmiyordu;
                 alışılmış yeri olan üst çubuğa da kondu. -->
            <button
              type="button"
              class="tz-iconbtn"
              (click)="signOut()"
              [attr.aria-label]="demo.isDemo ? 'Demodan çık' : 'Çıkış yap'"
              [attr.title]="demo.isDemo ? 'Demodan çık' : 'Çıkış yap'"
            >
              <tz-icon name="logout" />
            </button>
          </header>
        </div>

        <main class="tz-content">
          <router-outlet />
        </main>
      </div>

      @if (drawerOpen()) {
        <div class="tz-scrim" (click)="drawerOpen.set(false)"></div>
      }
    </div>

    <tz-demo-prompt />
    <tz-confirm-host />
    <tz-toast-host />
  `,
})
export class ShellComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly title = inject(Title);
  private readonly auth = inject(AuthService);

  readonly theme = inject(ThemeService);
  readonly demo = inject(DemoService);

  readonly pageTitle = signal('Panel');
  readonly collapsed = signal(this.readCollapsed());
  readonly drawerOpen = signal(false);

  ngOnInit(): void {
    this.auth.isAuthenticated();

    // Sayfa yenilendiğinde kota ve süre sunucudan tazeleniyor; yalnızca istemcide
    // tutulsa yenilemeden sonra sıfırdan başlamış gibi görünürdü.
    this.demo.refreshStatus();

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        map(() => this.deepestTitle())
      )
      .subscribe((heading) => {
        this.pageTitle.set(heading);
        this.title.setTitle(`${heading} · Tezgah`);
      });

    this.pageTitle.set(this.deepestTitle());
  }

  signOut(): void {
    // Demo oturumu sunucuya da bildiriliyor; sandbox sıradaki ziyaretçiye
    // beklemeden iade edilsin.
    this.demo.exit();
  }

  toggleRail(): void {
    // Dar ekranda aynı düğme çekmeceyi açıyor, geniş ekranda rayı daraltıyor.
    if (window.matchMedia('(max-width: 900px)').matches) {
      this.drawerOpen.update((open) => !open);
      return;
    }

    this.collapsed.update((value) => {
      const next = !value;

      try {
        localStorage.setItem(COLLAPSE_KEY, String(next));
      } catch {
        /* yoksayılır */
      }

      return next;
    });
  }

  private deepestTitle(): string {
    let route = this.route;
    while (route.firstChild) route = route.firstChild;

    return (route.snapshot.data['title'] as string) ?? 'Tezgah';
  }

  private readCollapsed(): boolean {
    try {
      return localStorage.getItem(COLLAPSE_KEY) === 'true';
    } catch {
      return false;
    }
  }
}
