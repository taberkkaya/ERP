import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  HostListener,
  Input,
  Output,
  booleanAttribute,
} from '@angular/core';
import { IconComponent } from './icon.component';

/**
 * Uygulamanın tek kip penceresi. Bootstrap'ın `data-dismiss` düğmelerine
 * dayanan kipleri yerine, açık/kapalı durumu tamamen çağıran bileşende duruyor:
 * `@if (open()) { <tz-modal ...> }`.
 */
@Component({
  selector: 'tz-modal',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <div class="tz-modal" (click)="onBackdrop($event)">
      <div
        class="tz-modal__box"
        [class.tz-modal__box--wide]="wide"
        [class.tz-modal__box--slim]="slim"
        role="dialog"
        aria-modal="true"
        [attr.aria-label]="title"
      >
        <div class="tz-modal__head">
          <div class="tz-modal__title">{{ title }}</div>
          <button type="button" class="tz-iconbtn" (click)="close.emit()" aria-label="Kapat">
            <tz-icon name="close" />
          </button>
        </div>

        <div class="tz-modal__body">
          <ng-content />
        </div>

        <div class="tz-modal__foot">
          <ng-content select="[modalFooter]" />
        </div>
      </div>
    </div>
  `,
})
export class ModalComponent {
  @Input() title = '';
  @Input({ transform: booleanAttribute }) wide = false;
  @Input({ transform: booleanAttribute }) slim = false;

  /** Zemine tıklayınca kapanmasın: uzun bir formda yanlışlıkla veri kaybettiriyor. */
  @Input({ transform: booleanAttribute }) closeOnBackdrop = false;

  @Output() close = new EventEmitter<void>();

  onBackdrop(event: MouseEvent): void {
    if (!this.closeOnBackdrop) return;

    if (event.target === event.currentTarget) this.close.emit();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.close.emit();
  }
}
