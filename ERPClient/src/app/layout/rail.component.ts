import { ChangeDetectionStrategy, Component, EventEmitter, Output, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { DemoService } from '../core/demo.service';
import { MENU } from '../core/menu';
import { IconComponent } from '../ui/icon.component';

/** Sol gezinme rayı: marka, bölümlere ayrılmış menü ve altta oturum kutusu. */
@Component({
  selector: 'tz-rail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, IconComponent],
  template: `
    <aside class="tz-rail">
      <a class="tz-rail__brand" routerLink="/" (click)="navigate.emit()">
        <span class="tz-rail__mark"><tz-icon name="brand" [width]="1.9" /></span>
        <span class="tz-rail__word">
          Tezgah
          <small>Üretim Yönetimi</small>
        </span>
      </a>

      <nav class="tz-rail__nav">
        @for (section of menu(); track section.title) {
          @if (section.title) {
            <div class="tz-rail__section">{{ section.title }}</div>
          }

          @for (item of section.items; track item.url) {
            <a
              class="tz-rail__link"
              [routerLink]="item.url"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{ exact: !!item.exact }"
              (click)="navigate.emit()"
            >
              <tz-icon [name]="item.icon" />
              <span>{{ item.label }}</span>
            </a>
          }
        }
      </nav>

      <div class="tz-rail__foot">
        <button type="button" class="tz-user" (click)="signOut()">
          <span class="tz-user__badge">{{ auth.initials() }}</span>
          <span class="tz-user__text">
            <span class="tz-user__name">{{ auth.user().name || 'Ziyaretçi' }}</span>
            <span class="tz-user__role">{{ demo.isDemo ? 'Demo oturumu' : 'Çıkış yap' }}</span>
          </span>
        </button>
      </div>
    </aside>
  `,
})
export class RailComponent {
  readonly auth = inject(AuthService);
  readonly demo = inject(DemoService);
  /**
   * Demo ziyaretçisine kapalı satırlar çıkarılıyor; bir bölümün tüm satırları
   * düşerse başlığı da kalmasın diye bölüm de eleniyor.
   */
  readonly menu = computed(() =>
    MENU.map((section) => ({
      ...section,
      items: section.items.filter((item) => !item.adminOnly || this.auth.user().isAdmin),
    })).filter((section) => section.items.length > 0)
  );

  /** Dar ekranda çekmeceyi kapatmak için kabuğa haber verir. */
  @Output() navigate = new EventEmitter<void>();

  signOut(): void {
    this.demo.exit();
  }
}
