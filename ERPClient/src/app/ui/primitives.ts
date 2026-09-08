import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
  booleanAttribute,
} from '@angular/core';
import { IconComponent } from './icon.component';

/**
 * Sayfa başlığı. Her ekranın üstünde aynı hiyerarşi duruyor: küçük bir bölüm
 * etiketi, başlık, açıklama ve sağda araçlar.
 */
@Component({
  selector: 'tz-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="tz-page__head">
      <div class="tz-page__lead">
        @if (eyebrow) {
          <div class="tz-page__eyebrow">{{ eyebrow }}</div>
        }
        <h1>{{ heading }}</h1>
        @if (description) {
          <div class="tz-page__desc">{{ description }}</div>
        }
      </div>

      <div class="tz-page__tools">
        <ng-content select="[pageTools]" />
      </div>
    </div>

    <ng-content />
  `,
})
export class PageComponent {
  @Input() eyebrow = '';
  @Input({ required: true }) heading = '';
  @Input() description = '';
}

/** Kart yüzeyi: başlık şeridi, araçlar ve gövde. */
@Component({
  selector: 'tz-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="tz-panel">
      @if (heading || hasTools) {
        <div class="tz-panel__head">
          <div class="tz-grow">
            <div class="tz-panel__title">{{ heading }}</div>
            @if (sub) {
              <div class="tz-panel__sub">{{ sub }}</div>
            }
          </div>
          <div class="tz-panel__tools">
            <ng-content select="[panelTools]" />
          </div>
        </div>
      }

      <div class="tz-panel__body" [class.tz-panel__body--flush]="flush">
        <ng-content />
      </div>
    </div>
  `,
})
export class PanelComponent {
  @Input() heading = '';
  @Input() sub = '';

  /** Tablo gibi kendi iç boşluğu olan içerikler için gövde dolgusunu kaldırır. */
  @Input({ transform: booleanAttribute }) flush = false;

  /** Başlık yoksa bile araç şeridi gerekebiliyor (yalnız arama kutusu gibi). */
  @Input({ transform: booleanAttribute }) hasTools = false;
}

/** Liste boşken gösterilen yüzey. */
@Component({
  selector: 'tz-empty',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <div class="tz-empty">
      <div class="tz-empty__mark"><tz-icon [name]="icon" /></div>
      <div class="tz-empty__title">{{ title }}</div>
      @if (text) {
        <div class="tz-empty__text">{{ text }}</div>
      }
      <div class="tz-empty__action">
        <ng-content />
      </div>
    </div>
  `,
})
export class EmptyComponent {
  @Input() icon = 'box';
  @Input({ required: true }) title = '';
  @Input() text = '';
}

/** Liste ekranlarının arama kutusu. */
@Component({
  selector: 'tz-search',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <label class="tz-search">
      <tz-icon name="search" />
      <span class="tz-sr">{{ placeholder }}</span>
      <input
        type="search"
        [placeholder]="placeholder"
        [value]="value"
        (input)="valueChange.emit($any($event.target).value)"
      />
    </label>
  `,
})
export class SearchComponent {
  @Input() value = '';
  @Input() placeholder = 'Ara';

  @Output() valueChange = new EventEmitter<string>();
}
