import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../core/toast.service';
import { IconComponent } from './icon.component';

/** Bildirim yığını. Kabuğun ve giriş ekranının en altında bir kez duruyor. */
@Component({
  selector: 'tz-toast-host',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    @if (toast.toasts().length) {
      <div class="tz-toasts" aria-live="polite">
        @for (item of toast.toasts(); track item.id) {
          <div class="tz-toast tz-toast--{{ item.kind }}">
            <span class="tz-toast__icon"><tz-icon [name]="toast.iconFor(item.kind)" /></span>
            <span class="tz-toast__text">{{ item.text }}</span>
            <button
              type="button"
              class="tz-rowbtn"
              (click)="toast.dismiss(item.id)"
              aria-label="Kapat"
            >
              <tz-icon name="close" />
            </button>
          </div>
        }
      </div>
    }
  `,
})
export class ToastHostComponent {
  readonly toast = inject(ToastService);
}
