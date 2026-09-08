import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ConfirmService } from '../core/confirm.service';
import { IconComponent } from './icon.component';
import { ModalComponent } from './modal.component';

/** Onay penceresi. `ConfirmService.ask(...)` çağrısını bekleyen tek yüzey. */
@Component({
  selector: 'tz-confirm-host',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ModalComponent, IconComponent],
  template: `
    @if (confirm.request(); as request) {
      <tz-modal [title]="request.title" slim (close)="confirm.answer(false)">
        <div class="tz-row tz-row--top" style="gap: 12px">
          <span
            class="tz-demoprompt__mark"
            style="margin: 0; width: 36px; height: 36px; border-radius: 10px"
            [style.background]="request.danger ? 'var(--tz-danger-soft)' : 'var(--tz-accent-soft)'"
            [style.color]="request.danger ? 'var(--tz-danger)' : 'var(--tz-accent)'"
          >
            <tz-icon [name]="request.danger ? 'alert' : 'info'" />
          </span>
          <p class="tz-ink2" style="margin: 0">{{ request.text }}</p>
        </div>

        <ng-container modalFooter>
          <button type="button" class="tz-btn" (click)="confirm.answer(false)">Vazgeç</button>
          <button
            type="button"
            class="tz-btn"
            [class.tz-btn--danger]="request.danger"
            [class.tz-btn--primary]="!request.danger"
            (click)="confirm.answer(true)"
          >
            {{ request.confirmLabel }}
          </button>
        </ng-container>
      </tz-modal>
    }
  `,
})
export class ConfirmHostComponent {
  readonly confirm = inject(ConfirmService);
}
